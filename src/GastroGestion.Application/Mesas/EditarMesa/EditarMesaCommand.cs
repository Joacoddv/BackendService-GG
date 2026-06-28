namespace GastroGestion.Application.Mesas.EditarMesa;

/// <summary>
/// Command to update a mesa's Numero and Capacidad.
/// </summary>
/// <param name="Id">Primary key of the mesa to update.</param>
/// <param name="Numero">New table number — must be greater than zero and unique.</param>
/// <param name="Capacidad">New seating capacity — must be greater than zero.</param>
public sealed record EditarMesaCommand(Guid Id, int Numero, int Capacidad);
