using FluentValidation;
using GastroGestion.Application.Proveedores.CrearProveedor;
using GastroGestion.Application.Proveedores.EditarProveedor;
using GastroGestion.Domain.Proveedores;

namespace GastroGestion.Contracts.Proveedores;

// ── Requests ────────────────────────────────────────────────────────────────

/// <summary>Request DTO for POST /proveedores. Only Nombre is required.</summary>
public sealed record CrearProveedorRequest(string Nombre, string? Cuit, string? Email, string? Telefono);

/// <summary>Request DTO for PUT /proveedores/{id}.</summary>
public sealed record EditarProveedorRequest(string Nombre, string? Cuit, string? Email, string? Telefono);

// ── Response ──────────────────────────────────────────────────────────────────

public sealed record ProveedorResponse(
    Guid Id,
    string Nombre,
    string? Cuit,
    string? Email,
    string? Telefono,
    bool Activo);

// ── Mappings ──────────────────────────────────────────────────────────────────

public static class ProveedorMappings
{
    public static CrearProveedorCommand ToCommand(this CrearProveedorRequest r)
        => new(r.Nombre, r.Cuit, r.Email, r.Telefono);

    public static EditarProveedorCommand ToCommand(this EditarProveedorRequest r, Guid id)
        => new(id, r.Nombre, r.Cuit, r.Email, r.Telefono);

    public static ProveedorResponse ToResponse(this Proveedor p)
        => new(p.Id, p.Nombre, p.Cuit?.Valor, p.Email?.Valor, p.Telefono, p.Activo);
}

// ── Validators ────────────────────────────────────────────────────────────────

public sealed class CrearProveedorValidator : AbstractValidator<CrearProveedorRequest>
{
    public CrearProveedorValidator()
        => RuleFor(x => x.Nombre).NotEmpty().WithMessage("Nombre is required.");
}

public sealed class EditarProveedorValidator : AbstractValidator<EditarProveedorRequest>
{
    public EditarProveedorValidator()
        => RuleFor(x => x.Nombre).NotEmpty().WithMessage("Nombre is required.");
}
