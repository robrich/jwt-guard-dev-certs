namespace TheApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{

    [HttpGet()]
    public ActionResult<ApiResponse> Get()
    {
        return Ok(new ApiResponse { Message = "Success" });
    }

}
