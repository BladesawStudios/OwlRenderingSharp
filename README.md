# OwlRenderingSharp

A C# OpenGL renderer for the Nintendo games' own models, materials and shaders, starting with *Ocarina of Time* on Switch 2. It is
WildRenderingSharp written properly from the start: BfShaderSharp does all the reading and preparing, and Owl only renders what it prepares.

Nothing is built yet. The design is in:

- [docs/architecture.md](docs/architecture.md): the projects, the frame graph, uniform blocks, how a game is added, and the build order
- [docs/oot-pipeline.md](docs/oot-pipeline.md): what is known about OOT's render pipeline and what is still to find out
- [docs/guidelines.md](docs/guidelines.md): the code rules

## Building

```bash
git clone --recursive https://github.com/BladesawStudios/OwlRenderingSharp.git
```

`vendor/BfShaderSharp` is a submodule, and has its own submodules.
