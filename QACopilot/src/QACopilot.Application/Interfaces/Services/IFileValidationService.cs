namespace QACopilot.Application.Interfaces.Services;

public interface IFileValidationService
{
    Task<bool> IsValidFileAsync(byte[] fileContent, string fileName, string contentType);
    Task<bool> IsAllowedExtensionAsync(string fileName);
    Task<bool> IsWithinSizeLimitAsync(long fileSizeBytes);
    string GetSafeFileName(string originalFileName);
}