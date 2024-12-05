using Newtonsoft.Json;
using Npgsql;

namespace ThesisAPI
{
    public static class DBHelper
    {
        // API masterkey for meta data and testing
        public static string Masterkey;

        // ConnectionString
        private static string ConnectionString;

        // Database connection entity
        private static NpgsqlDataSource Context;

        public static void init()
        {
            // envirenmental variables
            Masterkey = Environment.GetEnvironmentVariable("MASTERKEY");
            string host = Environment.GetEnvironmentVariable("DB_HOST");
            string user = Environment.GetEnvironmentVariable("DB_USER");
            string pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            string name = Environment.GetEnvironmentVariable("DB_NAME");

            // connection
            ConnectionString = $"Host={host};Username={user};Password={pass};Database={name};Include Error Detail=true;Timeout=30;CommandTimeout=30";

            // context
            Context = NpgsqlDataSource.Create(ConnectionString);
        }

        // Query
        public static async Task<(bool, string, int)> DatabaseQuery(string table, string fields, string whereClause = "")
        {
            // result
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            try
            {
                // connection
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    string query = $"SELECT {fields} from {table} {whereClause}";

                    // command
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        // select
                        using (var reader = cmd.ExecuteReader())
                        {
                            // read
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                result.Add(row);
                            }
                        }
                    }
                }
            }

            // error
            catch (Exception ex)
            {
                return (false, ex.Message, 0);
            }


            // done
            return (true, "{\"data\":" + JsonConvert.SerializeObject(result) + "}", result.Count);
        }

        // Command
        public static async Task<(bool, string)> DatabaseExecute(string command)
        {
            try
            {
                int result = 00;

                await using (NpgsqlCommand cmd = Context.CreateCommand(command))
                {
                    result = await cmd.ExecuteNonQueryAsync();
                }

                return (true, $"{result} rows affected.");
            }

            catch (Exception ex)
            {
                return (false, $"db execution error: {ex.Message}");
            }
        }

        // binary copy : securities
        public static async Task<(bool, string)> DatabaseBulkImport(List<Instrument> data, string endpoint)
        {
            try
            {
                await using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                {
                    conn.Open();
                    Console.WriteLine("  - importing bulk data");

                    // bulk import
                    await using (var writer = conn.BeginBinaryImport("COPY instrument_input (instrument_type, symbol, exchange, currency, currency_base, currency_quote, name, country, type) FROM STDIN (FORMAT BINARY)"))
                    {
                        foreach (Instrument n in data.Distinct())
                        {
                            writer.StartRow();
                            writer.Write(endpoint);
                            writer.Write(n.symbol);
                            writer.Write(n.exchange             == null ? ""   : n.exchange);
                            writer.Write(n.currency);
                            writer.Write(n.currency_base);
                            writer.Write(n.currency_quote); 
                            writer.Write(n.name                 == null ? null : n.name.Replace("'", "''").Replace("\"", ""));
                            writer.Write(n.country              == null ? ""   : n.country.Replace("'", "''"));
                            writer.Write(n.type);
                        }

                        string result = writer.Complete().ToString() + " rows affected.";

                        return (true, result);
                    }
                }
            }

            catch (Exception ex)
            {
                return (false, $"binary import error: {ex.Message}");
            }
        }
    }
}