using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL;
using Servicios.Services;
using Servicios.Services.Extensions;

namespace Servicios.BLL
{
    public static class BLLBDD
    {
        public static void BackUp(string path, string db)
        {
            try
            {
                DALBDD.Backup(path, db);
                LoggerManager.Current.Write($"BLL BDD - Creando Backup de base de datos {db.ToString()}", EventLevel.Informational);

            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL BDD - Error al crear Backup de base de datos {db.ToString()}", EventLevel.Informational);
                throw new Exception("Error al crear Backup de base de datos".Traducir() + $"{db.ToString()}" + $": {ex.Message}");
            }
        }
        public static void Restore(string path, string db)
        {
            try
            {
                DALBDD.Restore(path, db);
                LoggerManager.Current.Write($"BLL BDD - Restaurando base de datos {db.ToString()}", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL BDD - Error al restaurar base de datos {db.ToString()}", EventLevel.Informational);
                throw new Exception("Error al restaurar base de datos".Traducir() + $"{db.ToString()}" + $": {ex.Message}");
            }
        }
    }
}
