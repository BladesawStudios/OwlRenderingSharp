namespace OwlRenderingSharp.Gpu.Tests;

public class ShaderLogExcerptTests
{
    private const string Source = "#version 450\nvoid main()\n{\n    foo();\n}\n";

    [Fact]
    public void ANvidiaLineNumberShowsThatLineWithContext()
    {
        string text = ShaderLogExcerpt.Format("0(4) : error C1008: undefined variable \"foo\"", Source);
        Assert.Contains("--- line 4", text);
        Assert.Contains("> 4:     foo();", text);
        Assert.Contains("  3: {", text);
        Assert.Contains("  5: }", text);
    }

    [Fact]
    public void AMesaLineNumberIsRecognisedToo()
    {
        Assert.Contains("> 2: void main()", ShaderLogExcerpt.Format("0:2(1): error: syntax error", Source));
    }

    [Fact]
    public void ALineThatRepeatsIsShownOnce()
    {
        string text = ShaderLogExcerpt.Format("0(4) : error A\n0(4) : error B", Source);
        Assert.Single(text.Split("--- line"), s => s.Contains("foo();")) ;
        Assert.Equal(2, text.Split("--- line").Length);
    }

    [Fact]
    public void ALogWithoutALineNumberIsReturnedAsItIs()
    {
        Assert.Equal("link failed", ShaderLogExcerpt.Format("link failed\n", Source));
    }

    [Fact]
    public void ALineBeyondTheSourceIsIgnored()
    {
        Assert.Equal("0(99) : error", ShaderLogExcerpt.Format("0(99) : error", Source));
    }
}
