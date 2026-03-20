using System.Text.Json;
using Unityctl.Shared;

namespace Unityctl.Core.EditorRouting;

public sealed class EditorSelectionStore
{
    private const string SelectionFileName = "editor-selection.json";
    private readonly string _filePath;

    public EditorSelectionStore(string? configDirectory = null)
    {
        var baseDirectory = configDirectory ?? Constants.GetConfigDirectory();
        _filePath = Path.Combine(baseDirectory, SelectionFileName);
    }

    public string FilePath => _filePath;

    public EditorSelection? GetCurrent()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize(json, EditorSelectionJsonContext.Default.EditorSelection);
        }
        catch
        {
            return null;
        }
    }

    public void SaveProject(string projectPath)
        => SaveSelection(projectPath, null, "project");

    public void SaveProcess(string projectPath, int unityPid)
        => SaveSelection(projectPath, unityPid, "pid");

    public void SaveSelection(string projectPath, int? unityPid, string? selectionMode)
    {
        var selection = new EditorSelection
        {
            ProjectPath = Constants.NormalizeProjectPath(projectPath),
            UnityPid = unityPid,
            SelectionMode = selectionMode,
            SelectedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(
            _filePath,
            JsonSerializer.Serialize(selection, EditorSelectionJsonContext.Default.EditorSelection));
    }
}
