namespace PizzaPlace.Api.Common;

/// <summary>
/// Represents the outcome of a service operation without throwing for expected failures.
/// Controllers inspect IsSuccess and map to the appropriate IActionResult.
/// </summary>
public sealed class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public int ErrorStatusCode { get; }
    public string? ErrorMessage { get; }
    public object? ErrorDetail { get; }  // e.g. List<string> deficit lines

    private Result(T value) { Value = value; IsSuccess = true; }

    private Result(int statusCode, string message, object? detail = null)
    {
        ErrorStatusCode = statusCode;
        ErrorMessage = message;
        ErrorDetail = detail;
    }

    public static Result<T> Ok(T value)                                         => new(value);
    public static Result<T> NotFound()                                          => new(404, "Not found.");
    public static Result<T> BadRequest(string message, object? detail = null)   => new(400, message, detail);
    public static Result<T> Unauthorized(string message)                        => new(401, message);
    public static Result<T> Forbidden()                                         => new(403, "Forbidden.");
}
