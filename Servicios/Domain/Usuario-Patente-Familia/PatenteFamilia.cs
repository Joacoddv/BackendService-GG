using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Domain.Usuario_Patente_Familia
{
    public abstract class PatenteFamilia
    {
        public string Nombre { get; set; }

        public PatenteFamilia()
        {

        }

        public abstract void Agregar(PatenteFamilia component);
        public abstract void Remover(PatenteFamilia component);
        public abstract int CantidadHijos { get; }
    }
}
