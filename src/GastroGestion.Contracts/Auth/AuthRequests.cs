namespace GastroGestion.Contracts.Auth;

/// <summary>Request body for POST /auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Request body for POST /auth/refresh.</summary>
public sealed record RefrescarTokenRequest(string RefreshToken);
