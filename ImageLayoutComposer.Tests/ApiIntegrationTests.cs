using System.Net;
using System.Net.Http.Json;
using ImageLayoutComposer.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImageLayoutComposer.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        => _client = factory.CreateClient();

    // ── Upload ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithValidImages_Returns200WithSessionIdAndImageIds()
    {
        using var content = TestImageFactory.BuildMultipart(new[]
        {
            (TestImageFactory.CreatePng(),  "photo1.png"),
            (TestImageFactory.CreateJpeg(), "photo2.jpg"),
        });

        var response = await _client.PostAsync("/api/images/upload", content);
        var body     = await response.Content.ReadFromJsonAsync<UploadResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionId));
        Assert.Equal(2, body.Images.Count);
        Assert.All(body.Images, img => Assert.False(string.IsNullOrWhiteSpace(img.ImageId)));
    }

    [Fact]
    public async Task Upload_WithNoFiles_Returns400()
    {
        var response = await _client.PostAsync("/api/images/upload", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithUnsupportedFileType_Returns400()
    {
        using var content = new MultipartFormDataContent();
        content.Add(TestImageFactory.BuildRawPart(new byte[] { 0x47, 0x49, 0x46 }, "animation.gif"), "files", "animation.gif");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithSpoofedExtension_Returns400()
    {
        // Real GIF bytes with a .png extension — magic-byte check must catch this.
        var gifBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00 };
        using var content = new MultipartFormDataContent();
        content.Add(TestImageFactory.BuildRawPart(gifBytes, "not-really.png"), "files", "not-really.png");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithZeroByteFile_Returns400()
    {
        using var content = new MultipartFormDataContent();
        content.Add(TestImageFactory.BuildEmptyPart(), "files", "empty.png");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_MixedValidAndZeroByteFiles_Returns400()
    {
        using var content = TestImageFactory.BuildMultipart(new[]
        {
            (TestImageFactory.CreatePng(), "valid.png"),
        });
        content.Add(TestImageFactory.BuildEmptyPart(), "files", "empty.png");

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_With17Files_Returns400()
    {
        var images = Enumerable.Range(0, 17).Select(i => (TestImageFactory.CreatePng(), $"img{i}.png"));
        using var content = TestImageFactory.BuildMultipart(images);

        var response = await _client.PostAsync("/api/images/upload", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Compose ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Grid2x2", 2, 2)]
    [InlineData("Grid3x3", 3, 3)]
    [InlineData("Grid4x4", 4, 4)]
    public async Task Compose_AllLayouts_ReturnCorrectGridDimensions(string layout, int cols, int rows)
    {
        var sessionId = await UploadImagesAsync(cols * rows);
        var response  = await _client.PostAsync($"/api/images/{sessionId}/compose?layout={layout}", null);
        var body      = await response.Content.ReadFromJsonAsync<ComposeResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cols, body!.GridColumns);
        Assert.Equal(rows, body.GridRows);
        Assert.StartsWith("/outputs/", body.DownloadUrl);
    }

    [Fact]
    public async Task Compose_CalledTwiceOnSameSession_Returns409OnSecondCall()
    {
        var sessionId = await UploadImagesAsync(2);
        var first     = await _client.PostAsync($"/api/images/{sessionId}/compose", null);
        var second    = await _client.PostAsync($"/api/images/{sessionId}/compose", null);

        Assert.Equal(HttpStatusCode.OK,       first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Compose_UnknownSession_Returns404()
    {
        var response = await _client.PostAsync("/api/images/bad_session/compose", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Compose_InvalidCellSize_Returns400()
    {
        var sessionId = await UploadImagesAsync(1);
        var response  = await _client.PostAsync($"/api/images/{sessionId}/compose?cellSize=10", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Download ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Png",  "image/png")]
    [InlineData("Jpeg", "image/jpeg")]
    public async Task Download_ComposedFile_ReturnsCorrectContentType(string format, string expectedContentType)
    {
        var sessionId   = await UploadImagesAsync(2);
        var composeBody = await ComposeAsync(sessionId, format: format);

        var response = await _client.GetAsync($"/api/images/download/{composeBody.OutputFileName}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Download_NonExistentFile_Returns404()
    {
        var response = await _client.GetAsync("/api/images/download/doesnotexist.png");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> UploadImagesAsync(int count)
    {
        var images = Enumerable.Range(0, count).Select(i => (TestImageFactory.CreatePng(), $"img{i}.png"));
        using var content = TestImageFactory.BuildMultipart(images);
        var response = await _client.PostAsync("/api/images/upload", content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadResponse>())!.SessionId;
    }

    private async Task<ComposeResponse> ComposeAsync(string sessionId, string layout = "Grid2x2", string format = "Png")
    {
        var response = await _client.PostAsync(
            $"/api/images/{sessionId}/compose?layout={layout}&format={format}", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ComposeResponse>())!;
    }
}
