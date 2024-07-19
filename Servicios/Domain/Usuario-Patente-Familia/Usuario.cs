using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Utilities.Collections;
using Servicios.BLL.Usuario_Patente_Familia;

namespace Servicios.Domain.Usuario_Patente_Familia
{
    public class Usuario
    {

        public Guid UserId { get; set; }
        public int Numero_Usuario { get; set; }
        public string Mail { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime Fecha_Alta { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public bool Estado { get; set; }
        public string Idioma { get; set; }

        public List<PatenteFamilia> Permisos { get; set; } = new List<PatenteFamilia>();

        public Usuario()
        {

        }

    }
}
