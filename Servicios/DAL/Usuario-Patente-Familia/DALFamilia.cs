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

    internal sealed class DALFamilia
    {
        private readonly static DALFamilia _instance = new DALFamilia();

        public static DALFamilia Current
        {
            get
            {
                return _instance;
            }
        }

        private DALFamilia()
        {
            //Implent here the initialization of your singleton
        }

        //Statements
        #region Statements
        private static string SelectAll
        {
            get => "SELECT IdFamilia, Nombre FROM [dbo].[Familia]";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Familia] ([IdFamilia],[Nombre]) VALUES (@IdFamilia, @Nombre)";
        }
        private static string Update
        {
            get => "UPDATE [dbo].[Familia] SET Nombre = @Nombre WHERE IdFamilia = @IdFamilia";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Familia] WHERE IdFamilia = @IdFamilia";
        }

        private static string SelectOne
        {
            get => "SELECT IdFamilia, Nombre FROM [dbo].[Familia] WHERE Nombre = @Nombre";
        }
        #endregion


        public Familia GetOne(Familia obj)
        {
            List<SqlParameter> p = new List<SqlParameter>();

            p.Add(new SqlParameter("@Nombre", obj.Nombre));

            using (var dr = SqlHelper.ExecuteReader(SelectOne, CommandType.Text, p.ToArray()))
            {
                Familia familia = new Familia();
                Object[] values = new Object[dr.FieldCount];

                while (dr.Read())
                {
                    dr.GetValues(values);

                    familia.IdFamilia = Guid.Parse(values[0].ToString());
                    familia.Nombre = values[1].ToString();

                }
                return familia;
            }
        }








        public IEnumerable<Familia> GetAll()
        {
            try
            {
                List<Familia> familias = new List<Familia>();

                using (var dr = SqlHelper.ExecuteReader(SelectAll, CommandType.Text))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Familia familia = new Familia();
                        familia.IdFamilia = Guid.Parse((values[0].ToString().Trim()));
                        familia.Nombre = values[1].ToString();

                        familias.Add(familia);
                    }
                }
                return familias;
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
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@Nombre", familia.Nombre));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void UpdateFamilia(Familia familia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", familia.IdFamilia));
                p.Add(new SqlParameter("@Nombre", familia.Nombre));

                SqlHelper.ExecuteNonQuery(Update, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteFamilia(string idFamilia)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdFamilia", idFamilia));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {

            }
        }
    }

}
