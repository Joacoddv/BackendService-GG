using GastroGestion.Application.Clientes.Cumpleaneros;

namespace GastroGestion.Contracts.Clientes;

public sealed record CumpleaneroResponse(Guid Id, string Nombre, string? Email, DateOnly FechaNacimiento);

public sealed record EnviarPromoResponse(int Enviados, int SinEmail);

public static class CumpleanerosMappings
{
    public static CumpleaneroResponse ToResponse(this CumpleaneroResult r)
        => new(r.Id, r.Nombre, r.Email, r.FechaNacimiento);

    public static EnviarPromoResponse ToResponse(this EnviarPromoResult r)
        => new(r.Enviados, r.SinEmail);
}
