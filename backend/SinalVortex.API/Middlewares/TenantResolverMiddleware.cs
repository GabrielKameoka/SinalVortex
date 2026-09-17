using System.Security.Claims;
using SinalVortex.Application.Common.Contexts;

namespace SinalVortex.Api.Middlewares;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        if (context.User.Identity?.IsAuthenticated == true && Guid.TryParse(tenantClaim, out var tenantId))
        {
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
                (!Guid.TryParse(tenantHeader, out var requestedTenant) || requestedTenant != tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { Mensagem = "X-Tenant-Id não corresponde ao tenant autenticado." });
                return;
            }

            tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}
