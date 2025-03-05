namespace TheApi.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<ApiError>? Errors { get; set; }
}

public class ApiDataResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}

public class ApiError
{
    public string Name { get; set; } = "";
    public string? Message { get; set; }
}
