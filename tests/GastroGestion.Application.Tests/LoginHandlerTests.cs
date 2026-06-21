using FluentAssertions;
using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Abstractions.Security;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Usuarios;
using NSubstitute;

namespace GastroGestion.Application.Tests;

/// <summary>
/// Unit tests for LoginHandler. Covers AUTH-03.5 scenarios A–E.
/// All credential failures produce the same exception type (AUTH-03-E).
/// </summary>
public class LoginHandlerTests
{
    private readonly IUsuarioRepository      _userRepo         = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher         _hasher           = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer            _tokens           = Substitute.For<ITokenIssuer>();
    private readonly IRefreshTokenRepository _refreshTokens    = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator  _refreshGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IUnitOfWork             _uow              = Substitute.For<IUnitOfWork>();
    private readonly LoginHandler            _sut;

    private static readonly string ValidHash  = "STORED_HASH";
    private static readonly string ValidEmail = "admin@test.com";

    public LoginHandlerTests()
    {
        // Default: the generator returns a usable token so the success path can build a RefreshToken.
        _refreshGenerator.Generate().Returns(new GeneratedRefreshToken("raw-refresh", "hash-refresh"));
        _sut = new LoginHandler(_userRepo, _hasher, _tokens, _refreshTokens, _refreshGenerator, _uow);
    }

    private static Usuario MakeActiveUser(bool activo = true)
    {
        var u = Usuario.Crear(ValidEmail, "Admin", RolUsuario.Administrador, ValidHash);
        if (!activo) u.Desactivar();
        return u;
    }

    // AUTH-03-A: successful login
    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsLoginResult()
    {
        var usuario = MakeActiveUser();
        _userRepo.GetByEmailAsync(ValidEmail).Returns(usuario);
        _hasher.Verify(usuario, ValidHash, "correct").Returns(true);
        var token = new AccessToken("jwt.token.here", DateTime.UtcNow.AddHours(8));
        _tokens.Issue(usuario).Returns(token);

        var cmd    = new LoginCommand(ValidEmail, "correct");
        var result = await _sut.Handle(cmd);

        result.AccessToken.Should().Be("jwt.token.here");
        result.UsuarioId.Should().Be(usuario.Id);
        result.Rol.Should().Be(RolUsuario.Administrador);
        result.ExpiresAtUtc.Should().BeCloseTo(token.ExpiresAtUtc, TimeSpan.FromSeconds(1));
        result.RefreshToken.Should().Be("raw-refresh");
        result.RefreshTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        await _refreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // AUTH-03-B: unknown email → generic failure
    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsAuthenticationFailedException()
    {
        _userRepo.GetByEmailAsync("ghost@test.com").Returns((Usuario?)null);

        var act = async () => await _sut.Handle(new LoginCommand("ghost@test.com", "any"));
        await act.Should().ThrowAsync<AuthenticationFailedException>()
                          .WithMessage("Invalid credentials.");
    }

    // AUTH-03-C: wrong password → same generic failure
    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsAuthenticationFailedException()
    {
        var usuario = MakeActiveUser();
        _userRepo.GetByEmailAsync(ValidEmail).Returns(usuario);
        _hasher.Verify(usuario, ValidHash, "wrong").Returns(false);

        var act = async () => await _sut.Handle(new LoginCommand(ValidEmail, "wrong"));
        await act.Should().ThrowAsync<AuthenticationFailedException>()
                          .WithMessage("Invalid credentials.");
    }

    // AUTH-03-D: inactive user → same generic failure
    [Fact]
    public async Task Handle_WithInactiveUser_ThrowsAuthenticationFailedException()
    {
        var usuario = MakeActiveUser(activo: false);
        _userRepo.GetByEmailAsync(ValidEmail).Returns(usuario);

        var act = async () => await _sut.Handle(new LoginCommand(ValidEmail, "correct"));
        await act.Should().ThrowAsync<AuthenticationFailedException>()
                          .WithMessage("Invalid credentials.");
    }

    // AUTH-03-E: all three failure paths produce structurally identical exceptions
    [Fact]
    public async Task Handle_AllFailurePaths_ProduceSameExceptionTypeAndMessage()
    {
        // Arrange three scenarios
        _userRepo.GetByEmailAsync("ghost@test.com").Returns((Usuario?)null);

        var inactive = MakeActiveUser(activo: false);
        _userRepo.GetByEmailAsync("inactive@test.com").Returns(inactive);

        var activeUser = MakeActiveUser();
        _userRepo.GetByEmailAsync(ValidEmail).Returns(activeUser);
        _hasher.Verify(activeUser, ValidHash, "wrong").Returns(false);

        var unknownAct  = async () => await _sut.Handle(new LoginCommand("ghost@test.com",    "any"));
        var inactiveAct = async () => await _sut.Handle(new LoginCommand("inactive@test.com", "correct"));
        var wrongPwAct  = async () => await _sut.Handle(new LoginCommand(ValidEmail,          "wrong"));

        // All three MUST throw exactly AuthenticationFailedException with identical messages
        var ex1 = await Assert.ThrowsAsync<AuthenticationFailedException>(unknownAct);
        var ex2 = await Assert.ThrowsAsync<AuthenticationFailedException>(inactiveAct);
        var ex3 = await Assert.ThrowsAsync<AuthenticationFailedException>(wrongPwAct);

        ex1.Message.Should().Be(ex2.Message).And.Be(ex3.Message);
    }
}
