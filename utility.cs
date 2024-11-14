using Npgsql;

namespace thesis_api
{
    public static class utility
    {
        // db context and connection string
        const string connection_string = "Host=10.2.0.202;Username=admin;Password=asd;Database=thesis;Include Error Detail=true";

        private static NpgsqlDataSource DB;
        private static NpgsqlDataSource db_context
        {
            get
            {
                if (DB == null) DB = NpgsqlDataSource.Create(connection_string);
                return DB;
            }
        }

        // query
        public static async Task<(string, bool)> query(string command)
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
                return (ex.Message, false);
            }
        }

        // command
        public static async Task<(string, bool)> execute(string command)
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
                return (ex.Message, false);
            }
        }

        // binary copy : securities
        public static async Task<(string, bool)> import(List<security> data, string endpoint)
        {
            try
            {
                await using (NpgsqlConnection conn = new NpgsqlConnection(connection_string))
                {
                    conn.Open();
                    await using (var writer = conn.BeginBinaryImport("COPY securities_input (security_type, symbol, exchange, currency, currency_base, currency_quote, name, country, type, exchange_list) FROM STDIN (FORMAT BINARY)"))
                    {
                        Console.WriteLine("   Importing bulk data!");
                        foreach (security n in data.Distinct())
                        {
                            writer.StartRow();
                            writer.Write(endpoint);
                            writer.Write(n.symbol);
                            writer.Write(n.exchange             == null ? null : n.exchange);
                            writer.Write(n.currency             == null ? null : n.currency);
                            writer.Write(n.currency_base        == null ? null : n.currency_base);
                            writer.Write(n.currency_quote       == null ? null : n.currency_quote);
                            writer.Write(n.name                 == null ? null : n.name.Replace("'", "''").Replace("\"", ""));
                            writer.Write(n.country              == null ? null : n.country.Replace("'", "''"));
                            writer.Write(n.type);
                            writer.Write(n.available_exchanges  == null ? null : string.Join(',', n.available_exchanges));
                        }

                        string result = writer.Complete().ToString() + " rows affected.";

                        return (result, true);
                    }
                }
            }

            catch (Exception ex)
            {
                return (ex.Message, false);
            }
        }
    }
}