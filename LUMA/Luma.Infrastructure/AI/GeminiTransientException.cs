namespace Luma.Infrastructure.AI;

public class GeminiTransientException : Exception
{
    public int? StatusCode { get; }

    public GeminiTransientException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public GeminiTransientException(string message, Exception innerException, int? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
