// ★ AUTO-COPIED from Unityctl.Shared — do not edit directly
// Source: src/Unityctl.Shared/Protocol/StatusCode.cs
namespace Unityctl.Plugin.Editor.Shared
{
    public enum StatusCode
    {
        Ready = 0,
        Compiling = 100,
        Reloading = 101,
        EnteringPlayMode = 102,
        Busy = 103,
        NotFound = 200,
        ProjectLocked = 201,
        LicenseError = 202,
        PluginNotInstalled = 203,
        UnknownError = 500,
        CommandNotFound = 501,
        InvalidParameters = 502,
        BuildFailed = 503,
        TestFailed = 504
    }
}
