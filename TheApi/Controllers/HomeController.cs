namespace TheApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ApiResponse Get()
    {
        return new ApiResponse { Message = "Success" };
    }
}
