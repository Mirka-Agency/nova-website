using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AllowAnonymous]
public class ErrorController : Controller
{
    [HttpGet]
    [ActionName("NotFound")]
    public IActionResult NotFoundError(string? message = null)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        ViewData["Title"] = "یافت نشد";
        ViewData["Message"] = string.IsNullOrWhiteSpace(message) ? "صفحه یا مورد درخواستی یافت نشد." : message;
        return View("Error");
    }

    [HttpGet]
    [ActionName("BadRequest")]
    public IActionResult BadRequestError(string? message = null)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        ViewData["Title"] = "درخواست نامعتبر";
        ViewData["Message"] = string.IsNullOrWhiteSpace(message) ? "درخواست شما معتبر نیست." : message;
        return View("Error");
    }

    [HttpGet]
    public IActionResult ServerError(string? message = null)
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        ViewData["Title"] = "خطای سرور";
        ViewData["Message"] = string.IsNullOrWhiteSpace(message)
            ? "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید."
            : message;
        return View("Error");
    }
}
