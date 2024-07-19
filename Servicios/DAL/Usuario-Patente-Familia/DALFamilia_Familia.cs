using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Tools;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.DAL.Usuario_Patente_Familia
{
    public sealed class DALFamilia_Familia
    {
        private readonly static DALFamilia_Familia _instance = new DALFamilia_Familia();

        public static DALFamilia_Familia Current
        {
            get
            {
                return _instance;
            }
        }

        private DALFamilia_Familia()
        {
            //Implent here the initialization of your singleton
        }

        //Statements
        #region Statements
        private static string SelectAsignadas
        {
            get => "SELECT A.IdFamiliaHijo, B.Nombre " +
                "FROM [dbo].[Familia_Familia] A " +
                "INNER JOIN [dbo].[Familia] B on B.IdFamilia = A.IdFamiliaHijo " +
                "WHERE A.IdFamilia = @IdFamilia";
        }
        private static string SelectDisponibles
        {
            get => "SELECT A.IdFamilia, A.Nombre " +
                "FROM [dbo].[Familia] A " +
                "WHERE A.IdFamilia not in (SELECT B.IdFamiliaHijo FROM Familia_Familia B WHERE B.IdFamilia = @IdFamilia)";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Familia_Familia] ([IdFamilia],[IdFamiliaHijo]) VALUES (@IdFamilia, @IdFamiliaHijo)";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Familia_Familia] WHERE IdFamilia = @IdFamilia AND IdFamiliaHijo = @IdFamiliaHijo";
        }
        #endregion


        public IEnumerable<Familia> GetFamiliasAsignadas(Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                List<Familia> familias = new List<Familia>();

                using (var dr = SqlHelper.ExecuteReader(SelectAsignadas, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Familia familianueva = new Familia();
                        familianueva.IdFamilia = Guid.Parse(values[0].ToString());
                        familianueva.Nombre = values[1].ToString();

                        familias.Add(familianueva);
                    }
                }
                return familias;
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
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));

                List<Familia> familias = new List<Familia>();

                using (var dr = SqlHelper.ExecuteReader(SelectDisponibles, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Familia familianueva = new Familia();
                        familianueva.IdFamilia = Guid.Parse(values[0].ToString());
                        familianueva.Nombre = values[1].ToString();

                        familias.Add(familianueva);
                    }
                }
                return familias;
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
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@IdFamiliaHijo", familiaHijo.IdFamilia));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteFamiliaFamilia(Familia familia, Familia familiaHijo)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@IdFamiliaHijo", familiaHijo.IdFamilia));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }
    }

}
