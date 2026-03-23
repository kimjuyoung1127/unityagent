#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ProfilerGetStatsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ProfilerGetStats;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var isPlaying = EditorApplication.isPlaying;

            var data = new JObject
            {
                ["isPlaying"] = isPlaying,
                ["profilerEnabled"] = Profiler.enabled,
                ["totalAllocatedMemoryMB"] = Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0),
                ["totalReservedMemoryMB"] = Profiler.GetTotalReservedMemoryLong() / (1024.0 * 1024.0),
                ["totalUnusedReservedMemoryMB"] = Profiler.GetTotalUnusedReservedMemoryLong() / (1024.0 * 1024.0),
                ["monoUsedSizeMB"] = Profiler.GetMonoUsedSizeLong() / (1024.0 * 1024.0),
                ["monoHeapSizeMB"] = Profiler.GetMonoHeapSizeLong() / (1024.0 * 1024.0)
            };

            string message;
            if (isPlaying)
            {
                // Rendering stats — only meaningful in Play Mode
                var dt = Time.deltaTime;
                if (dt > 0f)
                {
                    data["fps"] = 1.0f / dt;
                    data["frameTimeMs"] = dt * 1000f;
                }
                data["smoothDeltaTimeMs"] = Time.smoothDeltaTime * 1000f;
                data["batches"] = UnityStats.batches;
                data["drawCalls"] = UnityStats.drawCalls;
                data["triangles"] = UnityStats.triangles;
                data["vertices"] = UnityStats.vertices;
                data["setPassCalls"] = UnityStats.setPassCalls;

                message = "Profiler stats (Play Mode — all stats valid)";
            }
            else
            {
                data["message"] = "Editor Mode: only memory stats are valid. Enter Play Mode for rendering stats.";
                message = "Profiler stats (Editor Mode — memory only)";
            }

            // Detailed stats — GC and graphics driver memory
            var detailed = request.GetParam<bool>("detailed");
            if (detailed)
            {
                data["graphicsDriverMemoryMB"] = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024.0 * 1024.0);
                data["gcCollectionCount0"] = System.GC.CollectionCount(0);
                data["gcCollectionCount1"] = System.GC.CollectionCount(1);
                data["gcCollectionCount2"] = System.GC.CollectionCount(2);
                data["gcTotalMemoryMB"] = System.GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            }

            return Ok(message, data);
        }
    }
}
#endif
