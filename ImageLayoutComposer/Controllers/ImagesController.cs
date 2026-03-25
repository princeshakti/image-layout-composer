using ImageLayoutComposer.Models;
using ImageLayoutComposer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageLayoutComposer.Controllers;

[ApiController]
[Route("api/images")]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    private const long MaxFileSizeBytes  = 20 * 1024 * 1024;
    private const long MaxTotalSizeBytes = 80 * 1024 * 1024;
    private const int  MaxFilesPerUpload = 16;

    private readonly IStorageService _storage;
    private readonly IImageComposerService _composer;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(IStorageService storage, IImageComposerService composer, ILogger<ImagesController> logger)
    {
        _storage  = storage;
        _composer = composer;
        _logger   = logger;
    }

    // POST /api/images/upload
    [HttpPost("upload")]
    [RequestSizeLimit(MaxTotalSizeBytes)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),  StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        var (error, validFiles) = ValidateFiles(files);
        if (error != null) return BadRequest(new ErrorResponse(error));

        var sessionId = Guid.NewGuid().ToString("N");
        var stored    = new List<ImageInfo>(validFiles!.Count);

        foreach (var file in validFiles!)
        {
            try   { stored.Add(await _storage.SaveAsync(file, sessionId)); }
            catch (InvalidOperationException ex) { return BadRequest(new ErrorResponse(ex.Message)); }
        }

        _logger.LogInformation("Session {Session}: {Count} image(s) uploaded.", sessionId, stored.Count);
        return Ok(new UploadResponse(sessionId, stored,
            $"{stored.Count} image(s) uploaded. Use sessionId with POST /api/images/{{sessionId}}/compose."));
    }

    // GET /api/images/{sessionId}
    [HttpGet("{sessionId}")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),  StatusCodes.Status404NotFound)]
    public IActionResult GetSession(string sessionId)
    {
        var images = _storage.GetSessionImages(sessionId);
        if (images.Count == 0)
            return NotFound(new ErrorResponse($"Session '{sessionId}' not found or contains no images."));

        return Ok(new UploadResponse(sessionId, images.ToList(), $"{images.Count} image(s) in session."));
    }

    // POST /api/images/{sessionId}/compose
    [HttpPost("{sessionId}/compose")]
    [ProducesResponseType(typeof(ComposeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse),   StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse),   StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Compose(
        string sessionId,
        [FromQuery] GridLayout layout   = GridLayout.Grid2x2,
        [FromQuery] int cellSize        = 400,
        [FromQuery] int padding         = 10,
        [FromQuery] OutputFormat format = OutputFormat.Png)
    {
        // Range validation — the service also guards these, but validating here
        // lets us return a descriptive 400 rather than an unhandled exception.
        if (cellSize is < 50 or > 2000)
            return BadRequest(new ErrorResponse("cellSize must be between 50 and 2000 pixels."));

        if (padding is < 0 or > 100)
            return BadRequest(new ErrorResponse("padding must be between 0 and 100 pixels."));

        var paths = _storage.GetSessionFilePaths(sessionId);
        if (paths.Count == 0)
            return NotFound(new ErrorResponse($"Session '{sessionId}' not found or contains no images."));

        if (!_storage.TryMarkComposed(sessionId))
            return Conflict(new ErrorResponse(
                $"Session '{sessionId}' has already been composed. Upload new images to start a new session."));

        var result = await _composer.ComposeGridAsync(
            paths, _storage.GetOutputDirectory(), layout, cellSize, padding, format);

        int squareCapacity = (int)layout * (int)layout;
        string? warning = paths.Count > squareCapacity
            ? $"You uploaded {paths.Count} images but a {layout} grid has {squareCapacity} cells. " +
              $"The grid was extended to {result.Columns}x{result.Rows} to fit all images."
            : null;

        return Ok(new ComposeResponse(result.FileName, _storage.GetOutputUrl(result.FileName),
            layout.ToString(), result.Columns, result.Rows, paths.Count,
            result.PlacedImages, result.EmptyCells, warning));
    }

    // GET /api/images/download/{fileName}
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(typeof(FileResult),    StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult Download(string fileName)
    {
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return BadRequest(new ErrorResponse("Invalid file name."));

        var filePath = Path.Combine(_storage.GetOutputDirectory(), fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new ErrorResponse($"Output file '{fileName}' not found."));

        var contentType = Path.GetExtension(fileName).ToLowerInvariant() is ".jpg" or ".jpeg"
            ? "image/jpeg" : "image/png";

        return PhysicalFile(filePath, contentType, fileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (string? Error, List<IFormFile>? ValidFiles) ValidateFiles(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
            return ("No files were provided.", null);

        if (files.Count > MaxFilesPerUpload)
            return ($"A maximum of {MaxFilesPerUpload} files may be uploaded per request.", null);

        var valid      = new List<IFormFile>(files.Count);
        long totalSize = 0;

        foreach (var file in files)
        {
            if (file.Length == 0)
                return ($"File '{file.FileName}' is empty (0 bytes).", null);

            if (file.Length > MaxFileSizeBytes)
                return ($"File '{file.FileName}' exceeds the 20 MB per-file size limit.", null);

            totalSize += file.Length;
            if (totalSize > MaxTotalSizeBytes)
                return ("Total upload size exceeds 80 MB.", null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            // Use the same extension set as StorageService — single source of truth.
            if (!LocalStorageService.AllowedExtensions.ContainsKey(ext))
                return ($"File '{file.FileName}' has unsupported extension '{ext}'. " +
                        $"Only {string.Join(", ", LocalStorageService.AllowedExtensions.Keys.Distinct())} are accepted.", null);

            valid.Add(file);
        }

        return valid.Count == 0
            ? ("No non-empty image files were found in the request.", null)
            : (null, valid);
    }
}
