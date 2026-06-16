namespace GastroGestion.Application.Auth.Login;

/// <summary>Command for the login use case. Carries raw (unhashed) credentials.</summary>
public sealed record LoginCommand(string Email, string Password);
