using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Domain
{
    public class Log
    {
        public string Message { get; set; }

        public string Usuario { get; set; }

        public string Severity { get; set; }

        public DateTime Fecha { get; set; }


        public Log(string message, string usuario, string severity, DateTime fecha)
        {
            Message = message;
            Usuario = usuario;
            Severity = severity;
            Fecha = fecha;

        }

        public Log()
        { }
    }
}
