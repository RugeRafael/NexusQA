using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.Infrastructure.Services;

public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private readonly long _maxFileSizeBytes;

    private static readonly Dictionary<string, byte[]> AllowedMagicBytes = new()
    {
        { ".pdf",  [0x25, 0x50, 0x44, 0x46] },
        { ".docx", [0x50, 0x4B, 0x03, 0x04] },
        { ".xlsx", [0x50, 0x4B, 0x03, 0x04] },
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },
        { ".jpg",  [0xFF, 0xD8, 0xFF] },
    };

    private static readonly HashSet<string> AllowedExtensions =
        [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"];

    public FileValidationService(
        IConfiguration configuration,
        ILogger<FileValidationService> logger)
    {
        _logger = logger;
        _maxFileSizeBytes = configuration.GetValue<long>("Security:MaxFileSizeBytes", 10_485_760);
    }

    public async Task<bool> IsValidFileAsync(
        byte[] fileContent, string fileName, string contentType)
    {
        await Task.CompletedTask;

        var extension = Path.GetExtension(fileName).ToLower();

        if (!AllowedMagicBytes.TryGetValue(extension, out var expectedMagic))
        {
            _logger.LogWarning("File {FileName} has unsupported extension", fileName);
            return false;
        }

        if (fileContent.Length < expectedMagic.Length)
        {
            _logger.LogWarning("File {FileName} too small to validate magic bytes", fileName);
            return false;
        }

        var actualMagic = fileContent[..expectedMagic.Length];
        var isValid = actualMagic.SequenceEqual(expectedMagic);

        if (!isValid)
            _logger.LogWarning(
                "File {FileName} failed magic bytes validation. Possible file spoofing attempt.", fileName);

        return isValid;
    }

    public async Task<bool> IsAllowedExtensionAsync(string fileName)
    {
        await Task.CompletedTask;
        var extension = Path.GetExtension(fileName).ToLower();
        return AllowedExtensions.Contains(extension);
    }

    public async Task<bool> IsWithinSizeLimitAsync(long fileSizeBytes)
    {
        await Task.CompletedTask;
        return fileSizeBytes <= _maxFileSizeBytes;
    }

    public string GetSafeFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLower();
        var safeName = Path.GetFileNameWithoutExtension(originalFileName);

        safeName = new string(safeName
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        if (string.IsNullOrEmpty(safeName))
            safeName = "file";

        return $"{safeName}_{Guid.NewGuid():N}{extension}";
    }
}