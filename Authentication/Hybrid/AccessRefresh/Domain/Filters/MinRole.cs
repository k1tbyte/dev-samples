using AccessRefresh.Data.Entities;
using AccessRefresh.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccessRefresh.Domain.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class MinRoleAttribute(EUserRole minRole) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.GetUser();
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.Role < minRole)
        {
            context.Result = new ForbidResult();
        }
    }
}