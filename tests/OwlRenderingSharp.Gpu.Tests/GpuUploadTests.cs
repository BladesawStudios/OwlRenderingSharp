using OwlRenderingSharp.Content.Geometry;
using OwlRenderingSharp.Content.Textures;

namespace OwlRenderingSharp.Gpu.Tests;

[Collection(GlCollection.Name)]
public class GpuUploadTests(GlFixture gl)
{
    [SkippableFact]
    public void ABlockCompressedTextureUploadsEveryLevelAndComesBackIdentical()
    {
        byte[] top = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        byte[] low = [9, 8, 7, 6, 5, 4, 3, 2];
        DecodedTexture decoded = new(8, 4, TextureEncoding.Bc1, [top, low]);
        (byte[] a, byte[] b) = gl.Require().Run(device =>
        {
            using GpuTexture texture = GpuTextureLoader.Create(device, "bc1", decoded);
            return (texture.Download<byte>(0), texture.Download<byte>(1));
        });
        Assert.Equal(decoded.Levels[0], a);
        Assert.Equal(low, b);
    }

    [SkippableFact]
    public void ASmallerMipThanABlockStillUploadsOneBlock()
    {
        DecodedTexture decoded = new(4, 4, TextureEncoding.Bc4, [new byte[8], new byte[8], new byte[8]]);
        int mips = gl.Require().Run(device =>
        {
            using GpuTexture texture = GpuTextureLoader.Create(device, "bc4", decoded);
            return texture.MipCount;
        });
        Assert.Equal(3, mips);
    }

    [SkippableFact]
    public void AnRgbaTextureUploadsLikeAnyOther()
    {
        byte[] pixels = Enumerable.Range(0, 16).Select(i => (byte)(i * 3)).ToArray();
        byte[] back = gl.Require().Run(device =>
        {
            using GpuTexture texture = GpuTextureLoader.Create(device, "rgba", new DecodedTexture(2, 2, TextureEncoding.Rgba8Srgb, [pixels]));
            return texture.Download<byte>();
        });
        Assert.Equal(pixels, back);
    }

    [SkippableFact]
    public void CompressedAndUncompressedUploadsRefuseEachOthersFormats()
    {
        Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture bc = new(device, "bc", GpuFormat.Bc1, 4, 4);
            bc.Upload<byte>(new byte[8]);
        }));
        Assert.Throws<GpuException>(() => gl.Require().Run(device =>
        {
            using GpuTexture rgba = new(device, "rgba", GpuFormat.Rgba8, 4, 4);
            rgba.UploadCompressed(new byte[64]);
        }));
    }

    [SkippableFact]
    public void GeometryCreatesAMeshWithItsIndicesAndAttributes()
    {
        byte[] vertices = new byte[3 * 32];
        MeshGeometry mesh = new(3, false, [0, 0, 1, 0, 2, 0], 0);
        ShapeGeometry shape = new(3, [0, 6], vertices, [mesh, mesh]);
        gl.Require().Run(device =>
        {
            using GpuGeometry geometry = new(device, "g", shape);
            Assert.Equal(2, geometry.Meshes.Count);
            Assert.Equal(3, geometry.Meshes[0].IndexCount);
            Assert.NotEqual(0u, geometry.Meshes[1].VertexArray);
            device.Check("geometry");
        });
    }
}
