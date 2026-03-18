#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Named Pipe IPC server for Unity Editor.
    /// Listens on a background thread; dispatches commands on the main thread via EditorApplication.update.
    /// Singleton — use IpcServer.Instance.
    /// </summary>
    public sealed class IpcServer
    {
        private const int MaxServerInstances = 4;
        private const int PipeBusyRetryDelayMs = 250;
        private const int ErrorPipeBusy = 231;

        // Watch session constants
        private const int MaxWatchQueueSize = 1000;
        private const int HeartbeatIntervalMs = 5000;
        private const int MaxEventsPerPump = 50;
        private const int WatchWriterPollMs = 50;

        private static readonly Lazy<IpcServer> _lazy = new Lazy<IpcServer>(() => new IpcServer());
        public static IpcServer Instance => _lazy.Value;

        private Thread _listenThread;
        private volatile bool _stopping;
        private string _pipeName;
        private string _projectPath;
        private NamedPipeServerStream _currentPipe;
        private readonly object _lock = new object();
        private TaskCompletionSource<bool> _shutdownCompletion = CreateShutdownCompletion();

        private readonly ConcurrentQueue<PendingWork> _mainThreadQueue = new ConcurrentQueue<PendingWork>();

        // Watch session state
        private readonly ConcurrentQueue<EventEnvelope> _watchQueue = new ConcurrentQueue<EventEnvelope>();
        private volatile int _watchQueueCount;
        private volatile bool _watchActive;
        private WatchEventSource _watchEventSource;
        private Thread _watchThread;
        private int _watchDroppedCount;
        private volatile NamedPipeServerStream _watchPipe;

        /// <summary>Whether the IPC server is currently running.</summary>
        public bool IsRunning { get; private set; }

        private IpcServer() { }

        /// <summary>
        /// Start the IPC server. Idempotent — safe to call multiple times.
        /// Does nothing in batchmode.
        /// </summary>
        public void Start(string projectPath)
        {
            if (Application.isBatchMode) return;

            lock (_lock)
            {
                var pipeName = PipeNameHelper.GetPipeName(projectPath);

                // Already running with same pipe name
                if (IsRunning && _pipeName == pipeName) return;

                // Different project or not running — stop existing, start new
                if (IsRunning) StopInternal();

                _projectPath = projectPath;
                _pipeName = pipeName;
                _stopping = false;
                _shutdownCompletion = CreateShutdownCompletion();

                _listenThread = new Thread(ListenLoop)
                {
                    Name = "unityctl-ipc",
                    IsBackground = true
                };
                _listenThread.Start();

                EditorApplication.update -= PumpMainThreadQueue;
                EditorApplication.update += PumpMainThreadQueue;

                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

                EditorApplication.quitting -= OnQuitting;
                EditorApplication.quitting += OnQuitting;

                IsRunning = true;
                Debug.Log($"[unityctl] IPC server started on pipe: {_pipeName}");
            }
        }

        /// <summary>Stop the IPC server gracefully.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            if (!IsRunning) return;

            _stopping = true;
            _shutdownCompletion.TrySetResult(true);

            // Signal watch session to stop
            if (_watchActive)
            {
                try
                {
                    var closeEnvelope = EventEnvelope.Create("_close", "Shutdown");
                    _watchQueue.Enqueue(closeEnvelope);
                }
                catch { }
                _watchActive = false;
                _watchEventSource?.Unsubscribe();
                _watchEventSource = null;
            }

            // Dispose current pipe to unblock WaitForConnection
            try { _currentPipe?.Dispose(); } catch { }

            if (_listenThread != null && _listenThread.IsAlive)
                _listenThread.Join(3000);

            // Wait for watch writer thread to finish
            if (_watchThread != null && _watchThread.IsAlive)
                _watchThread.Join(2000);

            EditorApplication.update -= PumpMainThreadQueue;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.quitting -= OnQuitting;

            // Cancel and drain remaining queued requests so listener threads do not block.
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                pending.WorkItem.Cancel();
            }

            IsRunning = false;
            Debug.Log("[unityctl] IPC server stopped");
        }

        private void OnBeforeAssemblyReload()
        {
            Stop();
        }

        private void OnQuitting()
        {
            Stop();
        }

        /// <summary>
        /// Called after domain reload. Re-registers lifecycle hooks and restarts if needed.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnAfterAssemblyReload()
        {
            // If the server was running before reload, the static singleton is re-created.
            // Bootstrap will call Start() again via UnityctlBootstrap.
        }

        /// <summary>
        /// Background thread: accepts one connection at a time, reads request, queues for main thread.
        /// Watch commands get their own writer thread; all others use request-response.
        /// </summary>
        private void ListenLoop()
        {
            while (!_stopping)
            {
                NamedPipeServerStream pipe = null;
                bool watchSessionStarted = false;
                try
                {
                    pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        MaxServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);

                    _currentPipe = pipe;
                    pipe.WaitForConnection();

                    if (_stopping) break;

                    // Read request
                    var requestJson = MessageFraming.ReadMessage(pipe);
                    var request = JsonConvert.DeserializeObject<CommandRequest>(requestJson);

                    if (request == null)
                    {
                        var errorResponse = CommandResponse.Fail(StatusCode.InvalidParameters, "Failed to deserialize request");
                        var errorJson = JsonConvert.SerializeObject(errorResponse);
                        MessageFraming.WriteMessage(pipe, errorJson);
                        continue;
                    }

                    // Watch command: streaming path (pipe ownership passes to WatchWriterLoop thread)
                    if (string.Equals(request.command, WellKnownCommands.Watch, StringComparison.OrdinalIgnoreCase))
                    {
                        watchSessionStarted = StartWatchSession(pipe, request);
                        continue;
                    }

                    // Standard request-response path
                    var workItem = new WorkItem();
                    _mainThreadQueue.Enqueue(new PendingWork(request, pipe, workItem));

                    // Wait for main thread to process, shutdown, or safety timeout.
                    var completedTask = Task.WhenAny(
                        workItem.Completion,
                        _shutdownCompletion.Task,
                        Task.Delay(TimeSpan.FromMinutes(10)))
                        .GetAwaiter()
                        .GetResult();

                    if (completedTask != workItem.Completion)
                    {
                        if (completedTask == _shutdownCompletion.Task)
                            workItem.Cancel();
                        else
                            workItem.Cancel("IPC request timed out waiting for Unity main thread.");
                    }

                    var response = workItem.Completion.GetAwaiter().GetResult();
                    if (response != null)
                    {
                        var responseJson = JsonConvert.SerializeObject(response);
                        try
                        {
                            if (pipe.IsConnected)
                                MessageFraming.WriteMessage(pipe, responseJson);
                        }
                        catch (IOException)
                        {
                            // Client disconnected before response — acceptable
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Normal shutdown path — Stop() disposed the pipe
                    break;
                }
                catch (IOException ex)
                {
                    if (!_stopping && IsPipeBusy(ex))
                    {
                        Thread.Sleep(PipeBusyRetryDelayMs);
                        continue;
                    }

                    if (!_stopping)
                        Debug.LogWarning($"[unityctl] IPC connection error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    if (!_stopping)
                        Debug.LogError($"[unityctl] IPC server error: {ex}");
                }
                finally
                {
                    // Pipe ownership passed to WatchWriterLoop — don't dispose here
                    if (!watchSessionStarted)
                    {
                        try { pipe?.Dispose(); } catch { }
                        _currentPipe = null;
                    }
                }
            }
        }

        /// <summary>
        /// Initialises a watch streaming session and starts the writer thread.
        /// Returns true if the thread was started (pipe ownership transferred).
        /// </summary>
        private bool StartWatchSession(NamedPipeServerStream pipe, CommandRequest request)
        {
            // Terminate any existing watch session first
            if (_watchActive)
            {
                _watchActive = false;
                _watchEventSource?.Unsubscribe();
                _watchEventSource = null;
                if (_watchThread != null && _watchThread.IsAlive)
                    _watchThread.Join(1000);
            }

            // Determine channels
            var channelParam = request.GetParam("channel", "all");
            var channels = channelParam.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? new[] { "all" }
                : new[] { channelParam };

            // Clear the queue
            while (_watchQueue.TryDequeue(out _)) { }
            _watchQueueCount = 0;
            _watchDroppedCount = 0;

            // Subscribe to Unity events
            _watchEventSource = new WatchEventSource(EnqueueWatchEvent, channels);
            _watchEventSource.Subscribe();

            // Send handshake response
            try
            {
                var handshake = CommandResponse.Ok("watch session started");
                MessageFraming.WriteMessage(pipe, JsonConvert.SerializeObject(handshake));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[unityctl] Watch handshake failed: {ex.Message}");
                _watchEventSource.Unsubscribe();
                _watchEventSource = null;
                return false;
            }

            _watchPipe = pipe;
            _watchActive = true;

            _watchThread = new Thread(() => WatchWriterLoop(pipe))
            {
                Name = "unityctl-watch",
                IsBackground = true
            };
            _watchThread.Start();
            return true;
        }

        /// <summary>
        /// Enqueue a watch event with bounded-queue overflow handling.
        /// Called from WatchEventSource (potentially background threads).
        /// </summary>
        private void EnqueueWatchEvent(EventEnvelope evt)
        {
            if (!_watchActive) return;

            if (Interlocked.Increment(ref _watchQueueCount) > MaxWatchQueueSize)
            {
                // Drop-oldest strategy
                if (_watchQueue.TryDequeue(out _))
                    Interlocked.Decrement(ref _watchQueueCount);

                int dropped = Interlocked.Increment(ref _watchDroppedCount);
                // Emit _overflow synthetic event every 100 drops
                if (dropped % 100 == 0)
                {
                    var overflow = EventEnvelope.Create("_overflow", "Dropped",
                        new JObject { ["dropped"] = dropped });
                    _watchQueue.Enqueue(overflow);
                    return; // don't double-count
                }
            }

            _watchQueue.Enqueue(evt);
        }

        /// <summary>
        /// Dedicated background thread: drains _watchQueue to the pipe with heartbeat.
        /// Exits when the pipe disconnects, the server stops, or the client sends close.
        /// </summary>
        private void WatchWriterLoop(NamedPipeServerStream pipe)
        {
            long lastHeartbeatMs = (long)Environment.TickCount;

            try
            {
                while (!_stopping && _watchActive && pipe.IsConnected)
                {
                    int sent = 0;
                    while (sent < MaxEventsPerPump && _watchQueue.TryDequeue(out var evt))
                    {
                        Interlocked.Decrement(ref _watchQueueCount);
                        WriteWatchEvent(pipe, evt);
                        sent++;

                        // If we sent _close, end the loop
                        if (evt.channel == "_close") goto cleanup;
                    }

                    long nowMs = (long)Environment.TickCount;
                    if (nowMs - lastHeartbeatMs >= HeartbeatIntervalMs)
                    {
                        WriteWatchEvent(pipe, EventEnvelope.Create("_heartbeat", "Ping"));
                        lastHeartbeatMs = nowMs;
                    }

                    if (sent == 0)
                        Thread.Sleep(WatchWriterPollMs);
                }
            }
            catch (IOException)
            {
                // Client disconnected — normal
            }
            catch (ObjectDisposedException)
            {
                // Server stopped — normal
            }
            catch (Exception ex)
            {
                if (!_stopping)
                    Debug.LogWarning($"[unityctl] Watch writer error: {ex.Message}");
            }

            cleanup:
            _watchActive = false;
            _watchEventSource?.Unsubscribe();
            _watchEventSource = null;
            _watchPipe = null;
            try { pipe.Dispose(); } catch { }
        }

        private static void WriteWatchEvent(NamedPipeServerStream pipe, EventEnvelope evt)
        {
            var json = JsonConvert.SerializeObject(evt);
            MessageFraming.WriteMessage(pipe, json);
        }

        /// <summary>
        /// Pumped every editor frame via EditorApplication.update.
        /// Dequeues pending work and executes command handlers on the main thread.
        /// </summary>
        private void PumpMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                if (_stopping)
                {
                    pending.WorkItem.Cancel();
                    continue;
                }

                try
                {
                    var response = IpcRequestRouter.Route(pending.Request);
                    pending.WorkItem.TryComplete(response);
                }
                catch (Exception ex)
                {
                    pending.WorkItem.TryComplete(CommandResponse.Fail(
                        StatusCode.UnknownError,
                        $"Handler exception: {ex.Message}",
                        new System.Collections.Generic.List<string> { ex.StackTrace }));
                }
            }
        }

        /// <summary>Work item for cross-thread signaling.</summary>
        private sealed class WorkItem
        {
            private readonly TaskCompletionSource<CommandResponse> _completion =
                new TaskCompletionSource<CommandResponse>();

            public Task<CommandResponse> Completion => _completion.Task;

            public bool TryComplete(CommandResponse response)
            {
                return _completion.TrySetResult(response);
            }

            public void Cancel(string message = "IPC server is stopping.")
            {
                _completion.TrySetResult(CommandResponse.Fail(StatusCode.Busy, message));
            }
        }

        /// <summary>Pending work queued for main thread execution.</summary>
        private sealed class PendingWork
        {
            public readonly CommandRequest Request;
            public readonly NamedPipeServerStream Pipe;
            public readonly WorkItem WorkItem;

            public PendingWork(CommandRequest request, NamedPipeServerStream pipe, WorkItem workItem)
            {
                Request = request;
                Pipe = pipe;
                WorkItem = workItem;
            }
        }

        private static TaskCompletionSource<bool> CreateShutdownCompletion()
        {
            return new TaskCompletionSource<bool>();
        }

        private static bool IsPipeBusy(IOException exception)
        {
            return (exception.HResult & 0xFFFF) == ErrorPipeBusy;
        }
    }
}
#endif
