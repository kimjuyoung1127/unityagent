using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Unityctl.Mcp.Prompts;

[McpServerPromptType]
internal sealed class GameWorkflowPrompts
{
    [McpServerPrompt(Name = "create_game_scene")]
    [Description("Step-by-step workflow for creating a complete game scene from scratch")]
    public static string CreateGameScene()
    {
        return """
            You are building a Unity game scene using unityctl MCP tools.
            Follow this exact order to avoid errors:

            ## Phase 1: Connect & Verify
            1. unityctl_ping — confirm Editor is reachable
            2. unityctl_status — check compilation state is Ready

            ## Phase 2: Scene Setup
            3. unityctl_run(command="scene-create") — create a new scene (requires --project)
            4. unityctl_run(command="scene-save") — save immediately to establish the file

            ## Phase 3: Environment
            5. unityctl_run(command="mesh-create-primitive") — create floor (Plane), walls (Cube), etc.
            6. unityctl_run(command="gameobject-create") — create empty GameObjects for organization
            7. unityctl_run(command="lighting-set-settings") — configure lighting if needed

            ## Phase 4: Game Objects
            8. unityctl_run(command="mesh-create-primitive") — create player, enemies, items
            9. unityctl_run(command="component-add") — add Rigidbody, Collider, etc.
            10. unityctl_run(command="component-set-property") — configure component values

            ## Phase 5: Scripts
            11. unityctl_run(command="script-create") — create C# scripts
            12. unityctl_run(command="script-edit") — write script logic
            13. unityctl_run(command="script-validate") — wait for compilation
            14. unityctl_query(command="script-get-errors") — check for compile errors
            15. unityctl_run(command="component-add") — attach scripts to GameObjects

            ## Phase 6: Verify
            16. unityctl_run(command="scene-save") — save all changes
            17. unityctl_query(command="project-validate") — run 6-point validation
            18. unityctl_query(command="scene-hierarchy") — confirm final structure
            19. unityctl_query(command="scene-snapshot") — take a snapshot for reference

            ## Phase 7: Test
            20. unityctl_run(command="play-start") — enter Play Mode
            21. unityctl_query(command="screenshot", parameters={"view":"game"}) — capture game view
            22. unityctl_run(command="play-stop") — exit Play Mode

            ## Important Rules
            - Always use --project for scene-create (editor select fallback not supported)
            - Always run script-validate after script-create/edit before adding as component
            - Use scene-snapshot between major phases to track progress
            - If anything fails, run unityctl_query(command="doctor") to diagnose
            - workflow verify uses "kind" field (not "type") for step definitions
            - After importing URP/HDRP packages, wait briefly for domain reload before IPC calls
            """;
    }

    [McpServerPrompt(Name = "debug_game")]
    [Description("Systematic debugging workflow when something goes wrong in Unity")]
    public static string DebugGame()
    {
        return """
            You are debugging a Unity project using unityctl MCP tools.
            Follow this systematic approach:

            ## Step 1: Diagnose
            1. unityctl_status — is the Editor ready or stuck?
            2. unityctl_ping — is IPC working?
            3. unityctl_query(command="doctor") — full diagnostic report

            ## Step 2: Check Compilation
            4. unityctl_query(command="script-get-errors") — any compile errors?
            5. If errors exist: unityctl_run(command="script-validate", parameters={"wait":true})

            ## Step 3: Check Console
            6. unityctl_query(command="console-get-count") — error/warning counts
            7. unityctl_log(level="error") — recent error history

            ## Step 4: Check Scene State
            8. unityctl_query(command="scene-hierarchy") — is the scene structure correct?
            9. unityctl_query(command="scene-snapshot") — detailed state with components

            ## Step 5: Check Runtime
            10. unityctl_run(command="play-start") — enter Play Mode
            11. unityctl_query(command="profiler-get-stats") — performance issues?
            12. unityctl_query(command="screenshot", parameters={"view":"game"}) — visual check
            13. unityctl_run(command="play-stop")

            ## Common Issues
            - "Unknown command" → Plugin needs domain reload (restart Unity Editor)
            - StatusCode 201 (ProjectLocked) → Another Unity instance has the project
            - StatusCode 203 (PluginNotInstalled) → Run unityctl_run(command="init")
            - Compile errors after script edit → Always run script-validate --wait
            """;
    }

    [McpServerPrompt(Name = "iterate_gameplay")]
    [Description("Rapid iteration workflow: modify, validate, test, capture")]
    public static string IterateGameplay()
    {
        return """
            You are iterating on gameplay using unityctl MCP tools.
            This is a tight loop for rapid changes:

            ## Before Changes
            1. unityctl_query(command="scene-snapshot") — baseline state

            ## Make Changes
            2. Edit scripts: unityctl_run(command="script-edit") or unityctl_run(command="script-patch")
            3. Modify objects: unityctl_run(command="component-set-property")
            4. Add objects: unityctl_run(command="mesh-create-primitive") or gameobject-create

            ## Validate
            5. unityctl_run(command="script-validate") — wait for compilation
            6. unityctl_query(command="script-get-errors") — zero errors?
            7. unityctl_run(command="scene-save")

            ## Test
            8. unityctl_run(command="play-start")
            9. unityctl_query(command="screenshot", parameters={"view":"game"}) — visual check
            10. unityctl_query(command="profiler-get-stats") — performance OK?
            11. unityctl_run(command="play-stop")

            ## Compare
            12. unityctl_query(command="scene-diff", parameters={"live":true}) — what changed?

            ## If Something Broke
            13. unityctl_run(command="undo") — revert last change
            14. Repeat from step 2

            ## Tips
            - Use script-patch for small edits (line-level insert/delete/replace)
            - Use script-edit for full file rewrites
            - batch-execute can combine multiple changes with auto-rollback on failure
            - Take snapshots frequently — they're cheap and saved to ~/.unityctl/snapshots/
            """;
    }

    [McpServerPrompt(Name = "setup_project")]
    [Description("Initial project setup: install plugin, verify connection, configure")]
    public static string SetupProject()
    {
        return """
            You are setting up unityctl for a Unity project.

            ## Step 1: Check Unity
            1. unityctl_query(command="editor-list") — find installed Unity editors
            2. Confirm Unity Editor is open with the target project

            ## Step 2: Install Plugin
            3. unityctl_run(command="init") with parameters:
               - project: "/path/to/unity/project"
               - source: "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.3.4"
            4. Restart Unity Editor (or wait for domain reload)

            ## Step 3: Verify
            5. unityctl_ping — should return "pong"
            6. unityctl_status — should show "Ready"
            7. unityctl_query(command="doctor") — full health check

            ## Step 4: Pin Project (Optional)
            8. unityctl_run(command="editor-select", parameters={"project":"/path/to/project"})
            9. Now ping/status/check/doctor can omit --project

            ## Troubleshooting
            - If ping fails: check Unity Editor is running and not compiling
            - If "PluginNotInstalled": re-run init, then restart Unity
            - If "ProjectLocked": close other Unity instances using this project
            - After URP/HDRP import, IPC may briefly disconnect during reload — wait and retry
            """;
    }
}
