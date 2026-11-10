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

`PostSettings` + `BloomPass` + `ResolvePass`: exposure, bloom (bright pass, quarter-size blur, composite), white balance, ACES curve and sRGB, applied per supersample, then FXAA. `OotPost` fills it from the palette (`BloomThreshold` in stops, `BloomIntensity`, `BloomComposePower`, `BloomClampedLuminance`, `WhiteBalanceTemperature/Tint`).

Not the game's: the tone curve and the exposure (0.25), the stops reading of the threshold. The game's own post shaders are in `Shader/ApplicationPackage.*.sarc.zs`, whose decompressed magic is `S_Wo`, a container this library cannot open yet; `agl/ColorCorrectionParam/MainField` is `Enable: false` with a luminance-saturation (0.75) and a multiply-colour (1, 0.97, 0.85) step, and `agl/LocalExposureParam` is disabled.

## Lead not yet applied

`agl/DirectionalLightParam/dir_chara` is a white light of intensity 21.5 in view coordinates (direction 0, -0.62, 0.79) in group `main_env`, against `dir_main` 41.3 in world space; characters are probably lit by a camera-relative key light like this one rather than by the sun alone. The palette's `CharaLightIntensity` and `FieldObjCharaLightIntensity` are the probable multipliers.
