	public static class NHibernateConfigurationExtensions
	{
		private static readonly PropertyInfo tableMappingsProperty = typeof (Configuration).GetProperty("TableMappings",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		private static readonly FieldInfo indexesField = typeof (Table).GetField("indexes",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		public static void CreateIndexesForForeignKeys(this Configuration configuration,
			Func<string, bool> includeTablePredicate)
		{
			configuration.BuildMappings();
			var tables = (ICollection<Table>) tableMappingsProperty.GetValue(configuration, null);
			foreach (var table in tables.Where(x => includeTablePredicate(x.Name)))
			{
				var columnsOfPk = table.HasPrimaryKey ? table.PrimaryKey.Columns.Select(x => x.Name).ToArray() : new string[0];
				foreach (var foreignKey in table.ForeignKeyIterator)
				{
					if (table.HasPrimaryKey)
					{
						var columnsOfFk = foreignKey.Columns.Select(x => x.Name).ToArray();
						var fkHasSameColumnsOfPk = !columnsOfPk.Except(columnsOfFk).Concat(columnsOfFk.Except(columnsOfPk)).Any();
						if (fkHasSameColumnsOfPk)
						{
							continue;
						}
					}
					var idx = new Index();
					idx.AddColumns(foreignKey.Columns);
					idx.Name = "IX" + foreignKey.Name.Substring(2);
					idx.Table = table;
					table.AddIndex(idx);
				}
			}
		}

		public static void CreateIndexesForForeignKeys(this Configuration configuration)
		{
			CreateIndexesForForeignKeys(configuration, x=> true);
		}

		public static void AddClusteredIndexesWhereNeeded(this Configuration configuration)
		{
			string defaultCatalog = PropertiesHelper.GetString(Environment.DefaultCatalog, configuration.Properties, null);
			string defaultSchema = PropertiesHelper.GetString(Environment.DefaultSchema, configuration.Properties, null);
			var dialect = Dialect.GetDialect(configuration.Properties);
			var tables = (ICollection<Table>) tableMappingsProperty.GetValue(configuration, null);
			foreach (var table in tables)
			{
				var indexes = (Dictionary<string, Index>) indexesField.GetValue(table);
				if (table.HasPrimaryKey || indexes.Count != 1)
				{
					continue;
				}
				Index lonelyIndex = indexes.First().Value;
				indexes.Clear();
				var clusteredIndexCreateSql = SqlCreateClusteredIndexString(dialect, lonelyIndex.Name, table, lonelyIndex.ColumnIterator, defaultCatalog, defaultSchema);
				var indexDropSql = lonelyIndex.SqlDropString(dialect, defaultCatalog, defaultSchema);
				var indexDbObject = new SimpleAuxiliaryDatabaseObject(clusteredIndexCreateSql, indexDropSql);
				configuration.AddAuxiliaryDatabaseObject(indexDbObject);
			}
		}

		private static string SqlCreateClusteredIndexString(Dialect dialect, string name, Table table,
			IEnumerable<Column> columns, string defaultCatalog, string defaultSchema)
		{
			StringBuilder buf = new StringBuilder("CREATE CLUSTERED INDEX ")
				.Append(dialect.QualifyIndexName ? name : StringHelper.Unqualify(name))
				.Append(" ON ")
				.Append(table.GetQualifiedName(dialect, defaultCatalog, defaultSchema))
				.Append(" (")
			  .Append(string.Join(", ", columns.Select(x=> x.GetQuotedName(dialect))))
			  .Append(")");

			return buf.ToString();
		}
	}
