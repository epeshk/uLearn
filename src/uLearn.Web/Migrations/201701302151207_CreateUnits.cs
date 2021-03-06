using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using uLearn.Web.DataContexts;

namespace uLearn.Web.Migrations
{
	using System;
	using System.Data.Entity.Migrations;
	
	public partial class CreateUnits : DbMigration
	{
		public override void Up()
		{
			AddColumn("dbo.UnitAppearances", "UnitId", c => c.Guid(nullable: false));

			var db = new ULearnDb();
			var connection = db.Database.Connection as SqlConnection;
			if (connection == null)
				throw new Exception("Can\'t open database connection to migrate units");

			var unitAppearancesNewUnitsIds = new Dictionary<int, Guid>();

			connection.Open();
			var selectCommand = new SqlCommand("SELECT * FROM dbo.UnitAppearances", connection);
			var reader = selectCommand.ExecuteReader();
			while (reader.Read())
			{
				var id = (int) reader["Id"];
				var unitName = reader["UnitName"] as string;
				var newUnitId = unitName.ToDeterministicGuid();

				unitAppearancesNewUnitsIds[id] = newUnitId;
			}
			reader.Close();

			var allOk = unitAppearancesNewUnitsIds.All(kv =>
			{
				Sql($"UPDATE dbo.UnitAppearances SET UnitId = '{kv.Value}' WHERE Id = {kv.Key}");
				
				return true;
			});

			DropColumn("dbo.UnitAppearances", "UnitName");
		}
		
		public override void Down()
		{
			AddColumn("dbo.UnitAppearances", "UnitName", c => c.String(nullable: false));
			DropColumn("dbo.UnitAppearances", "UnitId");
		}
	}
}
