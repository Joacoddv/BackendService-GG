using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Domain.Usuario_Patente_Familia
{
    public class Patente : PatenteFamilia
    {
        public Guid IdPatente { get; set; }
        public override int CantidadHijos => 0;
        public override void Agregar(PatenteFamilia component)
        {
            throw new Exception("No se pueden agregar elementos en una patente.");
        }
        public override void Remover(PatenteFamilia component)
        {
            throw new Exception("No se pueden quitar elementos en una patente.");
        }


    }
}
