using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Usuarios
{
    public class UsuarioCreacionDTO
    {
        public string Mail { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Password { get; set; }
        public bool Estado { get; set; }
        public string Idioma { get; set; }

        public UsuarioCreacionDTO()
        {
            Estado = true;
        }
    }
}
