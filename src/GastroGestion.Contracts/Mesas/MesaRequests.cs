namespace GastroGestion.Contracts.Mesas;

public sealed record CrearMesaRequest(int Numero, int Capacidad);

/// <summary>Request DTO for editing an existing Mesa (PUT /mesas/{id}).</summary>
public sealed record EditarMesaRequest(int Numero, int Capacidad);

/// <summary>Request DTO for setting floor-plan coordinates (PUT /mesas/{id}/posicion).</summary>
public sealed record UbicarMesaRequest(int X, int Y);
