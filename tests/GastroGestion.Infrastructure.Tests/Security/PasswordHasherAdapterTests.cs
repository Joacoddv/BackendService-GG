using FluentAssertions;
using GastroGestion.Domain.Enums;
using Xunit;
using GastroGestion.Domain.Usuarios;
using GastroGestion.Infrastructure.Security;

namespace GastroGestion.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for PasswordHasherAdapter. Covers AUTH-02.4 scenarios A–B.
/// </summary>
public class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter _sut = new();

    private static Usuario MakeUser()
        => Usuario.Crear("test@test.com", "Test User", RolUsuario.Administrador, "PLACEHOLDER_HASH");

    // AUTH-02-A: hash + verify round-trip succeeds
    [Fact]
    public void Hash_ThenVerify_WithSamePassword_ReturnsTrue()
    {
        var usuario = MakeUser();
        var hash = _sut.Hash(usuario, "correct-password");

        var result = _sut.Verify(usuario, hash, "correct-password");

        result.Should().BeTrue();
    }

    // AUTH-02-B: verification with wrong password fails
    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var usuario = MakeUser();
        var hash = _sut.Hash(usuario, "correct");

        var result = _sut.Verify(usuario, hash, "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesDifferentHashEachTime()
    {
        var usuario = MakeUser();

        var hash1 = _sut.Hash(usuario, "same-password");
        var hash2 = _sut.Hash(usuario, "same-password");

        // PBKDF2 uses random salt — two hashes of same password must differ
        hash1.Should().NotBe(hash2);
    }
}
