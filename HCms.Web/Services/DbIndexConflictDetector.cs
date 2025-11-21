using System;
using HCms.Infrastructure.Data;


namespace HCms.Web.Services
{
	public class DbIndexConflictDetector : IDbIndexConflictDetector
	{
		public bool ConflictDetected(Exception ex)
		{
			return ex.InnerException switch
			{
				Microsoft.Data.SqlClient.SqlException sqlEx => sqlEx.Number == 2601 || sqlEx.Number == 2627,

				Npgsql.PostgresException pgEx => pgEx.SqlState == "23505",

				MySql.Data.MySqlClient.MySqlException myEx => myEx.Number == 1062,

				_ => false
			};
		}
	}
}