namespace OwlRenderingSharp.Server;

/// <summary>The lines a client sends and the server answers with.</summary>
/// <remarks>
/// A client writes one command per line to standard input and reads lines from standard output. Floats are written in invariant culture, a name list is separated by <c>|</c>, and the fields of
/// a command that holds paths or names are separated by tabs.
/// <code>
/// open ROMFS TAB CACHE TAB NAME TAB MODELFILE TAB ROMFSPATHS TAB EDITEDFILE
///                                    prepares the model file (the plain BFRES EDITEDFILE when it is given) against the romfs, loads it and draws every material once so the GPU has compiled
///                                    its shaders; ROMFSPATHS are more romfs folders joined by |. Answers STATUS lines, then READY, or FAIL message
/// look SUN KEY AZIMUTH ELEVATION REFLECTIONS EXPOSURE WETNESS FOG OCCLUSION SUBSURFACE
///                                    sets the lighting of the pictures that follow
/// hide MATERIAL|MATERIAL... TAB SHAPE|SHAPE...
///                                    draws only the named shapes (none after the tab names all), leaving out those of the named materials
/// render W H EX EY EZ TX TY TZ UX UY UZ FOV NEAR FAR SAMPLES
///                                    draws the model; answers FRAME W H and then W*H*4 bytes of straight-alpha RGBA, top row first
/// preview MATERIAL TAB SIZE TAB sphere|mesh [TAB EX EY EZ TX TY TZ FOV NEAR FAR]
///                                    draws one material alone, square: on a sphere, or as its shapes seen from the camera given; answers like render
/// quit                               ends the server
/// </code>
/// Any command that fails answers FAIL and the message on one line; the server keeps running.
/// </remarks>
static class Protocol
{
    public const string Status = "STATUS ";
    public const string Ready = "READY";
    public const string Fail = "FAIL ";
    public const string Frame = "FRAME ";
}
