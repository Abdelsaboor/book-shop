using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.CartCount = context.HttpContext.Session.GetInt32("CartCount") ?? 0;
        base.OnActionExecuting(context);
    }
}
