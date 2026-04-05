namespace CsrApi.Models;

public class AppError
{
    public string Message { get; }
    public int StatusCode { get; }

    public AppError(string message, int statusCode = 500)
    {
        Message = message;
        StatusCode = statusCode;
    }

    public static AppError Internal(string message) => new AppError(message, 500);
    public static AppError NotFound(string message) => new AppError(message, 404);
    public static AppError BadRequest(string message) => new AppError(message, 400);
    public static AppError Unauthorized(string message) => new AppError(message, 401);
}
