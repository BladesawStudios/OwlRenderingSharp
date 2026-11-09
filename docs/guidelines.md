# Guidelines

## Size

- Classes are about 256 lines at most.
- Methods are about 64 lines at most.
- A file longer than that is split by what its parts are, not by where the line fell: each part is a class that can be named.

## Comments

- Every class has a `<summary>` and a `<remarks>`, and nothing else is an XML doc comment: no `<param>`, `<returns>` or member docs.
- The `<summary>` is 1-2 sentences that say what the class is.
- The `<remarks>` says in more detail what the class does: what it holds, what it guarantees, what a reader needs to know before using it.
- Remarks describe the class as it is. They never refer to how it used to work, what it replaced, or what is planned.
- Every other comment is a regular `//` comment of 1-2 sentences, and only where the code cannot say it.
- No giant comments, no comments on every member, no history.

## Code

- No re-defining shared code. Behavior that more than one class needs (reading a table, resolving a string, formatting a value) is one class that the others call.
- No ad-hoc copies. A second definition that works slightly differently "for convenience" is a bug in the design: extend the shared class instead.
- Always abstract the shared part.
- Nothing is temporary: no stopgaps, no "for now".
- `// TODO:` is for work that genuinely has to wait. It says what the code does today and what would remove the limitation. Keep them sparse.
- A stopgap that prevents a partial implementation is the one allowed workaround. It is marked `// TODO:` and says it exists to prevent a partial implementation.

## Design

- Dependencies point down, and the project graph enforces it: a layer is a project, and a project references only the layers below it.
- Only `OwlRenderingSharp.Gpu` references an OpenGL binding. Everything above it talks to the GPU through its types.
- No ambient state: no statics that hold a GL context, a log sink, a cache directory or a setting. What a class needs is passed to it.
- The renderer reads the asset pack and prepared shader files that BfShaderSharp writes, and nothing else the game ships. It parses no BFRES, BFSHA, BNTX or BYML of its own except the small parameter files a game profile names.
- A game is data plus a few classes in its own project. The shared layers know no game.
- Large data stays a slice of a pack until a frame needs it, and a GPU resource is created once per blob key however many models use it.

## Errors

- A GL error or a missing resource is reported with an exception that names the pass and the resource. Nothing is logged to the console.
- An exception is either handled or allowed to propagate. The library never swallows one.
- An empty `catch` is allowed only for a best-effort operation, narrows to the exception types it expects, and says what is lost.
- Diagnostics go to an `IRenderDiagnostics` the host passes in.

## Platforms

- The library builds for every platform .NET does and calls nothing operating-system specific.
- A path inside a game's files uses `/`.

## Tests

- Every class has tests. Header and structure tests use real bytes where there are any to take.
- Everything above the GPU layer is tested without a GL context. The GPU layer and the passes are tested in a hidden window and compared with recorded images.
- Corpus tests run when a game's romfs is configured, and are skipped otherwise.
- Frame time is a requirement: the frame graph reports CPU and GPU time per pass, and a test asserts a budget for a reference scene.

## Process

- One system per branch, and one commit per class or per concern.
- A refactor changes no behavior. If behavior has to change, it is its own commit and says so.
- The build and tests pass before a branch is merged.
- Commits are authored by the person doing the work, with no co-author trailers.
