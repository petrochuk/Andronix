using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[AllowAnonymous]
public class TopicController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public TopicController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [Route("{**topicName}", Order = 100)]
    public Task<IActionResult> TopicIndex(string topicName)
    {
        return Task.FromResult<IActionResult>(View("~/Views/Home/Index.cshtml"));
    }
}
