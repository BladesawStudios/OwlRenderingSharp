# Audit: metals, environment and what the render still lacks

Findings from rendering the Master Sword and Hylian Shield against a Temple of Time reference. Every item was checked with the snapshot sample (`--override sampler=0..1`, `--set`, `--slots`, `--material`, `--samplers`), not assumed.

## Verified

- **The reflection cube is the main metal input.** On the shield, `_m0` (metalness map) and `gsys_cube_map` change the image strongly (`--override _m0=1` darkens it, `--no-sky` halves its brightness); `_r0` changes it a little. The game's cube is a runtime capture of the place, which is not in the romfs, so a stand-in is used. A two-colour sky reads as flat grey, and the stand-in's contrast sets how metallic a surface looks. The sample now defaults to a cooled sky, a dark ground (`--ground-scale 0.25`), a dimmer sky (`--sky-scale 0.6`) and a sun lobe (`--sun-lobe 5`).
- **The sword blade does not respond to any of it.** The blade is `Blade_Mark__Mt_Weapon_Sword_Master_Metal` (material program 12366, option `ns_enable_overwrite_surface 1`, `ns_overwrite_surface_graph 2429020893`, `ns_enable_metalness 0`). Overriding `_m0`, `_r0`, the cube or the sun lobe leaves it unchanged to the digit; only `_a0` (its albedo, a pale ice blue with alpha 255 everywhere), `_n0` and two frame buffers move it. Its `_m0` is read at a normal-derived uv (a matcap) and multiplied by material colour 4, but that result never reaches the output in our run. The game's blade is dark steel, so something that drives the surface graph is missing; it is not decoded yet.
- **Light table SSBOs are unbound.** `fp_s0` is read at `LightIndex * 144` bytes (a 144-byte entry per light or probe: three vec3 at 16-byte spacing, then more); `fp_s1`, `fp_s2` hold the light loop's data. They are zero, so point lights, clustered lights and probe boxes never run.
- **Baked probe data exists** in `Bake/Probe/Scene/<place>/X*_Y*_Z*_<lod>.bkprb.zs` (1.8 GB, 80 places, three levels per cell; the data is in a grid of cells, not cube maps) and `Bake/Light/*.bklight.zs`. The format is not read yet. These feed the probe flags (env 64 zw), probe colour and box (65-67, 112) and the 3D light volumes the programs sample (`gsys_user3`, `gsys_depth_shadow_half`, `gsys_gbuffer_emission` as `sampler3D`), all neutral black now. `agl/CubeMapMgrParam` and the `cube_map_probe_analyze`, `light_probe_interpolator`, `sky_analyze` and `hemi_to_env` programs in the screen archive describe how the game builds and blends them.
- **Unfilled frame slots** (`--slots` on the sword): context 20 x, 148-151; environment 8, 34, 40, 55, 57, 58, 60, 62, 64-67, 79, 86, 94, 100-104, 112. Slots 57 and 58 drive a texture array lookup (`gsys_user2`, projection or cookie style) whose result enters the specular term.
- **Frame samplers still neutral:** `gsys_gbuffer_albedo`, `gsys_gbuffer_material_id`, `gsys_normal_modify`, `gsys_projection0/1`, `gsys_static_depth_shadow`, `gsys_user0/2/3`, and the 3D volumes. A white `gsys_gbuffer_albedo` or `gsys_normal_modify` changes the blade by about 18%, so their neutral values matter and are guesses.
- **Prepasses in use:** depth (view and skin), shadow pre-pass (game SSAO, cascade shadows) and the game's post chain. Not run: `fog_pre_pass`, `volume_fog` and the froxel volumes, `screen_space_shadow`, `local_depth_shadow` and `static_local_depth_shadow`, `light_probe_*`, `shadow_analyze`.
- **Library-made, not the game's:** skin scatter, depth of field, distance fog, bloom generation, the reflection cube, exposure (0.18, by eye).

## Next steps, in order of effect

1. Decode what drives the blade's surface graph (the fragment of program 12366 around the `_m0` matcap read and the `fp_c5.data[12]` and `[18]` terms).
2. Read `.bkprb` and `.bklight` so the probes and light volumes are real.
3. Bind the light-table SSBOs with at least the character light (`dir_chara`) as the game stores it.
