namespace GastroGestion.Domain.Enums;

/// <summary>
/// Argentine fiscal condition of a <see cref="GastroGestion.Domain.Clientes.Cliente"/>.
/// Drives which comprobante type the client can receive.
/// </summary>
public enum CondicionIVA
{
    ResponsableInscripto = 0,
    Monotributista       = 1,
    ConsumidorFinal      = 2,
    ExentoIVA            = 3
}
