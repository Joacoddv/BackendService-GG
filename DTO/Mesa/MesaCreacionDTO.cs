using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Mesa
{
    public class MesaCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Mesa { get; set; } = Guid.NewGuid();

        public string Ubicacion_Mesa { get; set; }

        public int Cantidad { get; set; }


        public MesaCreacionDTO()
        {
            Id_Mesa = Guid.NewGuid();

        }
    }
}
