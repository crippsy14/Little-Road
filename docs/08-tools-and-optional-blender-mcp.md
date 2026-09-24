# Creation, testing and optional Blender MCP

Checked on 16 September 2026.

## Required workflow

**Blender MCP is optional.** Blender 5.2.2 LTS is installed at `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`. A background Python invocation succeeded and confirmed that glTF export is available. We can generate/edit assets with Blender Python, save editable `.blend` files and export `.glb` assets directly.

This gives us a reproducible asset pipeline without requiring a live editor connection. MCP becomes useful if we want interactive scene inspection and edits in an already-open Blender session.

**Godot is now available.** The user installed the standard Windows x86_64 Godot 4.7.2 build at `C:\Users\cripp\Documents\Godot`; its console executable has passed import, headless tests and Forward+ Vulkan rendering on the RTX 4070 Ti SUPER. No further engine download is needed. A separate portable workspace copy also exists for tooling. Official downloads remain available from [Godot](https://godotengine.org/download/windows/).

The matching official Windows export templates have also been downloaded into workspace tooling, and a standalone executable has been exported and launched. No further installation is needed on this PC. For rebuilding the source archive on another PC, install Godot 4.7.2 export templates through the editor's export-template manager and clear the preset's local custom-template paths. Git is already installed. Krita and Audacity can wait until texture and audio work requires them.

## Testing tools

| Need | Tool or approach | Additional installation |
| --- | --- | --- |
| Import and script checks | Godot command-line editor, import and headless script runs | Godot only |
| Progression and save tests | Small project-specific GDScript test runner with failure exit codes | None initially |
| Runtime visual review | Rendered game runs; save viewport captures after frames have rendered | None beyond Godot |
| Performance review | Godot profiler and frame-time logging | None |
| Windows build verification | Matching export templates, then launch the exported game | Export templates |
| Asset validation | Blender background scripts and Godot import checks | Blender already works |
| Human handling feedback | Keyboard and an available gamepad | Controller useful, not a software dependency |

Headless tests are useful for logic but do not validate rendered appearance. Screenshots must come from a run with rendering enabled. [Godot command-line documentation](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html).

No testing MCP or paid automation service is required for the initial workflow. The visual review loop is specified in [the goal document](07-goal-and-quality-loop.md).

## Optional live Blender connection

The community [MCP for Blender project](https://github.com/ahujasid/mcp-for-blender) supplies the Blender add-on and MCP server. It is a third-party integration. Its current package name is `mcp-for-blender`.

If you want it later:

1. Install `uv` using its [official Windows instructions](https://docs.astral.sh/uv/getting-started/installation/), then reopen PowerShell and verify `uvx --version`.
2. Run:

```powershell
uvx mcp-for-blender install-addon
codex mcp add blender -- uvx mcp-for-blender
```

3. In Blender, open Edit → Preferences → Add-ons and enable **Interface: MCP for Blender**. Restart Blender if it is not listed.
4. In the 3D viewport press **N**, open the MCP for Blender panel and choose **Start MCP Server**.
5. Restart Codex to load the connection, then verify `codex mcp list`. Keep Blender running for live scene commands.

These steps follow the [integration's setup instructions](https://github.com/ahujasid/mcp-for-blender) and the [official Codex MCP registration syntax](https://learn.chatgpt.com/docs/extend/mcp?surface=cli). The add-on has not been installed or tested with this local Blender version; the direct Python workflow has been tested.

If Codex cannot find `uvx`, locate it with `(Get-Command uvx).Source` and use that absolute executable path in the server command. No paid asset-generation service or external asset account is required for our own models.

## Local verification record

The successful smoke check was:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python-expr "import bpy; print('LITTLE_ROAD_BLENDER_OK', bpy.app.version_string); print('GLTF_EXPORT_AVAILABLE', hasattr(bpy.ops.export_scene, 'gltf'))"
```

It returned `LITTLE_ROAD_BLENDER_OK 5.2.2 LTS` and `GLTF_EXPORT_AVAILABLE True`, then exited successfully. Subsequently, `source-art/build_car.py` created the editable `little_car.blend` and exported `game/assets/little_car.glb`. Godot imported the model, animated its wheel pivots, and rendered it in the standalone game. The actual export/import path is now verified.
