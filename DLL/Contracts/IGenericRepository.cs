using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Contracts
{
    /// <summary>
    /// Interface para generar DAO's dinámicamente
    /// </summary>
    /// 

    //Introducción a los tipos genéricos (Tipo T), generic types...
    public interface IGenericRepository<T>
    {
        //Pensamos cómo sería el patrón CRUD?
        void Insert(T obj);

        void Delete(T obj);

        void Update(T obj);

        T GetOne(T obj);

        IEnumerable<T> GetAll(T obj);
    }
}
