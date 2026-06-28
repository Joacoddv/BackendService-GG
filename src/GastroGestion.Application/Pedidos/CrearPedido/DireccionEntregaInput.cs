namespace GastroGestion.Application.Pedidos.CrearPedido;

public sealed record DireccionEntregaInput(
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento,
    string? Zona = null);
