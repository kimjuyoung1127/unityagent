# Showcase Roadmap

This guide answers a practical question for `unityctl`:

> "If we start from zero, what kind of Unity game should we build first so people immediately understand why this tool matters?"

The short answer is:

- Do **not** start with a Minecraft-like sandbox.
- Start with a **small 3D survival / base-defense game** that can be built from primitives, scripts, prefabs, UI, and validation loops.
- Treat Minecraft-like scope as a **later systems milestone**, not the first public proof.

---

## Recommended Showcase Ladder

### Stage 1 — First Public Proof

Build a **small 3D arena microgame**:

- one playable scene
- one player controller
- one enemy type
- one pickup type
- one HUD
- one restart loop
- one short build-and-verify pipeline

Recommended theme:

- **Top-down survival arena**
- or **small base-defense room**

Why this stage works:

- easy to explain in one GIF
- visually readable even with primitive blockout art
- exercises `scene`, `gameobject`, `component`, `mesh`, `script`, `ui`, `project validate`, `workflow verify`, and `batch execute`
- small enough to finish quickly, but real enough to prove the tool

### Stage 2 — Polished Vertical Slice

Expand Stage 1 into a **small survival / base-defense vertical slice**:

- 2 to 3 scenes
- enemy spawning waves
- prefab-based content reuse
- simple inventory or upgrade choice
- NavMesh-driven enemies
- basic lighting pass
- build profile validation
- artifact-based verification bundle

This is the best "people will remember this" tier for `unityctl`.
It shows that the tool is not just making cubes. It is driving an actual development loop.

### Stage 3 — Systems Showcase

Only after Stage 2 is solid, move into a **sandbox-lite prototype**:

- tile or chunk grid
- block placement and removal
- save/load
- procedural layout generation
- simple crafting
- larger world traversal

This is the right bridge toward "Minecraft-like" thinking.
It proves systems design without committing to full voxel-world complexity too early.

### Stage 4 — Minecraft-like Ambition

Treat this as a long-term showcase, not the first milestone:

- voxel mesh generation
- chunk streaming
- world serialization
- procedural terrain
- inventory and crafting trees
- performance optimization
- camera polish
- content pipeline scale

This is attractive, but it depends on many systems that are not the shortest path to proving `unityctl`.

---

## Best First Game for `unityctl`

If we choose one direction now, the strongest option is:

### `Project: Command Arena`

A **small 3D top-down survival / base-defense prototype** built in Unity 6 URP.

Core loop:

- player moves in a compact arena
- enemies spawn and chase
- player survives waves or protects one core object
- pickups or upgrades appear between waves
- HUD shows health, wave, and score

Why this is the best fit:

- blockout-friendly from primitive meshes
- readable with low-cost art
- demonstrates scene construction from zero
- demonstrates script creation and patch/fix loops
- demonstrates tags, layers, physics, and NavMesh
- demonstrates UI inspection and deterministic UI state changes
- demonstrates verification artifacts, screenshot capture, and build-readiness checks

It is a much better first proof than:

- open-world survival
- Minecraft clone
- cinematic third-person action
- terrain-heavy exploration game

Those ideas are appealing, but they depend too heavily on terrain tooling, camera tooling, content volume, and performance work.

---

## Capability-to-Game Mapping

What `unityctl` can already show well:

| Capability | Good Showcase Use |
|---|---|
| `scene create/open/save`, `gameobject create`, `component add/set-property` | build arena, spawners, pickups, hazards |
| `mesh create-primitive` | fast blockout of floor, walls, obstacles, towers |
| `script create/edit/patch/validate/get-errors` | movement, enemy AI, wave manager, pickups |
| `ui canvas-create`, `ui element-create`, `ui find/get`, `ui toggle/input` | HUD, settings screen, debug UI |
| `project validate`, `doctor`, `check`, `build --dry-run` | prove readiness after each milestone |
| `workflow verify`, `screenshot`, `scene diff`, `log`, `watch` | artifact-first evidence for README/GIF/CI |
| `tag`, `layer`, `physics`, `lighting`, `navmesh` | collision setup, lighting pass, enemy navigation |
| `prefab`, `material`, `asset` commands | repeatable content authoring and reuse |

What is better left for later:

| Area | Why It Should Wait |
|---|---|
| Minecraft-scale voxel world | too much engine/system work for first proof |
| terrain-heavy worlds | current early showcase does not need terrain APIs |
| camera-heavy cinematics | current strength is validation, not cinematic tooling |
| click-perfect runtime UI automation | current UI strength is deterministic state set, not full user simulation |

---

## Asset Checklist

Prepare these before starting the actual showcase build.

### Minimum Asset Pack

- URP 3D template project
- one readable font for HUD
- one icon pack for health, score, wave, and buttons
- one simple low-poly material palette
- 3 to 5 simple sound effects
- one short looped background track
- one enemy model or a primitive-based enemy style
- one pickup visual style

### Strongly Recommended

- one stylized skybox
- one VFX pack for hit flash, pickup glow, and spawn burst
- one small decal or surface texture pack
- one low-poly environment pack for props
- one animation source for enemy idle / move / hit

### Verification Assets

- one `verify.json` bundle for editor-state checks
- one `play-smoke.json` bundle for play-mode smoke checks
- baseline screenshots for key scenes
- a known-good build target and output path
- a short list of expected UI assertions

---

## Pre-Production Plan for 100% `unityctl` Usage

To get the most out of `unityctl`, prepare the project around the tool instead of treating the tool as an afterthought.

### 1. Lock the Environment First

- use **Unity 6000.0.64f1** for the first showcase
- use **one named project path** and keep scenes saved early
- prefer **running Editor + IPC ready** for the first public demo workflow
- keep the plugin source pinned and documented

Why:

- current validation history is strongest on Unity 6000.0.64f1
- saved scenes make targeting and verification more stable than unsaved scratch scenes
- current headless closed-editor flows are not yet the best "wow" path for a first public demo

### 2. Create a Project Structure That Matches the Commands

Recommended folders:

```text
Assets/
  Art/
    Materials/
    Models/
    UI/
    VFX/
  Audio/
    Music/
    Sfx/
  Data/
    Config/
  Prefabs/
    Enemies/
    Pickups/
    Props/
    UI/
  Scenes/
    Boot.unity
    Arena.unity
    Menu.unity
  Scripts/
    Gameplay/
    UI/
    Core/
  Tests/
    EditMode/
    PlayMode/
```

Why:

- this maps cleanly to `asset`, `prefab`, `script`, `scene`, `shader`, `texture`, and `scriptableobject` commands
- it makes README examples and automation examples easier to follow

### 3. Design the Game Around Verifiable Milestones

Milestone order:

1. project boots and `project validate` passes
2. arena scene blockout exists
3. player moves
4. one enemy can spawn and chase
5. damage / death / restart loop works
6. HUD updates correctly
7. wave loop works
8. build dry-run passes
9. verification bundle produces reusable artifacts

Each milestone should end with:

- one `workflow verify` run
- one screenshot artifact
- one short README-worthy note or GIF candidate

### 4. Keep the First Version Primitive-Friendly

For the first playable:

- use primitives for floors, walls, towers, pickups, and blockers
- use simple materials instead of custom art dependencies
- use one clear color language for teams and hazards

Why:

- `mesh create-primitive` is already a strength
- this lets the README show "built from zero" instead of "assembled from a purchased art pack"

### 5. Prepare Reusable Automation Inputs

Before large-scale building, prepare:

- `batch` command files for repeated scene setup
- `workflow` files for milestone verification
- baseline screenshots for diffing
- one script patching playbook for common compile-fix loops

This turns the README from "tool list" into "repeatable system."

---

## Suggested Milestone Plan

### Milestone A — Zero to Playable

Goal:

- a 60-second playable prototype

Features:

- player movement
- enemy chase
- damage
- restart
- HUD

Proof to capture:

- scene hierarchy output
- screenshot artifact
- `project validate`
- `script get-errors`
- `build --dry-run`

### Milestone B — Public Demo Slice

Goal:

- something worth recording into a trailer or GIF thread

Features:

- wave progression
- pickups or upgrades
- prefab reuse
- improved lighting
- improved UI
- play smoke verification

Proof to capture:

- `workflow verify`
- image diff baseline
- profiler stats
- build output summary

### Milestone C — Systems Expansion

Goal:

- prove `unityctl` can drive more than a toy project

Features:

- data-driven enemy configs
- simple save/load
- procedural room variants
- multiple scenes
- light content pipeline automation

Proof to capture:

- ScriptableObject workflow
- asset dependency graph
- prefab export or packaging path
- multi-scene verification bundle

---

## Improvements Worth Making Before a Minecraft-like Demo

These are the highest-value gaps to close before committing to a sandbox-heavy design.

### Must-Have

- **headless closed-editor reliability**
  - current integration tests still show failures around structured responses in the closed-editor batch path
- **command catalog sync guard**
  - current working tree has catalog drift during command expansion
- **stable sample showcase project**
  - a reproducible demo project should ship with the repo or docs

### Very Helpful

- **camera write commands**
  - current camera read support is useful, but fuller camera control would help game-feel iteration
- **Cinemachine support**
  - important for polished movement and presentation
- **runtime UI click helpers**
  - current UI control is strong for deterministic state changes, but not yet full input simulation
- **renderer feature and URP/HDRP workflow coverage**
  - useful once the demo graduates from prototype visuals to polished visuals

### Sandbox-Specific

- **terrain / chunk / voxel helpers**
  - not necessary for the first showcase, but highly relevant for Minecraft-like ambitions
- **performance and streaming workflows**
  - chunked worlds need stronger profiling and regression checks
- **save/load verification patterns**
  - sandbox projects live or die on persistence correctness

---

## Decision Summary

If the goal is to make people say, "this tool can actually build games," the right path is:

1. start with a **small 3D survival / base-defense prototype**
2. prove it with **verification artifacts and repeatable workflows**
3. expand into a **polished vertical slice**
4. only then attempt a **Minecraft-like sandbox**

That sequence maximizes the visible strengths of `unityctl` and avoids getting buried in systems complexity before the tool has had a chance to make a strong first impression.
