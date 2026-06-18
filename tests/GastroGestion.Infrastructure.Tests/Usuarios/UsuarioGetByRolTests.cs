using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using GastroGestion.Infrastructure.Persistence.Repositories;
using GastroGestion.Infrastructure.Tests.Common;
using Xunit;

namespace GastroGestion.Infrastructure.Tests.Usuarios;

/// <summary>
/// Integration tests for UsuarioRepository.GetByRolAsync (CCC-T11).
/// Verifies that the EF implementation returns only active Cocinero users.
/// </summary>
public sealed class UsuarioGetByRolTests : IClassFixture<LocalDbFixture>
{
    private readonly LocalDbFixture _fixture;

    public UsuarioGetByRolTests(LocalDbFixture fixture) => _fixture = fixture;

    private static Usuario CreateUsuario(string email, RolUsuario rol, bool activo = true)
    {
        var u = Usuario.Crear(email, $"User {email}", rol, "hash-placeholder");
        if (!activo)
            u.Desactivar();
        return u;
    }

    /// <summary>
    /// CCC-A01 — GetByRolAsync returns only active users whose role matches.
    /// </summary>
    [Fact]
    public async Task GetByRolAsync_ReturnsOnlyActiveCocineros()
    {
        // Arrange
        var activeCocinero1  = CreateUsuario("cocinero1@test.local",  RolUsuario.Cocinero);
        var activeCocinero2  = CreateUsuario("cocinero2@test.local",  RolUsuario.Cocinero);
        var inactiveCocinero = CreateUsuario("cocinero3@test.local",  RolUsuario.Cocinero, activo: false);
        var mozo             = CreateUsuario("mozo1@test.local",      RolUsuario.Mozo);
        var admin            = CreateUsuario("admin1@test.local",     RolUsuario.Administrador);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Usuarios.AddRangeAsync(
                activeCocinero1, activeCocinero2, inactiveCocinero, mozo, admin);
            await saveCtx.SaveChangesAsync();
        }

        // Act
        await using var readCtx = _fixture.CreateContext();
        var repo   = new UsuarioRepository(readCtx);
        var result = await repo.GetByRolAsync(RolUsuario.Cocinero);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == activeCocinero1.Id);
        Assert.Contains(result, u => u.Id == activeCocinero2.Id);
    }

    /// <summary>
    /// CCC-A01 — Inactive cocinero is excluded from results.
    /// </summary>
    [Fact]
    public async Task GetByRolAsync_ExcludesInactiveCocinero()
    {
        // Arrange
        var inactiveCocinero = CreateUsuario("inactive.cocinero@test.local", RolUsuario.Cocinero, activo: false);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Usuarios.AddAsync(inactiveCocinero);
            await saveCtx.SaveChangesAsync();
        }

        // Act
        await using var readCtx = _fixture.CreateContext();
        var repo   = new UsuarioRepository(readCtx);
        var result = await repo.GetByRolAsync(RolUsuario.Cocinero);

        // Assert — inactive cocinero must not appear
        Assert.DoesNotContain(result, u => u.Id == inactiveCocinero.Id);
    }

    /// <summary>
    /// CCC-A01 — Non-Cocinero roles are excluded even if active.
    /// </summary>
    [Fact]
    public async Task GetByRolAsync_ExcludesNonCocineroRoles()
    {
        // Arrange
        var mozo  = CreateUsuario("mozo.exclude@test.local",  RolUsuario.Mozo);
        var cajero = CreateUsuario("cajero.exclude@test.local", RolUsuario.Cajero);

        await using (var saveCtx = _fixture.CreateContext())
        {
            await saveCtx.Usuarios.AddRangeAsync(mozo, cajero);
            await saveCtx.SaveChangesAsync();
        }

        // Act
        await using var readCtx = _fixture.CreateContext();
        var repo   = new UsuarioRepository(readCtx);
        var result = await repo.GetByRolAsync(RolUsuario.Cocinero);

        // Assert — neither mozo nor cajero should appear
        Assert.DoesNotContain(result, u => u.Id == mozo.Id);
        Assert.DoesNotContain(result, u => u.Id == cajero.Id);
    }
}
