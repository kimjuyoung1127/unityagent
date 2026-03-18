#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Subscribes to Unity Editor events and forwards them to the watch event queue.
    /// Designed for on-demand subscription when a watch session is active.
    /// </summary>
    internal sealed class WatchEventSource
    {
        // Re-entrancy guard per thread (logMessageReceivedThreaded fires on background threads)
        [ThreadStatic]
        private static bool _inHandler;

        private readonly Action<EventEnvelope> _enqueue;
        private readonly HashSet<string> _channels;
        private bool _subscribed;

        private Application.LogCallback _logCallback;
        private Action _hierarchyCallback;
        private Action<object> _compilationStartedCallback;
        private Action<object> _compilationFinishedCallback;

        public WatchEventSource(Action<EventEnvelope> enqueue, IEnumerable<string> channels)
        {
            _enqueue = enqueue ?? throw new ArgumentNullException(nameof(enqueue));
            _channels = new HashSet<string>(channels, StringComparer.OrdinalIgnoreCase);
        }

        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            if (WatchesChannel("console"))
            {
                _logCallback = OnLogReceived;
                Application.logMessageReceivedThreaded += _logCallback;
            }

            if (WatchesChannel("hierarchy"))
            {
                _hierarchyCallback = OnHierarchyChanged;
                EditorApplication.hierarchyChanged += _hierarchyCallback;
            }

            if (WatchesChannel("compilation"))
            {
                _compilationStartedCallback = OnCompilationStarted;
                _compilationFinishedCallback = OnCompilationFinished;
                CompilationPipeline.compilationStarted += _compilationStartedCallback;
                CompilationPipeline.compilationFinished += _compilationFinishedCallback;
            }
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            if (_logCallback != null)
            {
                Application.logMessageReceivedThreaded -= _logCallback;
                _logCallback = null;
            }

            if (_hierarchyCallback != null)
            {
                EditorApplication.hierarchyChanged -= _hierarchyCallback;
                _hierarchyCallback = null;
            }

            if (_compilationStartedCallback != null)
            {
                CompilationPipeline.compilationStarted -= _compilationStartedCallback;
                _compilationStartedCallback = null;
            }

            if (_compilationFinishedCallback != null)
            {
                CompilationPipeline.compilationFinished -= _compilationFinishedCallback;
                _compilationFinishedCallback = null;
            }
        }

        // ─── Event Handlers ────────────────────────────────────────────────────

        // Called on background threads — MUST NOT call Unity APIs
        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_inHandler) return; // re-entrancy guard
            _inHandler = true;
            try
            {
                var eventType = type switch
                {
                    LogType.Error => "Error",
                    LogType.Assert => "Assert",
                    LogType.Warning => "Warning",
                    LogType.Log => "Log",
                    LogType.Exception => "Exception",
                    _ => "Log"
                };

                var payload = new JObject
                {
                    ["message"] = condition,
                    ["stackTrace"] = stackTrace
                };

                _enqueue(EventEnvelope.Create("console", eventType, payload));
            }
            finally
            {
                _inHandler = false;
            }
        }

        // Called on main thread — Unity APIs are safe here
        private void OnHierarchyChanged()
        {
            _enqueue(EventEnvelope.Create("hierarchy", "Changed"));
        }

        private void OnCompilationStarted(object obj)
        {
            _enqueue(EventEnvelope.Create("compilation", "Started"));
        }

        private void OnCompilationFinished(object obj)
        {
            _enqueue(EventEnvelope.Create("compilation", "Finished"));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────

        private bool WatchesChannel(string channel)
        {
            return _channels.Contains("all") || _channels.Contains(channel);
        }
    }
}
#endif
