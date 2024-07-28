using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using WebApp.Models;

namespace WebApp.Controllers;

[AllowAnonymous]
public class TopicController : Controller
{
    private readonly ILogger<HomeController> _logger;

    [Route("{**topicName}", Order = 100)]
    public Task<IActionResult> Index(string topicName)
    {
        if (topicName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var itemFileName = Path.GetFileName(topicName);
            return GetItemView(topicName.Substring(0, topicName.Length - itemFileName.Length - 1), itemFileName);
        }

        var topicPath = Path.Combine(SiteContentPath, topicName);
        string[] files;
        if (Directory.Exists(topicPath))
        {
            files = Directory.GetFiles(topicPath, "*.html", SearchOption.AllDirectories);
        }
        else
        {
            files = Array.Empty<string>();
        }

        List<TopicItem> items = new();
        foreach (var file in files)
        {
            var relativePath = file.Substring(SiteContentPath.Length).Replace('\\', '/');
            items.Add(new TopicItem() 
            { 
                FileName = file, 
                RelativePath = relativePath 
            });
        }

        var model = new TopicViewModel() { TopicItems = items };
        return Task.FromResult<IActionResult>(View(model));
    }

    private Task<IActionResult> GetItemView(string topicName, string itemFileName)
    {
        var item = new TopicItem() 
        { 
            FileName = Path.Combine(SiteContentPath, topicName, itemFileName),
            RelativePath = Path.Combine(topicName, itemFileName)
        };
        return Task.FromResult<IActionResult>(View("Item", item));
    }

    private string? _siteContentPath;
    private string SiteContentPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_siteContentPath))
            {
#if DEBUG
                _siteContentPath = Path.Combine(Directory.GetCurrentDirectory(), "site-content");
#else
                _siteContentPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "site-content");
#endif
            }

            return _siteContentPath;
        }
    }
}
