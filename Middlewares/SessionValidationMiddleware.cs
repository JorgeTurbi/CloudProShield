using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Services.SessionServices;

namespace CloudShield.Middlewares;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx)
    {
        // Resolver el servicio scoped desde el contexto de la petición
        var validator = ctx.RequestServices.GetRequiredService<ISessionValidationService>();

        // ---- archivos estáticos o docs ----------
        var path = ctx.Request.Path.Value!.ToLowerInvariant();
        if (path.StartsWith("/swagger") || path.StartsWith("/openapi") || path.StartsWith("/redoc"))
        {
            await _next(ctx); // se sirve sin validar token
            return;
        }

        // End-points públicos
        var endPoint = ctx.GetEndpoint();
        if (endPoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(ctx);
            return;
        }

        var auth = ctx.Request.Headers["Authorization"].FirstOrDefault();
        if (
            string.IsNullOrWhiteSpace(auth)
            || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Token missing");
            return;
        }

        var token = auth["Bearer ".Length..].Trim();
        var session = await validator.ValidateTokenAsync(token); // 👈  usa _validator

        if (session is null)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Invalid or expired session");
            return;
        }

        // Creamos identidad
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new("SessionToken", session.TokenRequest),
        };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionToken"));

        await _next(ctx);
    }
}
