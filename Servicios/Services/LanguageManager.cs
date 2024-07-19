using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.BLL;
using Servicios.Services;

namespace Servicios.Services
{
    internal sealed class LanguageManager
    {
        #region Singleton
        private readonly static LanguageManager _instance = new LanguageManager();

        public static LanguageManager Current
        {
            get
            {
                return _instance;
            }
        }

        private LanguageManager()
        {
            //Implement here the initialization code
        }
        #endregion


        public string Traducir(string clave)
        {
            return BLLLanguage.Current.Traducir(clave);
        }

    }
}
