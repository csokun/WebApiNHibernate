using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using NHibernate;
using WebApiNHibernate.Entities;

namespace WebApiNHibernate.Controllers
{
	[Route("api/[controller]")]
	public class EmployeeController: Controller
	{
		private ISession session;

		public EmployeeController(ISession session)
		{
			this.session = session;
		}

		[HttpPost]
		public void Post(string Name)
		{
			using (var tx = session.BeginTransaction())
			{
				var emp = new Employee();
				emp.Name = Name;

        session.Save(emp);
				tx.Commit();
			}
		}

		[HttpGet]
		public IEnumerable<Employee> Get()
		{
			return session.QueryOver<Employee>().Future();
		} 
	}
}
