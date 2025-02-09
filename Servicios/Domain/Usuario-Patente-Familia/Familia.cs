﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servicios.Domain.Usuario_Patente_Familia
{
    public class Familia : PatenteFamilia
    {
        private List<PatenteFamilia> patenteFamilias = new List<PatenteFamilia>();


        //Retorno el listado de mis elementos hijos...
        public List<PatenteFamilia> ListadoHijos => patenteFamilias;

        public override int CantidadHijos => patenteFamilias.Count;

        public Guid IdFamilia { get; set; }

        public Familia()
        {

        }
		public Familia(PatenteFamilia patenteFamilia)
		{
			patenteFamilias.Add(patenteFamilia);
		}

		/// 
		/// <param name="component"></param>
		public override void Agregar(PatenteFamilia component)
		{
			patenteFamilias.Add(component);
		}

		/// 
		/// <param name="component"></param>
		public override void Remover(PatenteFamilia component)
		{
			patenteFamilias.Remove(component);
			//Verificar restricción de eliminación, al menos una patente/familia debería de existir
		}


	}
}
