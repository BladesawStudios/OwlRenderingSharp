# Architecture

OwlRenderingSharp draws the games' models with the games' own shaders. BfShaderSharp does all the reading and preparing; Owl
only consumes what it writes (`.bfap` asset packs, `.bfsp` prepared shader archives) and turns it into frames.

This is the structure, what it keeps from WildRenderingSharp (WRS) and what it changes, and the order to build it in.

## What WRS gets right, and keeps

- **Neutral core, one profile per game.** The renderer describes a frame in neutral terms and the game turns it into the bytes its
  shaders read.
- **The game's own shaders.** Decompiled programs are never committed; the renderer's own GLSL is embedded files, not C# strings.
- **Uniform block snapshot tests** that pin the exact bytes of every block, and a headless GL test bench.
- **A fixed world convention** (right-handed, Y-up, the games' own), with no conversion in the blocks.

## What WRS gets wrong, and what Owl does instead

| WRS | Owl |
| --- | --- |
| One assembly; layers are namespaces, enforced by a reflection test (`LayeringTests`) | One project per layer. The project graph is the layering rule, so a violation does not compile |
| Preparation is a second program with a JSON manifest and a cache directory, and a preparer that may run out of process | No preparation step in the renderer. It opens a pack and a shader archive; the format is BfShaderSharp's and has one reader |
| A frame is an ordered list of stages that share a mutable `FrameContext`; what each reads and writes is only in the code | A frame graph. Passes declare the targets they read and write; the graph orders them, allocates and aliases targets, culls passes nothing reads, and can show any target |
| Ubo layouts are hand-numbered slot constants per game, built by per-game builders | Layouts are data (see Uniform blocks): named fields, with offsets from the shader reflection where the shader names them and from a layout file where it does not, each with its evidence |
| `Log.Sink` and `GLHostState` are static or ambient | An `IRenderDiagnostics` and a `GpuDevice` are passed in; nothing is static |
| GL calls are everywhere (`Gpu` is helpers, passes call `gl.*`) | Only the GPU project references OpenGL; passes use its resource types and record commands |
| A model is loaded per actor; shared textures and meshes are shared only by accident | GPU resources are keyed by pack blob key. Identical texture or geometry bytes are one GL object, whatever model asks |
| 158 files in `Profiles`, with TotK passes mixed with TotK data | A profile project holds a pass list, layout files and parameter readers; passes that two games share are in the shared pass library |
| Deferred is hard-wired into `DeferredPipeline` | The pipeline is whatever the game's graph is. A forward game is a graph with no G-buffer |

## Projects

Each row may reference the rows above it, never below.

| Layer | Project | What it is |
| --- | --- | --- |
| 0 | `OwlRenderingSharp.Core` | math, camera and lighting inputs, `IRenderDiagnostics`, the world convention. No GL, no pack |
| 1 | `OwlRenderingSharp.Content` | opens packs and prepared shaders through BfShaderSharp; hands out models, materials, textures and animations as plain data keyed by blob key. No GL |
| 2 | `OwlRenderingSharp.Animation` | curve evaluation (`CurveDecoder` from BfShaderSharp), skeleton posing, material animation. No GL |
| 3 | `OwlRenderingSharp.Gpu` | the only project that references OpenGL: device, buffers, textures, framebuffers, programs, samplers, timer queries, the resource cache keyed by blob key |
| 4 | `OwlRenderingSharp.Graph` | the frame graph: pass and target declarations, the compiler, execution and per-pass timing; the draw list, sorting and batching |
| 5 | `OwlRenderingSharp.Passes` | passes any game can use: depth pre-pass, shadow map, G-buffer draw, SSAO, bloom, tonemap, anti-aliasing, blit, debug views |
| 6 | `OwlRenderingSharp.Games.Oot` | everything specific to Ocarina of Time: its graph, uniform block layouts, parameter readers, its resolve and sky passes. Later siblings: `Games.Totk`, `Games.Botw` |
| 7 | `OwlRenderingSharp` | the host-facing `Renderer`, views and offscreen targets, background content loading |

A new project is added in the commit that adds its first class.

## The content path

```
romfs --bfshader assets--> game.bfap        (models, materials with prebuilt uniform blocks, textures, animations)
      --bfshader prepare--> Model.bfsp      (decompiled GLSL per program)
```

- **A model** is its record: shapes, a skeleton, materials, all by blob key. The renderer never builds a material; the record holds the
  selected program number, the sampler and texture names, the uniform block bytes and the layout.
- **Draw-time program selection.** The pack's program is the one the material's options pick. The renderer's own options (skin weight
  count, probe, wetness, the pass in `gsys_pass` and `gsys_assign_type`) are passed to `MaterialShaderSelector`-style lookup over the
  prepared archive's `ProgramLookup`, once per (material, pass) and cached.
- **Resources by blob key.** `GpuResources.Texture(key)` and `.Geometry(key)` create a GL object on first use. Native texture frames
  are decoded and uploaded on a worker and the GL thread only swaps the handle in, within a per-frame upload budget.
- **Animation** is evaluated on the CPU into a bone palette per actor per frame, in parallel across actors.

## Modded models and distribution

Owl is built to sit inside BfresStudio, where users open their own modded `.bfres` files as well as the game's.

- **A modded model is prepared like a dump model.** BfShaderSharp's `ModelAssetPreparer` (CLI `bfshader model`) runs the same importers on one `.bfres`
  plus its textures, against the user's own romfs for shader programs and for textures the mod does not bring, and writes a small pack of its own.
  The renderer opens it beside the game's pack and cannot tell a modded model from a dump one. Materials always name a real game shading model
  and program, so there is no 'program missing' case to design for. Textures the mod brings (given files or BNTX embedded in the `.bfres`) win over the romfs.
- **A mod can name things the pack lacks**: a material option combination the game never compiled, a shading model that is missing, a
  texture that is not in the pack. Program selection uses the nearest-program lookup, a missing texture is the pack's neutral texture,
  and each fallback is reported through `IRenderDiagnostics` so the studio can show it.
- **Nothing derived from the game is shipped.** The `.bfap` and `.bfsp` are generated on the user's machine from their own game dump, by a
  first-run step the host runs (`AssetPreparer` and `ArchivePreparer`, with progress). What may be shipped is the **game profile JSON**:
  the file that says where a game's files are, which parameter files configure a frame, and which uniform block layouts apply. A profile
  holds our own findings (names, offsets, evidence), no game code, shaders or textures.
- The renderer's own GLSL is ours and ships. Decompiled game shaders are generated and never committed or packaged.

## The frame graph

A pass is a class with a `Declare` method and an `Execute` method.

```
Declare:  builder.Read(depth); var gbuf = builder.Create("gbuffer", Format, Size.Full); builder.Write(gbuf);
Execute:  commands.Bind(...); commands.Draw(list);
```

- The graph compiles once per configuration (resolution, features on or off), not every frame. Compiling orders the passes by their
  dependencies, finds the lifetime of each target and lets targets of the same format and size share memory, and drops passes whose
  outputs nothing reads (a debug view off means its pass is gone).
- Every target has a name, so a host can ask to see any of them (`albedo`, `normal`, `shadow`, `raw-hdr`) without a pass doing special work.
- Execution records per-pass CPU and GPU time and the debug name for each pass as a GL debug group, so a RenderDoc capture reads as the graph.
- The draw list is built once per frame from the scene on the CPU (culling, sorting by program, material, then depth) and each geometry
  pass draws a filtered slice of it. Compatible draws are merged into multi-draw indirect calls over shared buffers.

## Uniform blocks

The games' shaders read named blocks (`gsys_context`, `gsys_environment`, `gsys_scene_material`, `gsys_material`, `gsys_shape`, `gsys_skeleton`
and game-specific ones). A block has a **layout**: a size and named, typed fields with byte offsets.

- `gsys_material` is built at prepare time and arrives in the pack. Its layout record is in the pack too.
- `gsys_scene_material` and the shader-option block name their fields in the BFSHA, so their layouts come from the shader reflection.
- `gsys_context` and `gsys_environment` have no field names in the BFSHA. Their layouts are **layout files** (embedded data, one per game
  and block): each field has a name, type, offset and a line of evidence (shader that reads it, or the engine function that writes it).
  WRS's `uniform_blocks.md` is the starting material for these.
- A `BlockWriter` sets fields by name and type against a layout; setting an unknown field or a wrong type is an error. A snapshot test
  per block pins the bytes.

## Games

A game project supplies:

1. **A graph builder** that assembles passes from the shared library and its own.
2. **Block fillers** that turn the neutral camera, lighting and actor inputs, and the game's parameter files, into block bytes.
3. **Parameter readers** for the small files the game uses to configure a frame (`gsys__ModelSceneConfigParam`, `agl__*Param`,
   `EnvSetParam`); these are BYML and read with BymlSharp, and nothing else is read from the romfs by the renderer.
4. **Option overrides** for program selection per pass.

Games differ in how much they share. The Wild-era engine (BotW, TotK, OOT) shares the `gsys` blocks, the G-buffer layout and the
deferred structure. A forward game is a different graph over the same Core, Content, Gpu and Graph.

## Build order

1. **Core, Gpu, Content.** Open a pack, upload one model's geometry and textures, draw it with a game program and a hand-filled camera
   block, into an offscreen target. The first proof is Link's body in the G-buffer albedo target. It needs the decision below.
2. **Graph.** Move that into passes: depth pre-pass, G-buffer, a debug view of each G-buffer target.
3. **Animation.** Pose Link from a `Player_Animation` clip.
4. **OOT lighting.** The resolve with directional light and hemisphere ambient, then depth shadows, then SSAO.
5. **Sky, fog, forward, post.**
6. **Scenes.** Many actors, instancing, streaming of a field.

## Decision needed first: the resolve programs

The G-buffer programs are in the `Model` archive, which BfShaderSharp prepares. The deferred **resolve** programs (the full-screen lighting
passes), the sky and the post effects are AGL programs in `Shader/ApplicationPackage.*.sarc.zs`, in the SHARC format, which BfShaderSharp does
not read yet (its TODO lists SHARCFB). Two ways to proceed:

- **Read SHARC archives in BfShaderSharp** (the old ShaderLibrary has a `Sharc` reader to port) and run the game's own resolve programs, as
  WRS does for TotK. It gives the game's exact look and the first bring-up cost is higher.
- **Write Owl's own resolve shaders** from the G-buffer layout. It is quicker to bring up and is never the game's look.

The recommendation is the first, because the aim is the games' own rendering and the SHARC reader is a bounded piece of work with a
reference implementation.
