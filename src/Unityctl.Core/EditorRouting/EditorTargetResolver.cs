using Unityctl.Core.Discovery;
using Unityctl.Shared;

namespace Unityctl.Core.EditorRouting;

public sealed class EditorTargetResolver
{
    private readonly UnityEditorDiscovery _discovery;
    private readonly UnityProcessDetector _processDetector;

    public EditorTargetResolver(UnityEditorDiscovery discovery, UnityProcessDetector? processDetector = null)
    {
        _discovery = discovery;
        _processDetector = processDetector ?? new UnityProcessDetector(Platform.PlatformFactory.Create());
    }

    public EditorTargetInfo Resolve(string projectPath)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var editor = _discovery.FindEditorForProject(fullProjectPath);
        var process = _processDetector.FindProcessForProject(fullProjectPath);

        return new EditorTargetInfo
        {
            ProjectPath = Constants.NormalizeProjectPath(fullProjectPath),
            PipeName = Constants.GetPipeName(fullProjectPath),
            EditorVersion = editor?.Version,
            EditorLocation = editor?.Location,
            UnityPid = process?.ProcessId,
            IsRunning = process != null
        };
    }
}
