using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

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
    public Task<IActionResult> Index(string topicName)
    {
#if DEBUG
        var topicPath = Path.Combine(Directory.GetCurrentDirectory(), "site-content", topicName);
#else
        var topicPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "site-content", topicName);
#endif
        string[] files;
        if (Directory.Exists(topicPath))
        {
            files = Directory.GetFiles(topicPath, "*.html", SearchOption.AllDirectories);
            Array.Sort(files);
        }
        else
        {
            files = Array.Empty<string>();
        }

        var model = new TopicViewModel() { TopicFiles = files.ToList() };
        return Task.FromResult<IActionResult>(View(model));
    }
}
