using ImageLayoutComposer.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageLayoutComposer.Services;

public interface IImageComposerService
{
    Task<CompositionResult> ComposeGridAsync(
        IReadOnlyList<string> imagePaths,
        string outputDirectory,
        GridLayout layout,
        int cellSize  = 400,
        int padding   = 10,
        OutputFormat format = OutputFormat.Png);
}

public record CompositionResult(string FileName, int Columns, int Rows, int PlacedImages, int EmptyCells);

public class ImageComposerService : IImageComposerService
{
    // Reuse across all cells — avoids allocating a new ResizeOptions per image.
    // cellSize is set per-call so we build it inside the method, but Sampler and Mode
    // are constant; extracted to named constants for readability.
    private const ResizeMode   TileResizeMode   = ResizeMode.Pad;
    private static readonly Color TilePadColor  = Color.White;

    private readonly ILogger<ImageComposerService> _logger;

    public ImageComposerService(ILogger<ImageComposerService> logger) => _logger = logger;

    public async Task<CompositionResult> ComposeGridAsync(
        IReadOnlyList<string> imagePaths,
        string outputDirectory,
        GridLayout layout,
        int cellSize  = 400,
        int padding   = 10,
        OutputFormat format = OutputFormat.Png)
    {
        if (imagePaths.Count == 0)
            throw new ArgumentException("At least one image is required.", nameof(imagePaths));
        if (cellSize is < 50 or > 2000)
            throw new ArgumentOutOfRangeException(nameof(cellSize), "cellSize must be between 50 and 2000.");
        if (padding is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(padding), "padding must be between 0 and 100.");

        int cols = (int)layout;
        int rows = (int)Math.Ceiling(imagePaths.Count / (double)cols);

        var (canvasW, canvasH) = CanvasSize(cols, rows, cellSize, padding);

        _logger.LogInformation(
            "Composing {Count} image(s) into {Cols}x{Rows}. Canvas: {W}x{H}px",
            imagePaths.Count, cols, rows, canvasW, canvasH);

        // Build resize options once — same for every tile in this call.
        var resizeOptions = new ResizeOptions
        {
            Size     = new Size(cellSize, cellSize),
            Mode     = TileResizeMode,
            PadColor = TilePadColor,
            Sampler  = KnownResamplers.Lanczos3,
        };

        using var canvas = new Image<Rgba32>(canvasW, canvasH, Color.White);
        int placed = 0;

        for (int i = 0; i < imagePaths.Count; i++)
        {
            var (x, y) = CellPosition(i, cols, cellSize, padding);
            try
            {
                using var tile = await Image.LoadAsync<Rgba32>(imagePaths[i]);
                tile.Mutate(ctx => ctx.Resize(resizeOptions));
                canvas.Mutate(ctx => ctx.DrawImage(tile, new Point(x, y), opacity: 1f));
                placed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load '{Path}'; cell left blank.", imagePaths[i]);
            }
        }

        Directory.CreateDirectory(outputDirectory);
        var ext  = format == OutputFormat.Jpeg ? "jpg" : "png";
        var name = $"grid_{layout}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.{ext}";
        var path = Path.Combine(outputDirectory, name);

        if (format == OutputFormat.Jpeg)
            await canvas.SaveAsJpegAsync(path);
        else
            await canvas.SaveAsPngAsync(path);

        _logger.LogInformation("Saved grid to '{Path}'", path);
        return new CompositionResult(name, cols, rows, placed, cols * rows - placed);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (int Width, int Height) CanvasSize(int cols, int rows, int cellSize, int padding) =>
        (cols * cellSize + (cols + 1) * padding,
         rows * cellSize + (rows + 1) * padding);

    private static (int x, int y) CellPosition(int index, int cols, int cellSize, int padding) =>
        (padding + (index % cols) * (cellSize + padding),
         padding + (index / cols) * (cellSize + padding));
}
