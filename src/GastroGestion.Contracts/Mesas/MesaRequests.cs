namespace GastroGestion.Contracts.Mesas;

public sealed record CrearMesaRequest(int Numero, int Capacidad);

/// <summary>Request DTO for editing an existing Mesa (PUT /mesas/{id}).</summary>
public sealed record EditarMesaRequest(int Numero, int Capacidad);
