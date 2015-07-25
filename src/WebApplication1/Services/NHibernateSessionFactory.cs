using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using WebApiNHibernate.Entities;

namespace WebApiNHibernate.Services
{
	public static class NHibernateSessionFactory
	{
		public static void AddNHibernateSessionFactory(this IServiceCollection services)
		{
			// By default NHibernate looks for hibernate.cfg.xml
			// otherwise for Web it will fallback to web.config
			// we got one under wwwroot/web.config
			Configuration config = new Configuration();
			config.Configure();

			// Auto load entity mapping class
			ModelMapper mapper = new ModelMapper();
			mapper.AddMappings(Assembly.GetAssembly(typeof(Employee)).GetExportedTypes());

			HbmMapping mapping = mapper.CompileMappingForAllExplicitlyAddedEntities();
			config.AddDeserializedMapping(mapping, "NHibernate.Mapping");
			
			SchemaMetadataUpdater.QuoteTableAndColumns(config);

			// Drop & Recreate database schema
			new SchemaExport(config).Drop(false, true);
			new SchemaExport(config).Create(false, true);

			// Register services
			services.AddSingleton<ISessionFactory>(provider => config.BuildSessionFactory());
			services.AddTransient<ISession>(provider => services.BuildServiceProvider().GetService<ISessionFactory>().OpenSession());
		}
	}
}
