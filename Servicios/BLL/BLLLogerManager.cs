using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL;
using Servicios.Domain;
using Servicios.Services;

namespace Servicios.BLL
{
    class BLLLogerManager
    {
        public void InsertBitacora(Log log)
        {
            try
            {
                DALLoggerManager.Current.Insert(log);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }


        public IEnumerable<Log> GetAll()
        {
            //Listo todos los logs en orden descendente
            LoggerManager.Current.Write($"BLL LoggerManager - Validando listar logs", EventLevel.Informational);
            try
            {
                return from o in DALLoggerManager.Current.GetAll()
                       orderby o.Fecha descending
                       select o;
        }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL LoggerManager  - Error al listar logs: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
    }
}
    }
}
