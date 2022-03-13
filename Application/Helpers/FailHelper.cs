using System.Data.SqlClient;
using Dapper;

namespace Application.Helpers;

internal static class FailHelper
{
	public static bool IsFail()
	{
		_connection.Execute("" +
		                    "IF NOT EXISTS (SELECT 1 FROM [Executions] WHERE [Uid] = @uid)" +
		                    "INSERT INTO [Executions] ([Uid], [Fail]) VALUES(@uid, 0) ",
			new { uid = _guid });


		return !_connection
			.Query<bool>("SELECT [Fail] FROM [Executions] (NOLOCK) WHERE [Uid] = @uid", new { uid = _guid })
			.Single();
	}


	private static Guid _guid = Guid.NewGuid();
	private static SqlConnection _connection = new ("Data Source=.;Initial Catalog=Test_DB;Integrated Security=true;");
}