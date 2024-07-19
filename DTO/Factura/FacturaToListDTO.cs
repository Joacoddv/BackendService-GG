using Dominio;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Factura
{
    public class FacturaToListDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Factura { get; set; }

        public int Numero_Factura { get; set; }

        public DateTime Fecha_Alta_Factura { get; set; }

        public Dominio.Cliente Cliente { get; set; }

        public EEstadoFactura Estado { get; set; }

        public decimal Sub_Total { get; set; }

        public decimal Total_Iva { get; set; }

        public decimal Total_Factura { get; set; }
    }
}
