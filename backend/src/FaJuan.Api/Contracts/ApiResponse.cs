namespace FaJuan.Api.Contracts;

public class ApiResponse<T>
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public bool Success => Code == 200;

    public static ApiResponse<T> Ok(T? data, string message = "success") => new()
    {
        Code = 200,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string message, int code = 400) => new()
    {
        Code = code,
        Message = message,
        Data = default
    };
}
