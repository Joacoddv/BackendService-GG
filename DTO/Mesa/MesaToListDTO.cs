using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Mesa
{
    public class MesaToListDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Mesa { get; set; }

        public int Numero_Mesa { get; set; }

        public string Ubicacion_Mesa { get; set; }

        public int Cantidad { get; set; }
    }
}
