using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageLayoutComposer.Tests;

/// <summary>Generates minimal valid in-memory image files for testing.</summary>
public static class TestImageFactory
{
    public static byte[] CreatePng(int width = 100, int height = 100, Rgba32? color = null)
    {
        using var img = new Image<Rgba32>(width, height, color ?? Color.CornflowerBlue);
        using var ms  = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    public static byte[] CreateJpeg(int width = 100, int height = 100, Rgba32? color = null)
    {
        using var img = new Image<Rgba32>(width, height, color ?? Color.Salmon);
        using var ms  = new MemoryStream();
        img.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    /// <summary>Builds a multipart/form-data body with the given image files.</summary>
    public static MultipartFormDataContent BuildMultipart(
        IEnumerable<(byte[] Bytes, string FileName)> images)
    {
        var content = new MultipartFormDataContent();
        foreach (var (bytes, name) in images)
            content.Add(BuildRawPart(bytes, name), "files", name);
        return content;
    }

    /// <summary>
    /// Builds a single multipart part with the given raw bytes.
    /// Useful for testing invalid or empty file content.
    /// </summary>
    public static ByteArrayContent BuildRawPart(byte[] bytes, string fileName)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");
        return part;
    }

    /// <summary>Builds a zero-byte multipart part — useful for testing empty-file rejection.</summary>
    public static ByteArrayContent BuildEmptyPart() => BuildRawPart(Array.Empty<byte>(), "empty.png");
}
