using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Usuario_Patente_Familia;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.BLL.Usuario_Patente_Familia
{
    public sealed class BLLFamilia
    {
        private readonly static BLLFamilia _instance = new BLLFamilia();

        public static BLLFamilia Current
        {
            get
            {
                return _instance;
            }
        }

        private BLLFamilia()
        {
            //Implent here the initialization of your singleton
        }

        public IEnumerable<Familia> GetAll()
        {
            try
            {
                return DALFamilia.Current.GetAll();
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public void AddFamilia(Familia familia)
        {
            try
            {
                DALFamilia.Current.AddFamilia(familia);
            }
            catch (Exception ex)
            {
            }
        }

        public void UpdateFamilia(Familia familia)
        {
            try
            {
                DALFamilia.Current.UpdateFamilia(familia);
            }
            catch (Exception ex)
            {
            }
        }

        public Familia GetOne(Familia familia)
        {
            try
            {
                return DALFamilia.Current.GetOne(familia);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IEnumerable<Patente> GetPatentesAsignadasaFamiliaMuestra(Familia familia)
        {
            try
            {
                return DALFamilia_Patente.Current.GetPatentesAsignadasaFamilia(familia);

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public IEnumerable<Patente> GetPatentesAsignadasaFamilia(Familia familia)
        {
            try
            {

                foreach (var item in DALFamilia_Patente.Current.GetPatentesAsignadasaFamilia(familia))
                {
                    familia.Agregar(item);
                }
                return DALFamilia_Patente.Current.GetPatentesAsignadasaFamilia(familia);

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IEnumerable<Patente> GetPatentesDisponiblesenFamilia(Familia familia)
        {
            try
            {
                return DALFamilia_Patente.Current.GetPatentesDisponiblesenFamilia(familia);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void AddFamiliaPatente(Familia familia, Patente patente)
        {
            try
            {
                DALFamilia_Patente.Current.AddFamiliaPatente(familia, patente);

            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteFamiliaPatente(Familia familia, Patente patente)
        {
            try
            {
                DALFamilia_Patente.Current.DeleteFamiliaPatente(familia, patente);

            }
            catch (Exception ex)
            {

            }
        }

        public void HidratarFamilia(Familia familia)
        {
            foreach (var itempatente in BLLFamilia.Current.GetPatentesAsignadasaFamilia(familia))
            {
                if (familia.ListadoHijos.Any(o => o.Nombre.Equals(itempatente.Nombre)))
                {

                }
                else
                {
                    familia.Agregar(itempatente);
                }

            }
            foreach (var itemfamilia in BLLFamilia.Current.GetFamiliasAsignadas(familia))
            {
                if (familia.ListadoHijos.Any(o => o.Nombre.Equals(itemfamilia.Nombre)))
                {

                }
                else
                {
                    familia.Agregar(itemfamilia);
                    HidratarFamilia(itemfamilia);
                }

            }
        }

        public IEnumerable<Familia> GetFamiliasAsignadas(Familia familia)
        {
            try
            {
                return DALFamilia_Familia.Current.GetFamiliasAsignadas(familia);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public IEnumerable<Familia> GetFamiliasAsignadasMuestra(Familia familia)
        {
            try
            {

                return DALFamilia_Familia.Current.GetFamiliasAsignadas(familia);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IEnumerable<Familia> GetFamiliasDisponibles(Familia familia)
        {
            try
            {
                return DALFamilia_Familia.Current.GetFamiliasDisponibles(familia);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public void AddFamiliaFamilia(Familia familia, Familia familiaHijo)
        {
            try
            {
                DALFamilia_Familia.Current.AddFamiliaFamilia(familia, familiaHijo);

            }
            catch (Exception ex)
            {

            }
        }


        public void DeleteFamiliaFamilia(Familia familia, Familia familiaHijo)
        {
            try
            {
                DALFamilia_Familia.Current.DeleteFamiliaFamilia(familia, familiaHijo);

            }
            catch (Exception ex)
            {
            }
        }
    }
}
