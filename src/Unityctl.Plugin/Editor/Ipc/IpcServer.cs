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
    /// Accepts client connections on a background listener thread and handles each connection independently,
    /// while dispatching command execution back to the Unity main thread via EditorApplication.update.
    /// Singleton — use IpcServer.Instance.
    /// </summary>
    public sealed class IpcServer
    {
        private enum ShutdownMode
        {
            Graceful,
            EditorQuit
        }

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
        private NamedPipeServerStream _listenPipe;
        private readonly object _lock = new object();
        private TaskCompletionSource<bool> _shutdownCompletion = CreateShutdownCompletion();
        private readonly ConcurrentDictionary<int, NamedPipeServerStream> _activePipes =
            new ConcurrentDictionary<int, NamedPipeServerStream>();
        private int _nextPipeId;

        private readonly ConcurrentQueue<PendingWork> _mainThreadQueue = new ConcurrentQueue<PendingWork>();
        private long _lastExpectedConnectionWarningTicks;

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
                if (IsRunning) StopInternal(ShutdownMode.Graceful);

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
                StopInternal(ShutdownMode.Graceful);
            }
        }

        private void StopForEditorQuit()
        {
            lock (_lock)
            {
                StopInternal(ShutdownMode.EditorQuit);
            }
        }

        private void StopInternal(ShutdownMode shutdownMode)
        {
            if (!IsRunning) return;

            var fastExit = shutdownMode == ShutdownMode.EditorQuit;
            _stopping = true;
            _shutdownCompletion.TrySetResult(true);

            EditorApplication.update -= PumpMainThreadQueue;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.quitting -= OnQuitting;

            // Signal watch session to stop
            if (_watchActive)
            {
                if (!fastExit)
                {
                    try
                    {
                        var closeEnvelope = EventEnvelope.Create("_close", "Shutdown");
                        _watchQueue.Enqueue(closeEnvelope);
                    }
                    catch { }
                }
                _watchActive = false;
                _watchEventSource?.Unsubscribe();
                _watchEventSource = null;
            }

            // Dispose current pipe to unblock WaitForConnection
            try { _listenPipe?.Dispose(); } catch { }
            try { _watchPipe?.Dispose(); } catch { }

            foreach (var activePipe in _activePipes.Values)
            {
                try { activePipe.Dispose(); } catch { }
            }

            if (!fastExit)
            {
                if (_listenThread != null && _listenThread.IsAlive)
                    _listenThread.Join(3000);

                // Wait for watch writer thread to finish
                if (_watchThread != null && _watchThread.IsAlive)
                    _watchThread.Join(2000);
            }

            // Cancel and drain remaining queued requests so listener threads do not block.
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                pending.WorkItem.Cancel();
            }

            _listenPipe = null;
            _watchPipe = null;
            _listenThread = null;
            _watchThread = null;
            IsRunning = false;
            if (!fastExit)
                Debug.Log("[unityctl] IPC server stopped");
        }

        private void OnBeforeAssemblyReload()
        {
            Stop();
        }

        private void OnQuitting()
        {
            StopForEditorQuit();
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
        /// Background thread: accepts client connections and hands each connected pipe to a worker.
        /// This keeps at least one server instance listening even while another client is waiting on the main thread.
        /// </summary>
        private void ListenLoop()
        {
            while (!_stopping)
            {
                NamedPipeServerStream pipe = null;
                bool pipeHandedOff = false;
                try
                {
                    pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        MaxServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);

                    _listenPipe = pipe;
                    pipe.WaitForConnection();
                    _listenPipe = null;

                    if (_stopping) break;

                    var pipeId = Interlocked.Increment(ref _nextPipeId);
                    _activePipes[pipeId] = pipe;
                    pipeHandedOff = true;

                    ThreadPool.QueueUserWorkItem(_ => HandleClientConnection(pipe, pipeId));
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

                    if (!_stopping && !IsExpectedConnectionError(ex))
                        Debug.LogWarning($"[unityctl] IPC connection error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    if (!_stopping)
                        Debug.LogError($"[unityctl] IPC server error: {ex}");
                }
                finally
                {
                    if (!pipeHandedOff)
                    {
                        try { pipe?.Dispose(); } catch { }
                        _listenPipe = null;
                    }
                }
            }
        }

        private void HandleClientConnection(NamedPipeServerStream pipe, int pipeId)
        {
            bool watchSessionStarted = false;
            try
            {
                var requestJson = MessageFraming.ReadMessage(pipe);
                var request = JsonConvert.DeserializeObject<CommandRequest>(requestJson);

                if (request == null)
                {
                    var errorResponse = CommandResponse.Fail(StatusCode.InvalidParameters, "Failed to deserialize request");
                    var errorJson = JsonConvert.SerializeObject(errorResponse);
                    MessageFraming.WriteMessage(pipe, errorJson);
                    return;
                }

                if (string.Equals(request.command, WellKnownCommands.Watch, StringComparison.OrdinalIgnoreCase))
                {
                    watchSessionStarted = StartWatchSession(pipe, request);
                    return;
                }

                var workItem = new WorkItem();
                _mainThreadQueue.Enqueue(new PendingWork(request, pipe, workItem));

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
                // Normal shutdown path
            }
            catch (IOException ex)
            {
                if (!_stopping && !IsExpectedConnectionError(ex))
                    Debug.LogWarning($"[unityctl] IPC connection error: {ex.Message}");
            }
            catch (Exception ex)
            {
                if (!_stopping)
                    Debug.LogError($"[unityctl] IPC server error: {ex}");
            }
            finally
            {
                _activePipes.TryRemove(pipeId, out _);

                if (!watchSessionStarted)
                {
                    try { pipe.Dispose(); } catch { }
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

            // Send handshake response first (still on background thread — pipe write is fine)
            try
            {
                var handshake = CommandResponse.Ok("watch session started");
                MessageFraming.WriteMessage(pipe, JsonConvert.SerializeObject(handshake));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[unityctl] Watch handshake failed: {ex.Message}");
                return false;
            }

            _watchPipe = pipe;
            _watchActive = true;

            // Start writer thread (reads from _watchQueue, writes to pipe)
            _watchThread = new Thread(() => WatchWriterLoop(pipe))
            {
                Name = "unityctl-watch",
                IsBackground = true
            };
            _watchThread.Start();

            // Subscribe to Unity events on the MAIN thread
            // (Application.logMessageReceivedThreaded requires main-thread subscription)
            var capturedChannels = channels;
            EditorApplication.delayCall += () =>
            {
                if (!_watchActive) return;
                _watchEventSource = new WatchEventSource(EnqueueWatchEvent, capturedChannels);
                _watchEventSource.Subscribe();
            };

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
            // Send immediate heartbeat so client knows connection is alive
            try
            {
                WriteWatchEvent(pipe, EventEnvelope.Create("_heartbeat", "Ping"));
            }
            catch
            {
                goto cleanup;
            }

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

        private bool IsExpectedConnectionError(IOException exception)
        {
            var message = exception.Message ?? string.Empty;
            var expected = message.IndexOf("pipe is broken", StringComparison.OrdinalIgnoreCase) >= 0
                           || message.IndexOf("pipe is being closed", StringComparison.OrdinalIgnoreCase) >= 0
                           || message.IndexOf("connection has been ended", StringComparison.OrdinalIgnoreCase) >= 0
                           || message.IndexOf("connection was aborted", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!expected)
                return false;

            var nowTicks = DateTime.UtcNow.Ticks;
            var previousTicks = Interlocked.Read(ref _lastExpectedConnectionWarningTicks);
            if (nowTicks - previousTicks < TimeSpan.FromSeconds(30).Ticks)
                return true;

            Interlocked.Exchange(ref _lastExpectedConnectionWarningTicks, nowTicks);
            return true;
        }
    }
}
#endif
