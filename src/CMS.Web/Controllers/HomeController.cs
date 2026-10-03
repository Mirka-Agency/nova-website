using System.Diagnostics;
using CMS.Application.Admin;
using CMS.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    public IActionResult Privacy()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    [HttpGet("about")]
    public IActionResult About()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    [HttpGet("contact")]
    public IActionResult Contact()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null, string? message = null)
    {
        var status = code ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = status;

        var (title, fallback) = status switch
        {
            StatusCodes.Status404NotFound => ("یافت نشد", "صفحه مورد نظر یافت نشد."),
            StatusCodes.Status400BadRequest => ("درخواست نامعتبر", "درخواست شما معتبر نیست."),
            _ => ("خطای سرور", "خطای غیرمنتظره‌ای رخ داد.")
        };

        return View(new ErrorViewModel
        {
            Title = title,
            Message = string.IsNullOrWhiteSpace(message) ? fallback : message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
