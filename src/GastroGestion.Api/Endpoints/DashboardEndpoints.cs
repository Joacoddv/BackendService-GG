using GastroGestion.Application.Dashboard.GetDashboard;
using GastroGestion.Contracts.Dashboard;
using GastroGestion.Domain.Enums;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class DashboardEndpoints
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        // GET /dashboard — operational metrics (Admin only)
        group.MapGet("/", async (
            HttpContext http,
            GetDashboardHandler handler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(title: "Invalid or missing role claim.", statusCode: StatusCodes.Status403Forbidden);

            if (rol is not RolUsuario.Administrador)
                return Results.Problem(title: "Access denied. Required role: Administrador.", statusCode: StatusCodes.Status403Forbidden);

            var result = await handler.Handle(new GetDashboardQuery(), ct);
            return Results.Ok(result.ToResponse());
        });

        return app;
    }
}
