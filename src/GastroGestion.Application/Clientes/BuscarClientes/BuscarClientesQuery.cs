namespace GastroGestion.Application.Clientes.BuscarClientes;

public sealed record BuscarClientesQuery(string? Nombre, bool IncluirInactivos);
