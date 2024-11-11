using Newtonsoft.Json;
using Npgsql;
using System.Diagnostics;
using System.Web;

namespace thesis_api
{
    internal class Program
    {
        static string mock_key = "0dd18a256cb649e48b71f593c5dd963f";

        static void Main()
        {
            // web application
            WebApplicationBuilder wb = WebApplication.CreateBuilder();
            WebApplication wa = wb.Build();

            // endpoints
            wa.MapGet("/v1/login",    (string email, string password) => { return login(email, password);    });
            wa.MapGet("/v1/register", (string email, string password) => { return register(email, password); });
            wa.MapGet("/v1/update",   (string endpoint)               => { return update(endpoint); });

            // start
            wa.Run("http://0.0.0.0:5000");

            /* METHODS */

            // login
            IResult login(string email, string password)
            {
                // error: no inputs
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return Results.BadRequest();

                //query
                (string result, bool success) = utility.query($"select api_key from registered_users where email = '{HttpUtility.UrlDecode(email)}' and password = '{password}'").Result;

                // error: internal server error
                if (!success) return Results.StatusCode(500);

                string[] rows = result
                    .Split('\n')
                    .Where(x => x != "")
                    .ToArray();

                // error: non unique record -> internal server error
                if (rows.Length  > 1) return Results.StatusCode(500);

                // error: no match
                if (rows.Length == 0) return Results.NotFound();

                // OK: returning API key
                string response = rows[0].Trim(';');
                return Results.Ok(response);
            }

            // register
            IResult register(string email, string password)
            {
                // error: no inputs
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return Results.BadRequest();

                // query
                (string result, bool success) = utility.query($"select id from registered_users where email = '{HttpUtility.UrlDecode(email)}'").Result;

                // error: internal server error
                if (!success) return Results.StatusCode(500);

                // error: user already registered
                if (result != "") return Results.NotFound();

                // query
                (result, success) = utility.execute($"insert into registered_users (email, password, api_key) values ('{HttpUtility.UrlDecode(email)}', '{password}', '{mock_key}')").Result;

                // error: internal server error
                if (!success) return Results.StatusCode(500);

                // OK
                return Results.Ok();
            }

            // database update
            IResult update(string endpoint)
            {
                // truncate destination table
                Console.WriteLine("Truncating destination table!");
                (string result1, bool success1) = utility.execute("truncate table securities_input").Result;
                if (!success1) return Results.NotFound(result1);

                // get
                List<security> elements = get_response().Result;

                // upload input data
                (string result2, bool success2) = utility.import(elements, endpoint).Result;
                if (!success2) return Results.NotFound(result2);

                // temp
                Debug.Print("DONE!");
                return Results.Ok(result2);

                // consolidate

                async Task<List<security>> get_response()
                {
                    HttpClient http = new HttpClient();

                    Console.WriteLine("Retrieving online data!");
                    string response = await http.GetStringAsync($"https://api.twelvedata.com/{endpoint}");

                    Console.WriteLine("Deserializing json data!");
                    return JsonConvert.DeserializeObject<packet>(response).data;
                }
            }
        }

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

            // binary copy
            public static async Task<(string, bool)> import(List<security> data, string endpoint)
            {
                try
                {
                    await using (NpgsqlConnection conn = new NpgsqlConnection(connection_string))
                    {
                        conn.Open();
                        await using (var writer = conn.BeginBinaryImport("COPY securities_input (security_type, symbol, exchange, currency, currency_base, currency_quote, name, country, type) FROM STDIN (FORMAT BINARY)"))
                        {
                            Console.WriteLine("Importing bulk data!");
                            foreach (security n in data.Distinct())
                            {
                                writer.StartRow();
                                writer.Write(endpoint);
                                writer.Write(n.symbol);
                                writer.Write(n.exchange == null ? "" : n.exchange);
                                writer.Write(n.currency == null ? "" : n.currency);
                                writer.Write(n.currency_base == null ? "" : n.currency_base);
                                writer.Write(n.currency_quote == null ? "" : n.currency_quote);
                                writer.Write(n.name);
                                writer.Write(n.country);
                                writer.Write(n.type);
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
}
