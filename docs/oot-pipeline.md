# Ocarina of Time (Switch 2): the render pipeline

OOT is a deferred renderer on the same engine as Breath of the Wild and Tears of the Kingdom: the `gsys` model scene, the `agl`
effects, and the `Nin_Ounce_NVN2` shader archives. What is below is read from the romfs, not from the game binary, unless it says
otherwise. WRS's `game-research.md` is the reference for what TotK does with the same pieces.

## Evidence

- **The shading model has a G-buffer.** The `model` shading model in `Model.Product.110` has the samplers `gsys_gbuffer_normal`,
  `gsys_gbuffer_albedo`, `gsys_gbuffer_material_id`, `gsys_gbuffer_shadow` and `gsys_gbuffer_emission`, a light pre-pass, a shadow
  pre-pass, and the cascaded, half, quarter and static depth shadows (`gsys_depth_shadow_cascade`, `_half`, `_quarter`,
  `gsys_static_depth_shadow`).
- **The pass is a shader option.** `gsys_assign_type` picks `gsys_assign_material` (G-buffer), `gsys_assign_cubemap`, `gsys_assign_zonly`,
  `gsys_assign_depthshadow`, `gsys_assign_xlu_zprepass`, `gsys_assign_visualize` or one of two dynamic variants. `gsys_pass` has four
  choices.
- **The scene config.** `gsys/ModelSceneConfigParam/Main` turns on `ZPrePass`, `XluZPrePass`, `CreateShadowPrePass`, `CreateDepthShadow`
  (three cascades of 1024 at 2, 32, 64 and 200 m near planes, far 3000), `CreateStaticDepthShadow` (4096), `CreateSsocclusion`
  (Alchemy, type 2), `CreateEnvCubeMap` (three 128 cubemaps), `CreateHdr`, `CreateBloom`, `CreateDof`, `CreateFilterAa`,
  `CreateColorCorrection`, `CreateOccludedEffect`, `CreateNdsr`, `ForwardSoftEdge`, and a clustered light grid of 32 by 18 by 512. It
  leaves off the light pre-pass, light maps, light probes, reflection, auto exposure and the shadow mask.
- **Per-place parameters.** `EnvSetParam` has one set for each place (Hyrule Field child and adult, Kokiri Forest, Kakariko Village,
  Zora's River, Death Mountain Trail, Sacred Forest Meadow, dungeons, UI, title, pouch). `ModelSceneShadowParam` names the sun light
  (`dir_main`), the projection shadow textures and the SSAO settings. `agl/DirectionalLightParam` has separate light rigs for
  characters (`dir_chara`, `dir_chara_field_obj`, `dir_chara_item`, `dir_chara_sub`), the main field (`dir_main`), the static shadow and
  specular add.
- **The block set** of the `model` shading model: `gsys_context` (13552 bytes), `gsys_shape` (256), `gsys_material` (976),
  `gsys_environment` (1824), `gsys_scene_material` (240, 40 named uniforms), `gsys_skeleton` (48). The terrain adds `gsys_user0` (5024) and
  `gsys_user1` (320).
- **Draw-time options** (`o_` and `ns_`): probe calculation and fetch, indoor check, wetness, micro shadow, dynamic light mask, character
  light mask, skinning overwrite, dirt mask, and the shadow filtering choices.

## The frame

This is the order the options and TotK's graph imply; the passes marked (verify) are the ones to confirm against the game or a capture.

1. **Z pre-pass** of opaque geometry (`gsys_assign_zonly`), then the **translucent z pre-pass** (`gsys_assign_xlu_zprepass`).
2. **Shadow pre-pass** (verify): the `gsys_shadow_prepass` target the lighting reads.
3. **Static depth shadow**: the baked 4096 map, `dir_static_shadow`.
4. **Dynamic depth shadow**: three cascades of 1024 from `dir_main` (`gsys_assign_depthshadow`), plus two projection shadows.
5. **G-buffer** (`gsys_assign_material`): normal, albedo (sRGB), material ID, shadow, emission and depth. The material ID selects the
   resolve program; the albedo's alpha holds flag bits.
6. **Reduced buffers** (verify): quarter-resolution depth and normal for AO and depth of field (`ReducedBuffer16bit`, `Nld`).
7. **SSAO**: Alchemy, with a blur.
8. **Environment cubemaps** (`gsys_assign_cubemap`): the three 128 cubemaps from the environment objects.
9. **Lighting resolve**: one full-screen draw per shading-model pass ordered by material ID, using the directional light, the hemisphere
   light, the environment cubemap, the shadows, AO and the clustered lights.
10. **Sky** and **fog** (aerial perspective).
11. **Forward**: translucent materials with soft edges against the depth; particle effects (`PtclSharp`) and occluded effects.
12. **Depth of field**, **bloom**, **HDR** exposure and tonemap, **colour correction** (a 3D LUT), **anti-aliasing**.
13. **UI** is a separate scene (`UI` config), drawn after.

## Questions that need an answer

- Whether the resolve programs for OOT are in `ApplicationPackage`, as TotK's `agl_technique_pfx` are, and whether OOT has the same
  `PassId` mechanism (a material ID written to attachment 0's red byte and expanded to depth by the resolve).
- The layouts of `gsys_context` and `gsys_environment` for OOT. TotK's are in WRS; OOT's block sizes are the same (13552 and 1824), so they
  are probably the same layout, but a shader's reads are the test.
- Where the sky comes from. TotK's is a precomputed table; OOT has `agl/SkyParam` and `StarParam`, and a time-of-day field.
- How characters are lit. The separate character light rigs say a character is lit by its own directional light, not the field's.
