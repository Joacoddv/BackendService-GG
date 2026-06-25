using FluentAssertions;
using GastroGestion.Domain.Bitacora;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Unit tests for the BitacoraEntry aggregate factory.
/// </summary>
public sealed class BitacoraEntryTests
{
    private static readonly Guid   SomeUserId = Guid.NewGuid();
    private const string           SomeEmail  = "user@gastrogestion.local";
    private const string           SomeAccion = "Create client";

    // ── Registrar happy path ─────────────────────────────────────────────────

    [Fact]
    public void Registrar_WithValidData_CreatesEntryWithCorrectProperties()
    {
        var fechaUtc = DateTime.UtcNow;

        var entry = BitacoraEntry.Registrar(
            SomeUserId,
            SomeEmail,
            RolUsuario.Administrador,
            SomeAccion,
            detalle: "some detail",
            resultadoHttp: 201,
            fechaUtc: fechaUtc);

        entry.Id.Should().NotBe(Guid.Empty);
        entry.UsuarioId.Should().Be(SomeUserId);
        entry.Email.Should().Be(SomeEmail);
        entry.Rol.Should().Be(RolUsuario.Administrador);
        entry.Accion.Should().Be(SomeAccion);
        entry.Detalle.Should().Be("some detail");
        entry.ResultadoHttp.Should().Be(201);
        entry.FechaUtc.Should().Be(fechaUtc);
    }

    [Fact]
    public void Registrar_WithNullDetalle_CreatesEntryWithNullDetalle()
    {
        var entry = BitacoraEntry.Registrar(
            SomeUserId, SomeEmail, RolUsuario.Cajero, SomeAccion,
            detalle: null, resultadoHttp: 200, fechaUtc: DateTime.UtcNow);

        entry.Detalle.Should().BeNull();
    }

    // ── Registrar guard: empty Accion ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_WithEmptyAccion_ThrowsDomainException(string accion)
    {
        var act = () => BitacoraEntry.Registrar(
            SomeUserId, SomeEmail, RolUsuario.Mozo, accion,
            detalle: null, resultadoHttp: 200, fechaUtc: DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*Accion*");
    }

    // ── RegistrarAnonimo ──────────────────────────────────────────────────────

    [Fact]
    public void RegistrarAnonimo_SetsUsuarioIdToEmptyAndNullRol()
    {
        var entry = BitacoraEntry.RegistrarAnonimo(
            email: SomeEmail,
            accion: "Login failed",
            detalle: null,
            resultadoHttp: 401,
            fechaUtc: DateTime.UtcNow);

        entry.Id.Should().NotBe(Guid.Empty);
        entry.UsuarioId.Should().Be(Guid.Empty);
        entry.Email.Should().Be(SomeEmail);
        entry.Rol.Should().BeNull();
        entry.Accion.Should().Be("Login failed");
        entry.ResultadoHttp.Should().Be(401);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarAnonimo_WithEmptyAccion_ThrowsDomainException(string accion)
    {
        var act = () => BitacoraEntry.RegistrarAnonimo(
            SomeEmail, accion, detalle: null, resultadoHttp: 401, fechaUtc: DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*Accion*");
    }

    // ── Two calls produce distinct ids ────────────────────────────────────────

    [Fact]
    public void Registrar_TwoCalls_ProduceDistinctIds()
    {
        var a = BitacoraEntry.Registrar(SomeUserId, SomeEmail, RolUsuario.Administrador,
            SomeAccion, null, 200, DateTime.UtcNow);
        var b = BitacoraEntry.Registrar(SomeUserId, SomeEmail, RolUsuario.Administrador,
            SomeAccion, null, 200, DateTime.UtcNow);

        a.Id.Should().NotBe(b.Id);
    }
}
