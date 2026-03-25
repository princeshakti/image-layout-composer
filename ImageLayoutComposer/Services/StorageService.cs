using System.Collections.Concurrent;
using ImageLayoutComposer.Models;

namespace ImageLayoutComposer.Services;

public interface IStorageService
{
    Task<ImageInfo> SaveAsync(IFormFile file, string sessionId);
    IReadOnlyList<string> GetSessionFilePaths(string sessionId);
    IReadOnlyList<ImageInfo> GetSessionImages(string sessionId);
    string GetOutputDirectory();
    string GetOutputUrl(string fileName);
    bool TryMarkComposed(string sessionId);
    void EvictExpiredSessions(TimeSpan ttl);
}

public class LocalStorageService : IStorageService
{
    // Single source of truth for accepted extensions and their MIME types.
    // The controller's ValidateFiles also uses this via the static property below.
    public static readonly IReadOnlyDictionary<string, string> AllowedExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg",  "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png",  "image/png"  },
        };

    private const string UploadsFolder = "uploads";
    private const string OutputsFolder = "outputs";

    private readonly string _uploadsRoot;
    private readonly string _outputRoot;

    private sealed class SessionEntry
    {
        public readonly List<(string Path, ImageInfo Info)> Items = new();
        public readonly DateTimeOffset CreatedAt = DateTimeOffset.UtcNow;
        private int _composed;

        public bool TryMarkComposed() =>
            Interlocked.CompareExchange(ref _composed, 1, 0) == 0;
    }

    private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();

    public LocalStorageService(IWebHostEnvironment env)
    {
        var webRoot  = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _uploadsRoot = Path.Combine(webRoot, UploadsFolder);
        _outputRoot  = Path.Combine(webRoot, OutputsFolder);
        Directory.CreateDirectory(_uploadsRoot);
        Directory.CreateDirectory(_outputRoot);
    }

    public async Task<ImageInfo> SaveAsync(IFormFile file, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID must not be empty.", nameof(sessionId));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.TryGetValue(ext, out var mimeType))
            throw new InvalidOperationException(
                $"Unsupported file type '{ext}'. Only JPG and PNG are accepted.");

        await ValidateImageSignatureAsync(file, ext);

        var sessionDir = Path.Combine(_uploadsRoot, sessionId);
        Directory.CreateDirectory(sessionDir);

        var imageId    = Guid.NewGuid().ToString("N");
        var storedName = $"{imageId}{ext}";
        var storedPath = Path.Combine(sessionDir, storedName);

        await using var fs = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write);
        await file.CopyToAsync(fs);

        var info  = new ImageInfo(imageId, file.FileName, storedName, file.Length, mimeType);
        var entry = _sessions.GetOrAdd(sessionId, _ => new SessionEntry());
        lock (entry.Items) entry.Items.Add((storedPath, info));

        return info;
    }

    public IReadOnlyList<string>   GetSessionFilePaths(string sessionId) =>
        GetFromSession(sessionId, e => e.Items.Select(x => x.Path).ToList());

    public IReadOnlyList<ImageInfo> GetSessionImages(string sessionId) =>
        GetFromSession(sessionId, e => e.Items.Select(x => x.Info).ToList());

    public bool TryMarkComposed(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var entry) && entry.TryMarkComposed();

    public string GetOutputDirectory() => _outputRoot;
    public string GetOutputUrl(string fileName) => $"/{OutputsFolder}/{fileName}";

    public void EvictExpiredSessions(TimeSpan ttl)
    {
        var cutoff = DateTimeOffset.UtcNow - ttl;
        foreach (var key in _sessions.Keys)
        {
            if (_sessions.TryGetValue(key, out var entry) && entry.CreatedAt < cutoff
                && _sessions.TryRemove(key, out _))
            {
                var dir = Path.Combine(_uploadsRoot, key);
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IReadOnlyList<T> GetFromSession<T>(string sessionId, Func<SessionEntry, List<T>> selector)
    {
        if (!_sessions.TryGetValue(sessionId, out var entry)) return Array.Empty<T>();
        lock (entry.Items) return selector(entry);
    }

    private static async Task ValidateImageSignatureAsync(IFormFile file, string ext)
    {
        var buf  = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buf.AsMemory(0, buf.Length));

        bool valid = ext switch
        {
            ".jpg" or ".jpeg" => read >= 3 && buf[0] == 0xFF && buf[1] == 0xD8 && buf[2] == 0xFF,
            ".png"            => read >= 8
                                 && buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E
                                 && buf[3] == 0x47 && buf[4] == 0x0D && buf[5] == 0x0A
                                 && buf[6] == 0x1A && buf[7] == 0x0A,
            _                 => false,
        };

        if (!valid)
            throw new InvalidOperationException(
                $"File '{file.FileName}' does not appear to be a valid {ext.TrimStart('.')} image.");
    }
}
