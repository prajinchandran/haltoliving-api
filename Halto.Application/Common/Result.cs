namespace Halto.Application.Common;

public class Result<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    public static Result<T> Success(T data, int statusCode = 200) =>
        new() { Succeeded = true, Data = data, StatusCode = statusCode };

    public static Result<T> Failure(string error, int statusCode = 400) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };

    public static Result<T> NotFound(string error = "Resource not found") =>
        new() { Succeeded = false, Error = error, StatusCode = 404 };

    public static Result<T> Forbidden(string error = "Access denied") =>
        new() { Succeeded = false, Error = error, StatusCode = 403 };
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
