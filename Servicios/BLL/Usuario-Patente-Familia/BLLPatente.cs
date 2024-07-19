using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Usuario_Patente_Familia;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.BLL.Usuario_Patente_Familia
{

    public sealed class BLLPatente
    {
        private readonly static BLLPatente _instance = new BLLPatente();

        public static BLLPatente Current
        {
            get
            {
                return _instance;
            }
        }

        private BLLPatente()
        {
            //Implent here the initialization of your singleton
        }

        public IEnumerable<Patente> GetAll()
        {
            try
            {
                return DALPatente.Current.GetAll();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void AddPatente(Patente patente)
        {
            try
            {
                DALPatente.Current.AddPatente(patente);

            }
            catch (Exception ex)
            {
            }
        }

        public void UpdatePatente(Patente patente)
        {
            try
            {
                DALPatente.Current.UpdatePatente(patente);

            }
            catch (Exception ex)
            {

            }
        }

        public void DeletePatente(Patente patente)
        {
            try
            {
                DALPatente.Current.DeletePatente(patente);

            }
            catch (Exception ex)
            {

            }
        }


        public List<Patente> HidratarPatentes(List<Patente> patentes)
        {
            foreach (var item in patentes)
            {
                DALPatente.Current.GetOne(item);
            }
            return patentes;
        }
    }

}
