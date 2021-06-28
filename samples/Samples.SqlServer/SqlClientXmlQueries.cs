using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SignalFx.Tracing;

namespace Samples.SqlServer
{
    public class SqlClientXmlQueries
    {
        private const string DropAndCreateCommandText =
            "DROP TABLE IF EXISTS Cities; CREATE TABLE Cities (Id int PRIMARY KEY, Name varchar(100));";
        private const string InsertCommandText =
            "INSERT INTO Cities (Id, Name) VALUES (0, 'Seattle');INSERT INTO Cities (Id, Name) VALUES (1, 'Renton');";
        private const string SelectXmlCommandText =
            "SELECT * FROM Cities FOR XML AUTO, XMLDATA;";

        private readonly IDbConnection _connection;

        public SqlClientXmlQueries(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task RunAsync()
        {
            using (var scopeAll = Tracer.Instance.StartActive("sql.client.xml"))
            {
                scopeAll.Span.SetTag("command-type", typeof(SqlCommand).FullName);

                _connection.Open();
                SetupTableForXmlQueries(_connection);
                SelectXml(_connection);
                await SelectXmlAsync(_connection).ConfigureAwait(false);
                _connection.Close();
            }
        }

        private static void SetupTableForXmlQueries(IDbConnection connection)
        {
            using (var command = (SqlCommand)connection.CreateCommand())
            {
                command.CommandText = DropAndCreateCommandText;

                int records = command.ExecuteNonQuery();
                Console.WriteLine($"Dropped and recreated table for XML queries. {records} record(s) affected.");

                command.CommandText = InsertCommandText;
                records = command.ExecuteNonQuery();
                Console.WriteLine($"Inserted {records} record(s).");
            }
        }

        private static void SelectXml(IDbConnection connection)
        {
            using (var command = (SqlCommand)connection.CreateCommand())
            {
                command.CommandText = SelectXmlCommandText;
                var xmlReader = command.ExecuteXmlReader();
                Console.WriteLine("Selected XML data synchronously: " + XmlReaderToString(xmlReader));
            }
        }

        private static async Task SelectXmlAsync(IDbConnection connection)
        {
            using (var command = (SqlCommand)connection.CreateCommand())
            {
                command.CommandText = SelectXmlCommandText;
                var xmlReader = await command.ExecuteXmlReaderAsync().ConfigureAwait(false);
                Console.WriteLine("Selected XML data asynchronously: " + XmlReaderToString(xmlReader));
            }
        }

        private static string XmlReaderToString(XmlReader xmlReader)
        {
            var sb = new StringBuilder();
            while (xmlReader.Read())
            {
                sb.Append(xmlReader.ReadOuterXml());
            }

            return sb.ToString();
        }
    }
}
