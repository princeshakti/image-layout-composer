using ImageLayoutComposer.Models;
using ImageLayoutComposer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;

namespace ImageLayoutComposer.Tests;

public class ImageComposerServiceTests : IDisposable
{
    private readonly string _outputDir;
    private readonly ImageComposerService _sut;

    public ImageComposerServiceTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), "ilc_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDir);
        _sut = new ImageComposerService(NullLogger<ImageComposerService>.Instance);
    }

    public void Dispose() => Directory.Delete(_outputDir, recursive: true);

    [Theory]
    [InlineData(GridLayout.Grid2x2, 2, 2)]
    [InlineData(GridLayout.Grid3x3, 3, 3)]
    [InlineData(GridLayout.Grid4x4, 4, 4)]
    public async Task ComposeGrid_AllLayouts_ProduceCorrectDimensionsAndOutputFile(
        GridLayout layout, int cols, int rows)
    {
        var result = await _sut.ComposeGridAsync(CreateTempImages(cols * rows), _outputDir, layout);

        Assert.Equal(cols, result.Columns);
        Assert.Equal(rows, result.Rows);
        Assert.True(File.Exists(Path.Combine(_outputDir, result.FileName)));
    }

    [Theory]
    [InlineData(OutputFormat.Png,  ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    [InlineData(OutputFormat.Jpeg, ".jpg", new byte[] { 0xFF, 0xD8 })]
    public async Task ComposeGrid_OutputFormat_ProducesCorrectFileType(
        OutputFormat format, string expectedExt, byte[] magic)
    {
        var result = await _sut.ComposeGridAsync(CreateTempImages(1), _outputDir, GridLayout.Grid2x2, format: format);

        Assert.EndsWith(expectedExt, result.FileName, StringComparison.OrdinalIgnoreCase);
        var bytes = await File.ReadAllBytesAsync(Path.Combine(_outputDir, result.FileName));
        for (int i = 0; i < magic.Length; i++)
            Assert.Equal(magic[i], bytes[i]);
    }

    [Fact]
    public async Task ComposeGrid_CanvasSize_MatchesExpectedPixelDimensions()
    {
        // 2 cols, cellSize=200, padding=10 → width = 2*200 + 3*10 = 430px
        var result = await _sut.ComposeGridAsync(CreateTempImages(4), _outputDir, GridLayout.Grid2x2,
            cellSize: 200, padding: 10);

        using var img = await Image.LoadAsync(Path.Combine(_outputDir, result.FileName));
        Assert.Equal(430, img.Width);
        Assert.Equal(430, img.Height);
    }

    [Fact]
    public async Task ComposeGrid_MoreImagesThanSquareCapacity_ExtraRowsAdded()
    {
        // 2 cols, 8 images → ceil(8/2) = 4 rows. All 8 placed, 0 dropped.
        var result = await _sut.ComposeGridAsync(CreateTempImages(8), _outputDir, GridLayout.Grid2x2);

        Assert.Equal(2, result.Columns);
        Assert.Equal(4, result.Rows);
        Assert.Equal(8, result.PlacedImages);
        Assert.Equal(0, result.EmptyCells);
    }

    [Fact]
    public async Task ComposeGrid_OddImageCount_LastRowHasOneEmptyCell()
    {
        // 2 cols, 3 images → ceil(3/2) = 2 rows, 1 white trailing cell.
        var result = await _sut.ComposeGridAsync(CreateTempImages(3), _outputDir, GridLayout.Grid2x2);

        Assert.Equal(3, result.PlacedImages);
        Assert.Equal(1, result.EmptyCells);
    }

    [Fact]
    public async Task ComposeGrid_NoImages_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ComposeGridAsync(Array.Empty<string>(), _outputDir, GridLayout.Grid2x2));
    }

    [Fact]
    public async Task ComposeGrid_InvalidCellSize_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ComposeGridAsync(CreateTempImages(1), _outputDir, GridLayout.Grid2x2, cellSize: 10));
    }

    [Fact]
    public async Task ComposeGrid_InvalidPadding_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.ComposeGridAsync(CreateTempImages(1), _outputDir, GridLayout.Grid2x2, padding: 200));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<string> CreateTempImages(int count, int w = 80, int h = 80) =>
        Enumerable.Range(0, count).Select(i =>
        {
            var path = Path.Combine(_outputDir, $"input_{i}.png");
            File.WriteAllBytes(path, TestImageFactory.CreatePng(w, h));
            return path;
        }).ToList();
}
