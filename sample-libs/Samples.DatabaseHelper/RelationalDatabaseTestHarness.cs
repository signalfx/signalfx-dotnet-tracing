using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SignalFx.Tracing;

namespace Samples.DatabaseHelper
{
    public class RelationalDatabaseTestHarness<TConnection, TCommand, TDataReader>
        where TConnection : class, IDbConnection
        where TCommand : class, IDbCommand
        where TDataReader : class, IDataReader
    {
        private const string DropCommandText = "DROP TABLE IF EXISTS Employees; CREATE TABLE Employees (Id int PRIMARY KEY, Name varchar(100));";
        private const string InsertCommandText = "INSERT INTO Employees (Id, Name) VALUES (@Id, @Name);";
        private const string SelectOneCommandText = "SELECT Name FROM Employees WHERE Id=@Id;";
        private const string UpdateCommandText = "UPDATE Employees SET Name=@Name WHERE Id=@Id;";
        private const string SelectManyCommandText = "SELECT * FROM Employees WHERE Id=@Id;";
        private const string DeleteCommandText = "DELETE FROM Employees WHERE Id=@Id;";

        private readonly TConnection _connection;

        private readonly Func<TCommand, int> _executeNonQuery;
        private readonly Func<TCommand, object> _executeScalar;
        private readonly Func<TCommand, TDataReader> _executeReader;
        private readonly Func<TCommand, CommandBehavior, TDataReader> _executeReaderWithBehavior;

        private readonly Func<TCommand, Task<int>> _executeNonQueryAsync;
        private readonly Func<TCommand, Task<object>> _executeScalarAsync;
        private readonly Func<TCommand, Task<TDataReader>> _executeReaderAsync;
        private readonly Func<TCommand, CommandBehavior, Task<TDataReader>> _executeReaderWithBehaviorAsync;

        public RelationalDatabaseTestHarness(
            TConnection connection,
            Func<TCommand, int> executeNonQuery,
            Func<TCommand, object> executeScalar,
            Func<TCommand, TDataReader> executeReader,
            Func<TCommand, CommandBehavior, TDataReader> executeReaderWithBehavior,
            Func<TCommand, Task<int>> executeNonQueryAsync,
            Func<TCommand, Task<object>> executeScalarAsync,
            Func<TCommand, Task<TDataReader>> executeReaderAsync,
            Func<TCommand, CommandBehavior, Task<TDataReader>> executeReaderWithBehaviorAsync)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _executeNonQuery = executeNonQuery ?? throw new ArgumentNullException(nameof(executeNonQuery));
            _executeScalar = executeScalar ?? throw new ArgumentNullException(nameof(executeScalar));
            _executeReader = executeReader ?? throw new ArgumentNullException(nameof(executeReader));
            _executeReaderWithBehavior = executeReaderWithBehavior ?? throw new ArgumentNullException(nameof(executeReaderWithBehavior));

            // async methods are not implemented by all ADO.NET providers, so they can be null
            _executeNonQueryAsync = executeNonQueryAsync;
            _executeScalarAsync = executeScalarAsync;
            _executeReaderAsync = executeReaderAsync;
            _executeReaderWithBehaviorAsync = executeReaderWithBehaviorAsync;
        }

        public async Task RunAsync()
        {
            using (var scopeAll = Tracer.Instance.StartActive("run.all"))
            {
                scopeAll.Span.SetTag("command-type", typeof(TCommand).FullName);

                using (var scopeSync = Tracer.Instance.StartActive("run.sync"))
                {
                    scopeSync.Span.SetTag("command-type", typeof(TCommand).FullName);

                    _connection.Open();
                    CreateNewTable(_connection);
                    InsertRow(_connection);
                    SelectScalar(_connection);
                    UpdateRow(_connection);
                    SelectRecords(_connection);
                    DeleteRecord(_connection);
                    _connection.Close();
                }

                if (_connection is DbConnection connection)
                {
                    // leave a small space between spans, for better visibility in the UI
                    await Task.Delay(TimeSpan.FromSeconds(0.1));

                    using (var scopeAsync = Tracer.Instance.StartActive("run.async"))
                    {
                        scopeAsync.Span.SetTag("command-type", typeof(TCommand).FullName);

                        await connection.OpenAsync();
                        await CreateNewTableAsync(_connection);
                        await InsertRowAsync(_connection);
                        await SelectScalarAsync(_connection);
                        await UpdateRowAsync(_connection);
                        await SelectRecordsAsync(_connection);
                        await DeleteRecordAsync(_connection);
                        _connection.Close();
                    }
                }
            }
        }

        private void DeleteRecord(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = DeleteCommandText;
                command.AddParameterWithValue("Id", 1);

                int records = _executeNonQuery(command);
                Console.WriteLine($"Deleted {records} record(s).");
            }
        }

        private void SelectRecords(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = SelectManyCommandText;
                command.AddParameterWithValue("Id", 1);

                using (var reader = _executeReader(command))
                {
                    var employees = reader.AsDataRecords()
                                          .Select(
                                               r => new { Id = (int)r["Id"], Name = (string)r["Name"] })
                                          .ToList();

                    Console.WriteLine($"Selected {employees.Count} record(s).");
                }

                using (var reader = _executeReaderWithBehavior(command, CommandBehavior.Default))
                {
                    var employees = reader.AsDataRecords()
                                          .Select(
                                               r => new { Id = (int)r["Id"], Name = (string)r["Name"] })
                                          .ToList();

                    Console.WriteLine($"Selected {employees.Count} record(s) with `CommandBehavior.Default`.");
                }
            }
        }

        private void UpdateRow(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = UpdateCommandText;
                command.AddParameterWithValue("Name", "Name2");
                command.AddParameterWithValue("Id", 1);

                int records = _executeNonQuery(command);
                Console.WriteLine($"Updated {records} record(s).");
            }
        }

        private void SelectScalar(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = SelectOneCommandText;
                command.AddParameterWithValue("Id", 1);

                var name = _executeScalar(command) as string;
                Console.WriteLine($"Selected scalar `{name ?? "(null)"}`.");
            }
        }

        private void InsertRow(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = InsertCommandText;
                command.AddParameterWithValue("Id", 1);
                command.AddParameterWithValue("Name", "Name1");

                int records = _executeNonQuery(command);
                Console.WriteLine($"Inserted {records} record(s).");
            }
        }

        private void CreateNewTable(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = DropCommandText;

                int records = _executeNonQuery(command);
                Console.WriteLine($"Dropped and recreated table. {records} record(s) affected.");
            }
        }

        private async Task DeleteRecordAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = DeleteCommandText;
                command.AddParameterWithValue("Id", 1);

                if (_executeNonQueryAsync != null)
                {
                    int records = await _executeNonQueryAsync(command);
                    Console.WriteLine($"Deleted {records} record(s).");
                }
            }
        }

        private async Task SelectRecordsAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = SelectManyCommandText;
                command.AddParameterWithValue("Id", 1);

                if (_executeReaderAsync != null)
                {
                    using (var reader = await _executeReaderAsync(command))
                    {
                        var employees = reader.AsDataRecords()
                                              .Select(
                                                   r => new { Id = (int)r["Id"], Name = (string)r["Name"] })
                                              .ToList();

                        Console.WriteLine($"Selected {employees.Count} record(s).");
                    }
                }

                if (_executeReaderWithBehaviorAsync != null)
                {
                    using (var reader = await _executeReaderWithBehaviorAsync(command, CommandBehavior.Default))
                    {
                        var employees = reader.AsDataRecords()
                                              .Select(
                                                   r => new { Id = (int)r["Id"], Name = (string)r["Name"] })
                                              .ToList();

                        Console.WriteLine($"Selected {employees.Count} record(s) with `CommandBehavior.Default`.");
                    }
                }
            }
        }

        private async Task UpdateRowAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = UpdateCommandText;
                command.AddParameterWithValue("Name", "Name2");
                command.AddParameterWithValue("Id", 1);

                if (_executeNonQueryAsync != null)
                {
                    int records = await _executeNonQueryAsync(command);
                    Console.WriteLine($"Updated {records} record(s).");
                }
            }
        }

        private async Task SelectScalarAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = SelectOneCommandText;
                command.AddParameterWithValue("Id", 1);

                if (_executeScalarAsync != null)
                {
                    object nameObj = await _executeScalarAsync(command);
                    var name = nameObj as string;
                    Console.WriteLine($"Selected scalar `{name ?? "(null)"}`.");
                }
            }
        }

        private async Task InsertRowAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = InsertCommandText;
                command.AddParameterWithValue("Id", 1);
                command.AddParameterWithValue("Name", "Name1");

                if (_executeNonQueryAsync != null)
                {
                    int records = await _executeNonQueryAsync(command);
                    Console.WriteLine($"Inserted {records} record(s).");
                }
            }
        }

        private async Task CreateNewTableAsync(IDbConnection connection)
        {
            using (var command = (TCommand)connection.CreateCommand())
            {
                command.CommandText = DropCommandText;

                if (_executeNonQueryAsync != null)
                {
                    int records = await _executeNonQueryAsync(command);
                    Console.WriteLine($"Dropped and recreated table. {records} record(s) affected.");
                }
            }
        }
    }
}
