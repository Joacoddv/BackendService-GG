using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Factura
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Factura { get; set; }

        public int Numero_Factura { get; set; }

        public DateTime Fecha_Alta_Factura { get; set; }

        public Cliente Cliente { get; set; }

        public EEstadoFactura Estado { get; set; }

        public decimal Sub_Total { get; set; }

        public decimal Total_Iva { get; set; }

        public decimal Total_Factura { get; set; }


        public Factura(Guid id_Empresa, Guid id_Sucursal, Guid id_factura, int numero_factura, DateTime fecha_alta_factura, Cliente cliente, EEstadoFactura estado, decimal subtotal, decimal total_iva, decimal total_factura)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Factura = id_factura;
            Numero_Factura = numero_factura;
            Fecha_Alta_Factura = fecha_alta_factura;
            Cliente = cliente;
            Estado = estado;
            Sub_Total = subtotal;
            Total_Iva = total_iva;
            Total_Factura = total_factura;

        }

        public Factura ()
        { }
    }
}
