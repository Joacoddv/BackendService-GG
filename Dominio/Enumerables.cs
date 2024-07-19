using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{

    public enum EEstadoPedido { Creado = 1, Modificado = 2, Preparandose = 3, Listo = 4, Entregado = 5, Cancelado = 6, }
    public enum EEstadoFactura { Creada = 1, Cancelada = 2, Pagada = 3 }
    public enum EEstadoFacturaPedido { No_Facturado = 1, Facturado = 2, Pagado = 3 }
    public enum EEstadoOT { No_Facturado = 1, Facturado = 2, Pagado = 3 }

    public enum ETipo_Pedido { Pedido_Salon = 1, Pedido_Delivery = 2, Pedidoi_Take_Away = 3 }


}


