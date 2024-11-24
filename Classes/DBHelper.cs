using Npgsql;

namespace thesis_api
{
    public static class DBHelper
    {
        // API masterkey for meta data and testing
        public static string masterkey;

        // connection string
        static string connection_string;

        // Database context
        private static NpgsqlDataSource DB;
        private static NpgsqlDataSource db_context
        {
            get
            {
                if (DB == null)
                {
                    // envirenmental variables
                    masterkey = Environment.GetEnvironmentVariable("MASTERKEY");
                    string host = Environment.GetEnvironmentVariable("DB_HOST");
                    string user = Environment.GetEnvironmentVariable("DB_USER");
                    string pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
                    string name = Environment.GetEnvironmentVariable("DB_NAME");

                    // connection string
                    connection_string = $"Host={host};Username={user};Password={pass};Database={name};Include Error Detail=true;Timeout=300;CommandTimeout=300";

                    // context
                    DB = NpgsqlDataSource.Create(connection_string);
                }
                return DB;
            }
        }

        // query
        public static async Task<(string, bool)> DatabaseQuery(string command)
        {
            try
            {
                string result = "";

                await using (NpgsqlCommand cmd = db_context.CreateCommand(command))
                await using (NpgsqlDataReader data = await cmd.ExecuteReaderAsync())
                {
                    while (await data.ReadAsync())
                    {
                        string row = "";
                        for (int i = 0; i < data.FieldCount; i++) row = $"{row}{data.GetValue(i)};";
                        result = $"{result}{row}\n";
                    }
                }

                return (result, true);
            }

            catch (Exception ex)
            {
                return ($"db query: {ex.Message}", false);
            }
        }

        // command
        public static async Task<(string, bool)> DatabaseExecute(string command)
        {
            try
            {
                int result = 00;

                await using (NpgsqlCommand cmd = db_context.CreateCommand(command))
                {
                    result = await cmd.ExecuteNonQueryAsync();
                }

                return ($"{result} rows affected.", true);
            }

            catch (Exception ex)
            {
                return ($"db execution: {ex.Message}", false);
            }
        }

        // binary copy : securities
        public static async Task<(string, bool)> DatabaseBulkImport(List<Security> data, string endpoint)
        {
            try
            {
                await using (NpgsqlConnection conn = new NpgsqlConnection(connection_string))
                {
                    conn.Open();
                    Console.WriteLine("  - importing bulk data");

                    // bulk import
                    await using (var writer = conn.BeginBinaryImport("COPY securities_input (security_type, symbol, exchange, currency, currency_base, currency_quote, name, country, type) FROM STDIN (FORMAT BINARY)"))
                    {
                        foreach (Security n in data.Distinct())
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

                        return (result, true);
                    }
                }
            }

            catch (Exception ex)
            {
                return ($"binary import error: {ex.Message}", false);
            }
        }
    }
}