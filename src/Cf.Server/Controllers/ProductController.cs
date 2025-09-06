using Microsoft.AspNetCore.Mvc;

namespace Cf.Server.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class ProductController : ControllerBase
{
    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        return Ok();
    }
}
