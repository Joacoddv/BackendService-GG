namespace GastroGestion.Contracts.Auth;

/// <summary>Request body for POST /auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);
