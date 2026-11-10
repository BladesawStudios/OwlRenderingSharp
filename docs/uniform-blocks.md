# Uniform blocks of the OOT shaders

Every uniform block the prepared OOT shaders declare, 64 distinct names: the engine blocks (`gsys_*`) every model-scene shader reads, and the
blocks of the AGL programs (`AglShader`) and the foliage compute programs (`FoliageShader`). Nothing here is guessed: names, types and sizes are what
the shader files declare, read with `bfshader blocks --models --uniforms <prepared files>` (BfShaderSharp) over the preparation of the OOT romfs.
Where a slot holds is a different question and is marked as such. WildRenderingSharp's `docs/uniform_blocks.md` and `Graphics/Ubos/*` hold what is
known about the slot contents for TotK and BotW; the same engine wrote these blocks, but OOT's sizes differ, so a slot is only trusted here once a
shader of OOT reads it.

## How the blocks reach a shader

- A block has a **name**, a **type** (0 plain, 1 material, 2 shape, 3 skeleton, 4 shader option) and a **byte size**. One name can have several sizes: the
  size is per shading model, and 0 means the model declares the block but reads nothing from it.
- The **binding slot** is per program, not per block: the compiler numbers the blocks a stage uses from 0, so `gsys_context` is slot 0 in one program
  and absent in another. A renderer resolves a block by name through `PreparedProgram.UniformBlock(index)`, which gives the vertex and fragment slot
  (-1 where the stage does not use it). Compute, task and mesh stages report their slots in the vertex field.
- Members are named only for the material, scene-material and shader-option blocks. The member offsets in the shader files are stored as byte offset + 1;
  the lists below are already corrected. The other blocks have no member names, so their layouts come from reading the decompiled shaders (slot by slot),
  as WRS did.

## Engine blocks (`gsys_*`)

| Block | Type | Sizes (bytes) | Models | Filled by | Layout source |
| --- | --- | --- | --- | --- | --- |
| `gsys_context` | 0 | 0, 13552 | 19 | renderer, once per view | camera slots 0-18 known from WRS (`GsysContext`); the rest of the 13,552 bytes is not mapped |
| `gsys_environment` | 0 | 1824 | 18 | renderer, from the palette and scene parameters | WRS `GsysEnvironment` and `uniform_blocks.md` for TotK; OOT slot use not yet checked |
| `gsys_material` | 1 | 240, 976 | 19 | prepare time, in the pack | named in the shader files; the pack carries the layout record |
| `gsys_scene_material` | 0 | 240 | 18 | renderer (defaults from `SystemModel.SceneMaterial`) | named in the shader files, 40 members (below) |
| `gsys_shader_option` | 4 | 0 | 18 | not a buffer the renderer fills | named (below); the options select the program, so nothing binds |
| `gsys_shape` | 2 | 0, 256 | 19 | renderer, per draw | not named; to read from the shaders |
| `gsys_shape_ex` | 0 | 0 | 19 | ? | not named; every size is 0 |
| `gsys_skeleton` | 3 | 0, 48 | 18 | renderer, per draw | not named; to read from the shaders |
| `gsys_skeleton_ex` | 0 | 0 | 18 | ? | not named; every size is 0 |
| `gsys_user0` | 0 | 0, 1792, 5024 | 16 | renderer / model-specific | not named |
| `gsys_user1` | 0 | 320, 16384 | 9 | renderer / model-specific | not named |
| `gsys_user2` | 0 | 4096, 5120, 18432, 21504, 24576, 41600 | 14 | renderer / model-specific | not named |
| `gsys_user3` | 0 | 6144 | 1 | renderer / model-specific | not named |

What each is for, in one line each:

- `gsys_context`: the frame: camera matrices and constants, resolution, and a large remainder not yet mapped. Used by Foliage.bfsp,Model.bfsp,SceneMaterial.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f0 v0/f-1 v0/f0.
- `gsys_environment`: the lighting environment: light directions and colours, ambient, fog, sky. Used by Foliage.bfsp,Model.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f3 v3/f-1 v3/f1 v3/f3.
- `gsys_material`: per-material parameters. Used by Foliage.bfsp,Model.bfsp,SceneMaterial.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f0 v-1/f2 v2/f-1 v2/f2.
- `gsys_scene_material`: scene-wide material parameters, all `p_dynamic_*`. Used by Foliage.bfsp,Model.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f4 v2/f-1 v4/f-1 v4/f2 v4/f4.
- `gsys_shader_option`: the shader options as constants (type 4), 244 named members, no size. Used by Foliage.bfsp,Model.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): none.
- `gsys_shape`: per-shape constants (256 bytes where read). Used by Foliage.bfsp,Model.bfsp,SceneMaterial.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v0/f-1 v0/f1 v1/f-1 v1/f1.
- `gsys_shape_ex`: shape extension; no size anywhere. Used by Foliage.bfsp,Model.bfsp,SceneMaterial.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v1/f1.
- `gsys_skeleton`: per-skeleton data, 48 bytes (three vec4) where read. Used by Foliage.bfsp,Model.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v2/f-1 v2/f3 v3/f-1 v4/f-1.
- `gsys_skeleton_ex`: skeleton extension; no size anywhere. Used by Foliage.bfsp,Model.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f3 v-1/f5 v2/f-1 v2/f5.
- `gsys_user0`: terrain and foliage user block 0. Used by Foliage.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f1 v-1/f2 v-1/f3 v3/f5 v4/f5 v5/f2 v5/f3 v5/f5.
- `gsys_user1`: user block 1. Used by Foliage.bfsp,TerrainGround.bfsp. Slots seen in programs (vertex/fragment): v-1/f2 v-1/f3 v4/f6 v5/f6 v6/f-1 v6/f3 v6/f4 v6/f6 v7/f-1.
- `gsys_user2`: user block 2 (foliage). Used by Foliage.bfsp. Slots seen in programs (vertex/fragment): v-1/f1 v-1/f2 v-1/f3 v-1/f4 v5/f2 v5/f3 v5/f4 v6/f-1.
- `gsys_user3`: user block 3 (foliage). Used by Foliage.bfsp. Slots seen in programs (vertex/fragment): v-1/f3 v7/f4 v7/f5.

Where the sizes differ by model: `gsys_context` is 13,552 bytes where the shader reads it and 0 where it does not; `gsys_material` is
240 bytes in some models and 976 in others; `gsys_shape` is 256 or 0. Which model has which size is not recorded here yet.

## Blocks of the AGL programs (`AglShader`)

The resolve, sky, post-effect and debug programs. These have their own block names; several are the engine blocks under other names.
`Context` (13,552) and `Env` (1,824) have the sizes of `gsys_context` and `gsys_environment`, and `SceneContext` (14,816) is the larger scene block the
lighting resolve reads.

| Block | Sizes (bytes) | Programs using it |
| --- | --- | --- |
| `Bilateral` | 144 | ssao_compute_bilateral |
| `BlurArg` | 64 | shadow_pre_pass_write_back |
| `Build` | 16 | shading_rate_image |
| `Context` | 160, 13552 | adjust_depth, bake_light_resource, cube_map_probe_analyze, eularian_fluid_simulation_draw, fog_pre_pass, game_over_filter, hdr_compose, inner_mask_analyze, local_depth_shadow, local_depth_shadow_analyze, probe_analyze, shading_rate_image, shadow_analyze, shadow_pre_pass_merge, star_render, static_local_depth_shadow, volume_fog, water_volume, wet_mask, wet_occlusion_mask |
| `ContextUBO` | 144 | active_camera_alpha_renderer, active_camera_alpha_renderer_cs |
| `Detect` | 656 | ssao_compute_detect |
| `DrawArg` | 16 | shadow_pre_pass_write_back |
| `Dynamic` | 16, 48 | shadow_pre_pass_merge, ssao_compute_bilateral, ssao_compute_copy, ssao_compute_detect, ssao_compute_gaussian |
| `Env` | 1824 | eularian_fluid_simulation_draw, fog_pre_pass, foliage_mesh_impostor, foliage_mesh_leaf, foliage_mesh_turf, light_probe_debug, light_probe_interpolator, light_probe_lerped_debug, local_depth_shadow, probe_analyze, shadow_pre_pass_merge, volume_fog, water_volume, wet_occlusion_mask |
| `ExtendFarDepthUBO` | 16 | adjust_depth |
| `HdrComposeParam` | 144 | hdr_compose |
| `InnerMaskAnalyzeParam` | 32 | inner_mask_analyze |
| `InputUBO` | 16 | light_probe_lerp_info_writer_debug |
| `LocalDepthShadow` | 384 | local_depth_shadow |
| `LocalExposure` | 64 | hdr_compose |
| `Mat` | 80, 176 | light_probe_debug, light_probe_lerped_debug |
| `ObjectUBO` | 80 | active_camera_alpha_renderer |
| `Palette` | 80 | shading_rate_image |
| `Param` | 32 | game_over_filter |
| `Parameter` | 16 | cube_map_probe_analyze |
| `Params` | 16, 32, 48, 64 | light_probe_debug, painting_filter, painting_filter_calc_distortion, painting_filter_draw_distortion, painting_filter_stroke |
| `PostFilterContext` | 416 | volume_fog_post_filter |
| `Primitive` | 80, 128 | bake_light_resource, wet_mask |
| `ProbeAnalyzeParam` | 32 | probe_analyze |
| `Rectangles` | 6160 | shading_rate_rectangle |
| `RegisterUBO` | 64, 96, 512 | invert_alpha, proc_tera_patch_const_water, skyocl_copy, skyocl_depthfill, skyocl_normalize |
| `SceneContext` | 14816 | volume_fog |
| `ShadowAnalyzeParam` | 32 | shadow_analyze |
| `ShpMtx` | 256 | water_volume |
| `SkyAnalyzeParam` | 64 | sky_analyze |
| `StaticLocalDepthShadow` | 96 | static_local_depth_shadow |
| `UnitInfo` | 32 | local_depth_shadow_analyze, star_render |
| `WhiteBalanceParam` | 64 | white_balance |
| `cBlurUBO` | 1552 | eularian_fluid_simulation |
| `cCommon` | 80 | screen_space_shadow |
| `cConstantVelocityUBO` | 1552 | eularian_fluid_simulation, water_surface_simulation |
| `cContext` | 96 | hemi_to_env |
| `cDynamic` | 48 | screen_space_shadow |
| `cFootStepUBO` | 1552 | eularian_fluid_simulation |
| `cObstacleUBO` | 1552 | eularian_fluid_simulation |
| `cParameters` | 16 | effect_sun_occlusion_cs |
| `cPointsInfoUBO` | 16 | light_probe_interpolator |
| `cPrimitive` | 112 | water_volume |
| `cRadialVelocityUBO` | 1552 | eularian_fluid_simulation |
| `cRippleUBO` | 1552 | water_surface_simulation |
| `cUBO` | 48, 128, 144, 704 | eularian_fluid_simulation, eularian_fluid_simulation_draw, height_map_editor, radial_blur_util, water_surface_simulation |
| `cViewUBO` | 128 | light_probe_interpolator |

## Blocks of the foliage programs (`FoliageShader`)

Compute programs for foliage draw lists and the mesh, impostor and turf programs. `Env` here is the same 1,824-byte environment block.

| Block | Sizes (bytes) | Programs using it |
| --- | --- | --- |
| `Env` | 1824 | eularian_fluid_simulation_draw, fog_pre_pass, foliage_mesh_impostor, foliage_mesh_leaf, foliage_mesh_turf, light_probe_debug, light_probe_interpolator, light_probe_lerped_debug, local_depth_shadow, probe_analyze, shadow_pre_pass_merge, volume_fog, water_volume, wet_occlusion_mask |
| `UboBindless` | 384 | foliage_draw_list_unified |
| `UboCtx` | 1792 | foliage_draw_list, foliage_draw_list_unified, foliage_mesh_impostor, foliage_mesh_leaf, foliage_mesh_turf |
| `UboShootBranchParam` | 6144 | foliage_draw_list, foliage_draw_list_unified, foliage_mesh_impostor, foliage_mesh_leaf |
| `UboShootLeafParam` | 5120, 18560 | foliage_draw_list, foliage_draw_list_unified, foliage_mesh_impostor, foliage_mesh_leaf, foliage_mesh_turf |

## Named members

### `gsys_scene_material`

40 named members across all models (byte offset in the block; a model that declares fewer members shares the lower offsets).

| Offset | Name |
| --- | --- |
| 0x0 | `p_dynamic_cloud_ratio` |
| 0x4 | `p_dynamic_depth_shadow_scale` |
| 0x8 | `p_dynamic_depth_shadow_off` |
| 0x18 | `p_dynamic_exposure` |
| 0x20 | `p_dynamic_diffuse_ambient_intensity` |
| 0x24 | `p_dynamic_specular_ambient_intensity` |
| 0x30 | `p_dynamic_vis_gray_box_enable_albedo_gray` |
| 0x34 | `p_dynamic_vis_gray_box_albedo_gray` |
| 0x38 | `p_dynamic_vis_gray_box_enable_albedo_luminance_gray` |
| 0x3c | `p_dynamic_vis_gray_box_albedo_luminance_gray_ratio` |
| 0x40 | `p_dynamic_vis_gray_box_albedo_offset` |
| 0x4c | `p_dynamic_vis_gray_box_enable_albedo_offset` |
| 0x50 | `p_dynamic_vis_gray_box_enable_roughness_overwrite` |
| 0x54 | `p_dynamic_vis_gray_box_roughness_overwrite_ratio` |
| 0x58 | `p_dynamic_vis_gray_box_enable_roughness_offset` |
| 0x5c | `p_dynamic_vis_gray_box_roughness_offset` |
| 0x60 | `p_dynamic_vis_gray_box_enable_metalness_overwrite` |
| 0x64 | `p_dynamic_vis_gray_box_metalness_overwrite_ratio` |
| 0x68 | `p_dynamic_vis_gray_box_enable_metalness_offset` |
| 0x6c | `p_dynamic_vis_gray_box_metalness_offset` |
| 0x70 | `p_dynamic_vis_gray_box_enable_normal_boost` |
| 0x74 | `p_dynamic_vis_gray_box_normal_boost_ratio` |
| 0x80 | `p_dynamic_vis_non_mask_color_scale` |
| 0x84 | `p_dynamic_vis_extension_flag0` |
| 0x88 | `p_dynamic_vis_extension_flag1` |
| 0x8c | `p_dynamic_vis_extension_flag2` |
| 0x90 | `p_dynamic_vis_mask_color` |
| 0xa0 | `p_dynamic_vis_tex_dot_pitch_target_res` |
| 0xa4 | `p_dynamic_vis_tex_dot_pitch_thresh_ratio` |
| 0xa8 | `p_dynamic_vis_tex_dot_pitch_target_tex_idx` |
| 0xac | `p_dynamic_vis_tex_dot_pitch_tex_blend_ratio` |
| 0xb0 | `p_dynamic_vis_disable_alpha_mask` |
| 0xb4 | `p_dynamic_vis_enable_blend_alpha_check` |
| 0xb8 | `p_dynamic_vis_enable_blend_alpha_thresh` |
| 0xc0 | `p_dynamic_vis_invalid_pixel_color` |
| 0xd0 | `p_dynamic_vis_range` |
| 0xe0 | `p_dynamic_debug0` |
| 0xe4 | `p_dynamic_debug1` |
| 0xe8 | `p_dynamic_debug2` |
| 0xec | `p_dynamic_debug3` |

### `gsys_material`

169 named members across all models (byte offset in the block; a model that declares fewer members shares the lower offsets).

| Offset | Name |
| --- | --- |
| 0x0 | `ns_tex_mtx0` |
| 0x0 | `p_dynamic_cloud_ratio` |
| 0x4 | `p_dynamic_depth_shadow_scale` |
| 0x8 | `p_dynamic_depth_shadow_off` |
| 0x18 | `p_dynamic_exposure` |
| 0x20 | `ns_tex_mtx1` |
| 0x20 | `p_dynamic_diffuse_ambient_intensity` |
| 0x24 | `p_dynamic_specular_ambient_intensity` |
| 0x30 | `p_dynamic_vis_gray_box_enable_albedo_gray` |
| 0x34 | `p_dynamic_vis_gray_box_albedo_gray` |
| 0x38 | `p_dynamic_vis_gray_box_enable_albedo_luminance_gray` |
| 0x3c | `p_dynamic_vis_gray_box_albedo_luminance_gray_ratio` |
| 0x40 | `ns_tex_mtx2` |
| 0x40 | `p_dynamic_vis_gray_box_albedo_offset` |
| 0x4c | `p_dynamic_vis_gray_box_enable_albedo_offset` |
| 0x50 | `p_dynamic_vis_gray_box_enable_roughness_overwrite` |
| 0x54 | `p_dynamic_vis_gray_box_roughness_overwrite_ratio` |
| 0x58 | `p_dynamic_vis_gray_box_enable_roughness_offset` |
| 0x5c | `p_dynamic_vis_gray_box_roughness_offset` |
| 0x60 | `ns_tex_mtx3` |
| 0x60 | `p_dynamic_vis_gray_box_enable_metalness_overwrite` |
| 0x64 | `p_dynamic_vis_gray_box_metalness_overwrite_ratio` |
| 0x68 | `p_dynamic_vis_gray_box_enable_metalness_offset` |
| 0x6c | `p_dynamic_vis_gray_box_metalness_offset` |
| 0x70 | `p_dynamic_vis_gray_box_enable_normal_boost` |
| 0x74 | `p_dynamic_vis_gray_box_normal_boost_ratio` |
| 0x80 | `ns_param_color0` |
| 0x80 | `p_dynamic_vis_non_mask_color_scale` |
| 0x84 | `p_dynamic_vis_extension_flag0` |
| 0x88 | `p_dynamic_vis_extension_flag1` |
| 0x8c | `p_dynamic_vis_extension_flag2` |
| 0x90 | `ns_param_color1` |
| 0x90 | `p_dynamic_vis_mask_color` |
| 0xa0 | `ns_param_color2` |
| 0xa0 | `p_dynamic_vis_tex_dot_pitch_target_res` |
| 0xa4 | `p_dynamic_vis_tex_dot_pitch_thresh_ratio` |
| 0xa8 | `p_dynamic_vis_tex_dot_pitch_target_tex_idx` |
| 0xac | `p_dynamic_vis_tex_dot_pitch_tex_blend_ratio` |
| 0xb0 | `ns_param_color3` |
| 0xb0 | `p_dynamic_vis_disable_alpha_mask` |
| 0xb4 | `p_dynamic_vis_enable_blend_alpha_check` |
| 0xb8 | `p_dynamic_vis_enable_blend_alpha_thresh` |
| 0xc0 | `ns_param_color4` |
| 0xc0 | `p_dynamic_vis_invalid_pixel_color` |
| 0xd0 | `ns_param_color5` |
| 0xd0 | `p_dynamic_vis_range` |
| 0xe0 | `ns_param_vector0` |
| 0xe0 | `p_dynamic_debug0` |
| 0xe4 | `p_dynamic_debug1` |
| 0xe8 | `p_dynamic_debug2` |
| 0xec | `p_dynamic_debug3` |
| 0xf0 | `ns_param_vector1` |
| 0x100 | `ns_param_texture0_array_index` |
| 0x104 | `ns_param_texture1_array_index` |
| 0x108 | `ns_param_texture2_array_index` |
| 0x10c | `ns_param_texture3_array_index` |
| 0x110 | `ns_param_texture4_array_index` |
| 0x114 | `ns_param_texture5_array_index` |
| 0x118 | `ns_param_texture6_array_index` |
| 0x11c | `ns_param_texture7_array_index` |
| 0x120 | `ns_param_scalar0` |
| 0x124 | `ns_param_scalar1` |
| 0x128 | `ns_param_scalar2` |
| 0x12c | `ns_param_scalar3` |
| 0x130 | `ns_param_scalar4` |
| 0x134 | `ns_param_scalar5` |
| 0x138 | `ns_param_scalar6` |
| 0x13c | `ns_param_scalar7` |
| 0x140 | `ns_aniso_specular_anisotropy` |
| 0x144 | `ns_aniso_spacular_shift_intens` |
| 0x148 | `ns_aniso_spacular_normal_diff_intens` |
| 0x14c | `ns_aniso_spacular_normal_diff_power_coef` |
| 0x150 | `ns_transmission_rate` |
| 0x154 | `ns_transmission_scattering` |
| 0x158 | `ns_transmission_density` |
| 0x15c | `ns_transmission_light_occlusion_pcf_offset` |
| 0x160 | `ns_transmission_light_occlusion_rate` |
| 0x164 | `ns_transmission_light_occlusion_distance` |
| 0x168 | `ns_transmission_light_extinction_distance` |
| 0x16c | `ns_soft_diffuse_scatter_distance` |
| 0x170 | `ns_soft_diffuse_scatter_coeff` |
| 0x174 | `ns_fuzz_rim_power` |
| 0x178 | `ns_fuzz_abledo_scale` |
| 0x17c | `ns_fuzz_reflectance_scale` |
| 0x180 | `ns_curve_fuzz_scale` |
| 0x184 | `ns_curve_fuzz_offset` |
| 0x188 | `ns_curve_fuzz_tone_coeff` |
| 0x18c | `ns_curve_fuzz_peak_pos` |
| 0x190 | `ns_curve_fuzz_peak_stop` |
| 0x194 | `ns_curve_fuzz_peak_intensity` |
| 0x198 | `ns_curve_fuzz_rim_power` |
| 0x19c | `ns_curve_fuzz_rim_intensity` |
| 0x1a0 | `ns_curve_fuzz_noise_intensity` |
| 0x1a4 | `ns_curve_fuzz_noise_decay` |
| 0x1a8 | `ns_emission_intensity` |
| 0x1ac | `gsys_xlu_zprepass_alpha` |
| 0x1b0 | `gsys_alpha_test_ref_value` |
| 0x1c0 | `gsys_bake_st0` |
| 0x1d0 | `gsys_bake_st1` |
| 0x1e0 | `p_bake_emission_single_color` |
| 0x1f0 | `ns_tex_mtx4` |
| 0x210 | `ns_tex_mtx5` |
| 0x230 | `p_model_blend_color` |
| 0x240 | `p_local_water_wet_param0` |
| 0x250 | `p_local_water_wet_param1` |
| 0x260 | `p_local_water_wet_param2` |
| 0x270 | `p_death_petrified_color` |
| 0x280 | `p_gohst_directional_light_color` |
| 0x290 | `p_gohst_edge_emission_color` |
| 0x2a0 | `p_ghost_top_ambient_color` |
| 0x2b0 | `p_ghost_bottom_ambient_color` |
| 0x2c0 | `p_warp_emission_color0` |
| 0x2d0 | `p_vertex_deformation_dir_offset_dir` |
| 0x2dc | `ns_sanda_sss_scatter_distance` |
| 0x2e0 | `p_gohst_overwrite_light_dir` |
| 0x2ec | `gsys_material_id` |
| 0x2f0 | `p_overwrite_stun_dir` |
| 0x2fc | `p_bake_transparency` |
| 0x300 | `p_bake_develop` |
| 0x304 | `p_bake_emission_intensity` |
| 0x308 | `p_bake_diffuse_directionality_multiplier` |
| 0x30c | `p_vertex_deformation_foliage_lie_scale` |
| 0x310 | `p_vertex_deformation_dir_offset_valid_ratio` |
| 0x314 | `p_vertex_deformation_dir_offset_scale` |
| 0x318 | `p_aniso_specular_start_interpolate_width` |
| 0x31c | `p_niso_specular_color_lut_intensity` |
| 0x320 | `p_shadow_density_influence` |
| 0x324 | `p_absorption_ratio` |
| 0x328 | `p_rain_wet_valid_ratio` |
| 0x32c | `p_reflection_caustics_intensity_scale` |
| 0x330 | `p_ssr_intensity_scale` |
| 0x334 | `p_chara_light_param` |
| 0x338 | `p_chara_light_scale` |
| 0x33c | `p_chara_light_mask_valid_ratio` |
| 0x340 | `p_shadow_alpha` |
| 0x344 | `p_glass_truth_alpha` |
| 0x348 | `p_damage_ratio` |
| 0x34c | `p_stun_ratio` |
| 0x350 | `p_water_fog_dist_offset` |
| 0x354 | `p_dither_alpha` |
| 0x358 | `p_local_wind_intensity` |
| 0x35c | `p_world_wind_affect_ratio` |
| 0x360 | `p_local_rain_wet_ratio` |
| 0x364 | `p_local_rain_wet_mask_ratio` |
| 0x368 | `p_softedge_blend_dist` |
| 0x36c | `p_death_petrified_ratio` |
| 0x370 | `p_death_petrified_albedo_min` |
| 0x374 | `p_death_petrified_albedo_max` |
| 0x378 | `p_death_petrified_albedo_affect_ratio` |
| 0x37c | `p_death_erosion_ratio` |
| 0x380 | `p_death_erosion_noise_scale` |
| 0x384 | `p_death_erosion_gradation_width` |
| 0x388 | `p_death_burned_ratio` |
| 0x38c | `p_gohst_efficiency_ratio` |
| 0x390 | `p_gohst_direct_intensity_coef` |
| 0x394 | `p_gohst_directional_light_dir_modify` |
| 0x398 | `p_stun_color_high_scale` |
| 0x39c | `p_warp_emission_intensity` |
| 0x3a0 | `p_warp_dissolve_ratio` |
| 0x3a4 | `p_stop_effect_ratio` |
| 0x3a8 | `p_stop_effect_color_scale` |
| 0x3ac | `p_dirt_effect_ratio` |
| 0x3b0 | `p_dirt_ratio` |
| 0x3b4 | `p_dirt_gradation_width` |
| 0x3b8 | `p_dirt_roughness` |
| 0x3bc | `p_chara_reflection_caustics_ratio` |
| 0x3c0 | `p_chara_reflection_caustics_start_height_ratio` |
| 0x3c4 | `p_chara_reflection_caustics_end_height_ratio` |
| 0x3c8 | `p_debug_param0` |

### `gsys_shader_option`

244 named members across all models (byte offset in the block; a model that declares fewer members shares the lower offsets).

`gsys_alpha_test_enable`, `gsys_alpha_test_func`, `gsys_display_face_type`, `gsys_enable_color_buffer`, `gsys_gbuffer_after_fx`, `gsys_pass`, `gsys_renderstate`, `ns_albedo_select`, `ns_alpha_channel`, `ns_alpha_select`, `ns_aniso_specular_anisotropy_channel`, `ns_aniso_specular_anisotropy_select`, `ns_aniso_specular_direction_flow_map_select`, `ns_aniso_specular_direction_type`, `ns_aniso_specular_mask_channel`, `ns_aniso_specular_mask_select`, `ns_aniso_specular_shift_type`, `ns_ao_blend_type`, `ns_ao_channel`, `ns_ao_select`, `ns_cavity_channel`, `ns_cavity_select`, `ns_curve_fuzz_blend_color_select`, `ns_curve_fuzz_curve_lut_channel`, `ns_curve_fuzz_curve_lut_select`, `ns_curve_fuzz_curve_lut_uv_v_channel`, `ns_curve_fuzz_curve_lut_uv_v_select`, `ns_curve_fuzz_noise_map_channel`, `ns_curve_fuzz_noise_map_select`, `ns_curve_fuzz_type`, `ns_dynamic_depth_shadow_pcf`, `ns_emission_select`, `ns_enable_albedo`, `ns_enable_alpha`, `ns_enable_aniso_specular`, `ns_enable_aniso_specular_mask`, `ns_enable_ao`, `ns_enable_backface_normal_modify`, `ns_enable_backface_normal_modify_with_tangent_space`, `ns_enable_cavity`, `ns_enable_curve_fuzz_inv_curve`, `ns_enable_curve_fuzz_noise`, `ns_enable_curve_fuzz_rim_offset`, `ns_enable_decal_ao`, `ns_enable_decode_normalmap`, `ns_enable_dynamic_depth_shadow`, `ns_enable_emission`, `ns_enable_expression_evaluate`, `ns_enable_fog`, `ns_enable_fuzz`, `ns_enable_fuzz_mask`, `ns_enable_light_pre_pass`, `ns_enable_metalness`, `ns_enable_normal_ao`, `ns_enable_normal_map`, `ns_enable_operation0`, `ns_enable_operation1`, `ns_enable_overwrite_skinning_result`, `ns_enable_overwrite_surface`, `ns_enable_overwrite_vertex_data`, `ns_enable_projection_shadow`, `ns_enable_roughness`, `ns_enable_sanda_sss`, `ns_enable_shadow_pre_pass`, `ns_enable_ssao`, `ns_enable_sss`, `ns_enable_sss_mask`, `ns_enable_static_depth_shadow`, `ns_enable_texcoord0`, `ns_enable_texcoord0_model_scale_link`, `ns_enable_texcoord1`, `ns_enable_texcoord1_model_scale_link`, `ns_enable_texcoord2`, `ns_enable_texcoord2_model_scale_link`, `ns_enable_texcoord3`, `ns_enable_texcoord3_model_scale_link`, `ns_enable_texcoord4`, `ns_enable_texcoord4_model_scale_link`, `ns_enable_texcoord5`, `ns_enable_texcoord5_model_scale_link`, `ns_enable_transmission`, `ns_enable_transmission_density_mask`, `ns_enable_transmission_light_occlusion`, `ns_enable_transmission_thickness_map`, `ns_enable_transmission_thickness_map_legacy_mode`, `ns_expression_evaluate_graph`, `ns_fuzz_mask_channel`, `ns_fuzz_mask_select`, `ns_fuzz_type`, `ns_metalness_channel`, `ns_metalness_select`, `ns_normal_map_select`, `ns_normalmap_sign_type`, `ns_operation0_clamp_result`, `ns_operation0_operand_a`, `ns_operation0_operand_a_channel`, `ns_operation0_operand_b`, `ns_operation0_operand_b_channel`, `ns_operation0_operand_c`, `ns_operation0_operand_c_channel`, `ns_operation0_operator`, `ns_operation0_result`, `ns_operation1_clamp_result`, `ns_operation1_operand_a`, `ns_operation1_operand_a_channel`, `ns_operation1_operand_b`, `ns_operation1_operand_b_channel`, `ns_operation1_operand_c`, `ns_operation1_operand_c_channel`, `ns_operation1_operator`, `ns_operation1_result`, `ns_overwrite_skinning_result_graph`, `ns_overwrite_surface_graph`, `ns_overwrite_vertex_data_graph`, `ns_roughness_channel`, `ns_roughness_select`, `ns_sanda_sss_scatter_color_select`, `ns_soft_diffuse_scatter_color_lut_select`, `ns_soft_diffuse_scatter_color_select`, `ns_sss_mask_channel`, `ns_sss_mask_select`, `ns_sss_type`, `ns_texcoord0_mapping`, `ns_texcoord0_srt`, `ns_texcoord1_mapping`, `ns_texcoord1_srt`, `ns_texcoord2_mapping`, `ns_texcoord2_srt`, `ns_texcoord3_mapping`, `ns_texcoord3_srt`, `ns_texcoord4_mapping`, `ns_texcoord4_srt`, `ns_texcoord5_mapping`, `ns_texcoord5_srt`, `ns_texture0_sampler`, `ns_texture0_texcoord`, `ns_texture1_sampler`, `ns_texture1_texcoord`, `ns_texture2_sampler`, `ns_texture2_texcoord`, `ns_texture3_sampler`, `ns_texture3_texcoord`, `ns_texture4_sampler`, `ns_texture4_texcoord`, `ns_texture5_sampler`, `ns_texture5_texcoord`, `ns_texture6_sampler`, `ns_texture6_texcoord`, `ns_texture7_sampler`, `ns_texture7_texcoord`, `ns_transmission_color_lut_select`, `ns_transmission_color_select`, `ns_transmission_color_type`, `ns_transmission_density_map_channel`, `ns_transmission_density_map_select`, `ns_transmission_light_occlusion_pcf`, `ns_transmission_light_occlusion_type`, `ns_transmission_rate_map_channel`, `ns_transmission_rate_map_select`, `ns_transmission_thickness_channel`, `ns_transmission_thickness_select`, `ns_vertex_calc_type`, `o_absorption_ratio`, `o_absorption_ratio_channel`, `o_aniso_specular_color_lut_sampler`, `o_chara_light_mask`, `o_chara_light_mask_channel`, `o_chara_light_type`, `o_death_erosion_gradation_input`, `o_death_erosion_gradation_input_channel`, `o_dirt_mask_channel`, `o_dirt_mask_select`, `o_dirt_noise_channel`, `o_dirt_noise_select`, `o_dynamic_light_mask_channel`, `o_dynamic_light_mask_select`, `o_enable_additional_specular_light`, `o_enable_aniso_specular_color_lut`, `o_enable_bake_approximate_specular`, `o_enable_bake_diffuse_directionality_adjustable`, `o_enable_bake_emission`, `o_enable_bake_emission_single_color`, `o_enable_bake_texture`, `o_enable_bake_texture_fetch_on_vertex`, `o_enable_death_burned`, `o_enable_death_erosion`, `o_enable_death_petrified`, `o_enable_death_petrified_emission`, `o_enable_dirt_effect`, `o_enable_dynamic_light_mask`, `o_enable_estimate_bake_light_pre`, `o_enable_exposure_link`, `o_enable_glass_truth_out_range_alpha`, `o_enable_gohst`, `o_enable_gohst_always`, `o_enable_gohst_overwrite_light_dir`, `o_enable_invariant_position`, `o_enable_micro_shadow`, `o_enable_overwrite_stun_dir`, `o_enable_point_light`, `o_enable_probe_calc_fragment`, `o_enable_probe_fetch_vertex`, `o_enable_probe_indoor_check`, `o_enable_reflection_caustics`, `o_enable_replace_albedo`, `o_enable_replace_emission`, `o_enable_softedge_blend`, `o_enable_softedge_blend_official`, `o_enable_ssr`, `o_enable_stop_effect`, `o_enable_vertex_deformation`, `o_enable_vertex_deformation_dir_offset_shadow_only`, `o_enable_wet`, `o_export_world_transform_matrix`, `o_far_material`, `o_force_enable_dither_alpha`, `o_force_enable_program_discard`, `o_force_indoor`, `o_force_outdoor`, `o_indirect_after_opa`, `o_inner_material`, `o_inner_material_probe`, `o_material_attribute`, `o_micro_shadow_channel`, `o_micro_shadow_select`, `o_reflection_caustics_ratio_input`, `o_reflection_caustics_ratio_input_channel`, `o_replace_albedo_select`, `o_replace_emission_select`, `o_replace_texcoord_for_planar_reflection`, `o_special_material_behave`, `o_terrain_shader_lod`, `o_use_snapshot_env`, `o_vertex_deformation_dir_offset_dir_input`, `o_vertex_deformation_dir_offset_offset_channel`, `o_vertex_deformation_dir_offset_offset_input`, `o_vertex_deformation_elastic_body_damp_near_by_camera`, `o_vertex_deformation_foliage_lie_to_root_vec`, `o_vertex_deformation_type`, `o_water_type`, `o_water_volume`, `o_water_volume_surface`, `o_wet_ratio`, `o_wet_ratio_channel`

