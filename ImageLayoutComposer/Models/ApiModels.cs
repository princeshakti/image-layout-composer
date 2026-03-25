namespace ImageLayoutComposer.Models;

/// <summary>
/// Supported grid layouts. The integer value encodes the number of columns.
/// Rows grow automatically to fit all uploaded images — no image is ever dropped.
/// </summary>
public enum GridLayout
{
    Grid2x2 = 2,
    Grid3x3 = 3,
    Grid4x4 = 4,
}

/// <summary>Supported output image formats.</summary>
public enum OutputFormat { Png, Jpeg }

/// <summary>Metadata about a single stored image.</summary>
public record ImageInfo(
    string ImageId,
    string OriginalFileName,
    string StoredFileName,
    long FileSizeBytes,
    string MimeType
);

/// <summary>Response returned after uploading images.</summary>
public record UploadResponse(
    string SessionId,
    List<ImageInfo> Images,
    string Message
);

/// <summary>Response returned after composing images into a grid.</summary>
public record ComposeResponse(
    string OutputFileName,
    string DownloadUrl,
    string Layout,
    int GridColumns,
    int GridRows,
    int TotalImages,
    int PlacedImages,
    int EmptyCells,
    string? Warning = null
);

/// <summary>Error response envelope.</summary>
public record ErrorResponse(string Error, string? Detail = null);
