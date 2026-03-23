public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}

public static class ResponseHelper
{
    public static ApiResponse<T> Success<T>(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Status = "success",
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<object> Error(string message, int code = 400)
    {
        return new ApiResponse<object>
        {
            Code = code,
            Status = "error",
            Message = message,
            Data = null
        };
    }
}