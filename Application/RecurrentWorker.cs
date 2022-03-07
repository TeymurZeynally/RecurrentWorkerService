using System.Data.SqlClient;
using Dapper;
using RecurrentWorkerService.Workers;

namespace Application;

internal class RecurrentWorker : IRecurrentWorker
{
	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		var sqlConnection = new SqlConnection("Data Source=.;Initial Catalog=Test_DB;Integrated Security=true;");
		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker Start");

		if (!_inserted)
		{
			await sqlConnection.ExecuteAsync("INSERT INTO [Executions] ([Uid], [Fail]) VALUES(@uid, 0)", new { uid = _guid });
			_inserted = true;
		}

		var fail = sqlConnection.Query<bool>("SELECT [Fail] FROM [Executions] (NOLOCK) WHERE [Uid] = @uid", new { uid = _guid }).Single();
		if (fail)
		{
			throw new Exception("FAIL");
		}


		Console.WriteLine($"{DateTimeOffset.UtcNow} RecurrentWorker End");
	}

	private static Guid _guid = Guid.NewGuid();
	private static bool _inserted = false;
}