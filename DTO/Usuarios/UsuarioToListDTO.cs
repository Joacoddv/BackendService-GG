using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Usuarios
{
    public class UsuarioToListDTO
    {
        //public Guid UserId { get; set; }
        public int Numero_Usuario { get; set; }
        public string Mail { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime Fecha_Alta { get; set; }
        public bool Estado { get; set; }
        public string Idioma { get; set; }
    }
}
