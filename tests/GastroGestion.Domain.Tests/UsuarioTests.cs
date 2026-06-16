using FluentAssertions;
using GastroGestion.Domain.Common;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;

namespace GastroGestion.Domain.Tests;

/// <summary>
/// Unit tests for the Usuario aggregate factory. Covers AUTH-01.3 scenarios A–E.
/// </summary>
public class UsuarioTests
{
    private const string ValidEmail    = "admin@gastrogestion.local";
    private const string ValidName     = "Admin User";
    private const string ValidHash     = "PBKDF2HASH==";

    // AUTH-01-A: happy path
    [Fact]
    public void Crear_WithValidData_CreatesActiveUsuario()
    {
        var usuario = Usuario.Crear(ValidEmail, ValidName, RolUsuario.Administrador, ValidHash);

        usuario.Email.Should().Be(ValidEmail);
        usuario.NombreCompleto.Should().Be(ValidName);
        usuario.Rol.Should().Be(RolUsuario.Administrador);
        usuario.PasswordHash.Should().Be(ValidHash);
        usuario.Activo.Should().BeTrue();
        usuario.Id.Should().NotBe(Guid.Empty);
    }

    // AUTH-01-B: empty email
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithEmptyOrWhitespaceEmail_ThrowsDomainException(string email)
    {
        var act = () => Usuario.Crear(email, ValidName, RolUsuario.Mozo, ValidHash);
        act.Should().Throw<DomainException>().WithMessage("*Email*");
    }

    // AUTH-01-C: malformed email (no @ or empty parts)
    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("noat@")]
    public void Crear_WithMalformedEmail_ThrowsDomainException(string email)
    {
        var act = () => Usuario.Crear(email, ValidName, RolUsuario.Cajero, ValidHash);
        act.Should().Throw<DomainException>().WithMessage("*email*");
    }

    // AUTH-01-D: empty nombre
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithEmptyNombreCompleto_ThrowsDomainException(string nombre)
    {
        var act = () => Usuario.Crear(ValidEmail, nombre, RolUsuario.Cocinero, ValidHash);
        act.Should().Throw<DomainException>().WithMessage("*NombreCompleto*");
    }

    // AUTH-01-E: empty hash
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_WithEmptyPasswordHash_ThrowsDomainException(string hash)
    {
        var act = () => Usuario.Crear(ValidEmail, ValidName, RolUsuario.Administrador, hash);
        act.Should().Throw<DomainException>().WithMessage("*PasswordHash*");
    }

    [Fact]
    public void Desactivar_SetsActivoFalse()
    {
        var usuario = Usuario.Crear(ValidEmail, ValidName, RolUsuario.Administrador, ValidHash);

        usuario.Desactivar();

        usuario.Activo.Should().BeFalse();
    }

    [Fact]
    public void Desactivar_IsIdempotent()
    {
        var usuario = Usuario.Crear(ValidEmail, ValidName, RolUsuario.Administrador, ValidHash);
        usuario.Desactivar();

        var act = () => usuario.Desactivar();
        act.Should().NotThrow();
        usuario.Activo.Should().BeFalse();
    }
}
