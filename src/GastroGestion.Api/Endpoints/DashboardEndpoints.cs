using GastroGestion.Application.Dashboard.GetDashboard;
using GastroGestion.Contracts.Dashboard;

namespace GastroGestion.Api.Endpoints;

public static class DashboardEndpoints
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        // GET /dashboard — operational metrics (Admin only)
        group.MapGet("/", async (
            GetDashboardHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetDashboardQuery(), ct);
            return Results.Ok(result.ToResponse());
        })
        .RequireAuthorization("SoloAdministrador");

        return app;
    }
}
