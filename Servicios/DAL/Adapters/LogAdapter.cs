using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.Domain;

namespace Servicios.DAL.Adapters
{
    public sealed class LogAdapter
    {
        private readonly static LogAdapter _instance = new LogAdapter();

        public static LogAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private LogAdapter()
        { 
            //Implent here the initialization of your singleton
        }

        public Log Adapt(object[] values)
        {
            return new Log()
            {
                Message = values[0].ToString(),
                Fecha = Convert.ToDateTime(values[1]),
                Usuario = values[2].ToString(),
                Severity = values[3].ToString()
            };
        }
    }
}
