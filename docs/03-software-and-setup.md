# Software and setup

Checked: 16 September 2026. Recommendation assumes a stylised Windows PC game made by a very small team.

## Download now

| Software | Purpose | Recommendation |
| --- | --- | --- |
| [Godot Engine for Windows](https://godotengine.org/download/windows/) | Runs the game: input, physics, world, UI, sound, saves and builds | Standard stable build; the official page currently lists 4.7.2. Select the architecture matching the PC, normally x86_64. |
| [Blender](https://www.blender.org/download/) | Creates cars, buildings, props, UV layouts and animations | Continue the download already in progress; record the installed version when setting up the first asset. |
| Git | Records project history and makes experiments recoverable | Already found at C:\Program Files\Git\cmd\git.exe; no further download needed. |

Use **GDScript** initially. It comes with Godot and avoids an additional language toolchain. The standard build is sufficient for this plan; the .NET build is for a C# workflow and requires the .NET SDK. Godot is distributed as a self-contained download. [Official Windows download instructions](https://godotengine.org/download/windows/).

Godot is free and MIT-licensed, with no engine royalty. Its licence has notice requirements when distributing the engine with a game. [Official licence](https://godotengine.org/license/). Blender is free and open source; Godot's asset documentation also describes its integration. [Godot's Blender import documentation](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).

## Add when needed

| Software | Use | When |
| --- | --- | --- |
| [Krita](https://krita.org/en/download/) | Paint textures, decals, signs and interface illustrations | When flat colours stop being sufficient; the official download is free |
| [Audacity](https://www.audacityteam.org/download/) | Edit and trim engine sounds, effects and ambience | When we replace placeholder sound |
| Godot export templates | Package a Windows build | Install templates matching the selected Godot version when making the first external playtest build |

Godot's own script editor is enough to start. A separate code editor is optional. No paid vehicle package, terrain plug-in, modelling subscription or sound middleware is needed for the first slice.

## Why this engine

This is a project-specific judgment: Godot offers a manageable starting point for a compact, stylised 3D game and keeps our systems editable in straightforward project files. GDScript, reusable scenes and a Blender asset pipeline fit the proposed workflow. The engine recommendation does not imply that vehicle handling or a populated world will be automatic.

| Engine | Why we might choose it | Trade-off for this project |
| --- | --- | --- |
| **Godot — recommended** | Small initial toolchain, editable scene/script workflow, open-source engine, direct fit with the proposed prototype | We must build and tune the arcade controller; closed-console distribution needs a separate porting route |
| **Unity** | A reasonable alternative if we prefer C# or identify a specific useful asset/tool package | More tooling and package decisions; current Personal eligibility is subject to revenue/funding limits |
| **Unreal Engine** | Worth considering if the goal changes toward ambitious visual fidelity or a Blueprint-heavy workflow | My assessment is that its broader production workflow adds unnecessary scope for this initial retro-style slice |

Unity's current Personal page specifies eligibility below US$200,000 in revenue and funds raised in the preceding 12 months. Unreal's standard game licensing page describes royalties above US$1 million in lifetime gross product revenue, with exceptions. These are brief comparisons, not a full licensing analysis. [Unity Personal](https://unity.com/products/unity-personal), [Unreal licensing](https://www.unrealengine.com/license).

Godot console exports are not a standard one-click extension of the desktop workflow; platform approval and a suitable porting arrangement are required. Revisit engine choice early if consoles become the primary goal. [Godot console support](https://godotengine.org/consoles/).

## Blender → Godot workflow

Keep editable `.blend` files under `source-art/`. Export runtime models to `.glb` under the future `game/assets/` directory. Godot recommends glTF and supports both `.gltf` and `.glb`; direct `.blend` import also works by invoking Blender. Explicit exports are our initial project convention. [Official format documentation](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).

For the first car, our proposed conventions are:

1. Use a consistent scale, initially one engine unit to one metre; exaggerate proportions deliberately.
2. Keep the body and four wheels separate, with wheel pivots at their centres.
3. Apply intended object transforms and test a small export before detailing the model.
4. Start with simple colours/materials and simple collision geometry in Godot.
5. Check orientation, scale, shading and pivots inside the engine.
6. Add textured detail only after the car works on the driving course.

These conventions are implementation choices. Do not spend hours on a detailed model before the basic import test passes.

## Setup sequence

1. Finish installing Blender.
2. Download and extract the current stable standard Godot build for the PC architecture.
3. Start with Godot's Forward+ renderer on the verified RTX 4070 Ti SUPER system. Keep the initial art simple and test performance; use Compatibility when targeting older machines if required.
4. Create the initial Godot project inside `game/`, recording the exact engine version. The current delivery contains planning files only.
5. Build a primitive driving test course and test both keyboard and controller if one is available.
6. Export one simple Blender car and verify it in Godot.
7. Install matching export templates and produce a Windows build.

Godot's download page lists Vulkan-compatible hardware as recommended and OpenGL 3.3 / OpenGL ES 3.0 as the minimum graphics API baseline. These are engine requirements, not a performance guarantee for our eventual game. [Official requirements](https://godotengine.org/download/windows/).

## Verified development PC

Read-only checks on 16 September 2026 returned:

| Component | Result |
| --- | --- |
| CPU | Intel Core i7-14700KF; 20 cores, 28 logical processors |
| GPU | NVIDIA GeForce RTX 4070 Ti SUPER |
| Dedicated GPU memory | 16,376 MiB reported by NVIDIA tools, approximately 16 GB |
| System memory | 31.82 GiB usable, consistent with a 32 GB configuration |
| OS | Windows 11 Home, 64-bit |
| C: free space | Approximately 526 GB at the time of checking |

These results support using this machine for the proposed Godot/Blender workflow. Hardware does not remove the need to optimise or test on less powerful target PCs. No hardware purchase is recommended for this prototype. The initial performance target is 1080p/60 fps, not a benchmark result.

## What has and has not been checked locally

Git is available on the command path. Blender 5.2.2 LTS at `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` has generated and exported the original car. Godot 4.7.2 at `C:\Users\cripp\Documents\Godot` has imported it, run scripted checks and rendered screenshots through Forward+ on the RTX 4070 Ti SUPER. The assistant also downloaded a portable workspace engine copy. No paid tools have been purchased. See [the tooling guide](08-tools-and-optional-blender-mcp.md).
