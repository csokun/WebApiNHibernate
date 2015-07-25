using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using WebApiNHibernate.Entities;

namespace WebApiNHibernate.Mappings
{
	public class EmployeeMap : ClassMapping<Employee>
	{
		public EmployeeMap()
		{
			Id(p => p.Id, map => map.Generator(Generators.Identity));
			Property(p => p.Name);
		}
	}
}
