using Dominio;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Factura
{
    public class FacturaCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Factura { get; set; }

        public DateTime Fecha_Alta_Factura { get; set; }

        public Dominio.Cliente Cliente { get; set; }

        public EEstadoFactura Estado { get; set; }

        public decimal Sub_Total { get; set; }

        public decimal Total_Iva { get; set; }

        public decimal Total_Factura { get; set; }

        public FacturaCreacionDTO()
        {
            Id_Factura = Guid.NewGuid();
            Fecha_Alta_Factura = DateTime.Now;
            Estado = EEstadoFactura.Creada;
            Sub_Total = 0;
            Total_Iva = 0;
            Total_Factura = 0;
        }
    }
}
