using System.Net;

namespace QACopilot.Domain.Exceptions;

/// <summary>
/// Excepcion para fallos al comunicarse con el microservicio de IA (Python).
/// Permite distinguir el tipo de fallo (rate limit, servicio caido, etc.)
/// para que el ExceptionMiddleware devuelva un ErrorCode consistente al frontend.
/// </summary>
public class AiServiceException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode { get; }

    public AiServiceException(string errorCode, string message, HttpStatusCode statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
