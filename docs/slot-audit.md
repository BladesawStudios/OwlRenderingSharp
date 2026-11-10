# Uniform slot audit and post chain (Young Link, Kokiri Forest noon)

`dotnet run --project samples/OwlRenderingSharp.Snapshot -- <pack> <shaders> Player_Child out.png --slots` lists every vec4 slot of each frame block that the model's programs read, whether the profile fills it, and whether the layout names it. `--palette` dumps the active palette.

## Slots that read zero or are unnamed, and what they are

| Block / slots | What the GLSL does with them | State |
|---|---|---|
| env 94-96, 100-104 | Fog volume addressing: 95 xy is the distance range, 96 xyz scales screen uv and the log-slice into the volume w; the volumes give extinction (exponent) and in-scatter, `colour * exp2(-ext * k) + scatter` | 95/96 filled from the camera; volumes are empty (no fog), which is the neutral state |
| env 64 zw, 65, 66, 67 w, 112 w | Probe flags (zero turns the probe paths off), probe colour, probe box scale/offset, parallax offset | Named, left zero: no baked probes |
| env 55 w, 59 x, 60 zw, 79 x, 97 x | Additive biases and the cube mip count | Zero is neutral; the stand-in cube has one level |
| env point-light colours 8/86/71 | Colour of point lights 0-2 | Zero: no point lights in the scene |
| context 148-151, 20 x, scene_material 1 z | Cluster light and screen-fade terms | Zero; the cluster SSBOs are unbound, so the light loop runs the sun only |
| fp_c0 81/82/84 | Driver block | Not ours |

Computed-index reads (context ~1500, env ~660 sites) address the light and cascade arrays; those arrays are filled.

## Frame buffers the programs sample

Provided from the render: `gsys_shadow_prepass`, `gsys_depth_shadow_cascade`, `gsys_depth_buffer` (hardware depth, zero to one), `gsys_normalized_linear_depth` (distance over far), `gsys_cube_map` (stand-in sky/ground cube).
Still the neutral default, because a lone model has no scene to put in them: `gsys_gbuffer_albedo`, `gsys_gbuffer_material_id`, `gsys_normal_modify`, `gsys_depth_shadow_quarter/half`, `gsys_projection0/1`, `gsys_static_depth_shadow`, the user samplers and the 3D volumes.

## Post chain

The game's screen shaders are in `Shader/ApplicationPackage.*.sarc.zs` (open with `bfshader unpack`, which uses SarcSharp); its `AglShader.sharcb` entry goes through `bfshader prepare` like any archive and is added to the content as `Agl` (`--post-shaders <bfsp>` in the snapshot sample, `OWL_AGL_SHADERS` in the tests). `GamePostChain` runs its programs through `ShaderScreenPass`:

1. bloom (this library's `BloomPass`: threshold, clamp, quarter-size blur, intensity from the palette),
2. `white_balance` (the palette's temperature and tint become a matrix in the game's own shader),
3. `hdr_compose` program 1920 (power-form bloom compose + tone mapping): `min(M * hdr + offset, 10000) + |bloom|^power`, times the exposure, then the filmic curve `x(2.51x + 0.03) / (x(2.43x + 0.59) + 0.14)`; the options are `BLOOM_COMPOSE` x768, `TONEMAPPING` x384, `GAMMA` x128 in the program number,
4. an sRGB-encoding box downsample of the supersampled frame, then FXAA.

The compose block is `HdrComposeParam`: rows 0-2 the colour matrix (identity here), row 3 holds the bloom power in y and the exposure in z. The exposure is the one value not read from the game's data (0.25, chosen by eye). `agl/ColorCorrectionParam/MainField` is disabled and `LocalExposureParam` is off. Not done: the game's bloom generation (not in this archive), depth of field, `volume_fog`, `fog_pre_pass`, the SSAO and the screen-space shadow programs; `skin_shading_scatter` has no fragment stage (it is compute).

## Alpha test

The colour programs write alpha and leave the cutout to the hardware alpha test (`gsys_alpha_test_enable/func/value` render info); the depth-only programs discard on their own against `gsys_alpha_test_ref_value`. `GlslAlphaTest` adds the missing discard to every colour program, reading the compare function and reference from the support buffer (`alpha_test` and `tfe_offset.x`), which `ModelDrawer` writes per draw from `MaterialResources.AlphaTest`. Hair cards and the cap's fringe now cut out.

## Reflection probe

`SkyProbe` builds the cube the way the game's `hemi_to_env` program does: ground weight `clamp(0.5 - 0.5 * sign(y) * |y|^0.33)` between the hemisphere's two colours, in HDR float with mips; `ReflectionLod` is its last mip.

## Lead not yet applied

`agl/DirectionalLightParam/dir_chara` is a white light of intensity 21.5 in view coordinates (direction 0, -0.62, 0.79) in group `main_env`, against `dir_main` 41.3 in world space; characters are probably lit by a camera-relative key light like this one rather than by the sun alone. The palette's `CharaLightIntensity` and `FieldObjCharaLightIntensity` are the probable multipliers.

In Kokiri Forest at noon `CharaLightIntensity` is 0, so that light would not contribute there; it is left unapplied.
