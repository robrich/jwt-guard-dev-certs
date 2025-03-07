namespace TheApi.Helpers;

public static class JwtAuthenticationFailedLogger
{
    public static Task WriteAuthenticationFailed(AuthenticationFailedContext context)
    {
        return Task.CompletedTask;
        // ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        //
        // logger.LogError($"Authentication failed for {context.Principal?.Identity?.Name} to {context.Request.Path}: {context.Exception.Message}");
        // ApiResponse res = new ApiResponse
        // {
        //     Success = false,
        //     Message = "401: Authentication failed"
        // };
        // try
        // {
        //     context.Response.StatusCode = 401;
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, $"Failed to set status code to 401 when athentication failed for {context.Principal?.Identity?.Name} to {context.Request.Path}: {ex.Message}");
        //     // Don't fail trying to error
        // }
        // await context.Response.WriteAsJsonAsync(res);
    }
}
