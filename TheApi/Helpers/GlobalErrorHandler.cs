namespace TheApi.Helpers;

public static class GlobalErrorHandler
{

    public static async Task HandleError(HttpContext context)
    {
        IServiceProvider serviceLocator = context.RequestServices;
		ILogger logger = serviceLocator.GetRequiredService<ILogger<Program>>();

        IExceptionHandlerPathFeature? exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        logger.LogError(exceptionHandlerPathFeature?.Error, context.Request.GetDisplayUrl());

        try
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
        }
        catch
        {
            // if headers are already sent, it's too late to change them here.
        }

        ApiResponse apiResponse = new ApiResponse
        {
            Success = false,
            Message = "500: Error"
        };
        string json = JsonSerializer.Serialize(apiResponse);
        await context.Response.WriteAsync(json);
    }

}
