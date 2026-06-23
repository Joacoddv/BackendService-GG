using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Facturacion.CrearFactura;

/// <summary>
/// Encodes which comprobante types are allowed per client fiscal condition.
/// </summary>
public static class ReglasComprobante
{
    public static bool EsPermitido(CondicionIVA condicion, TipoComprobanteSolicitado tipo)
    {
        if (condicion == CondicionIVA.ConsumidorFinal)
            return tipo == TipoComprobanteSolicitado.TicketInterno;

        // ResponsableInscripto, Monotributista, ExentoIVA: all types allowed
        return true;
    }
}
