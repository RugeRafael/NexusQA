namespace QACopilot.API.Helpers;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Request successful")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}