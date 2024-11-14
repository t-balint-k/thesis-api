using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Sockets;
using System.Web;
using static System.Net.WebRequestMethods;

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
            wa.MapGet("/v1/update_securities",                     () => { return update_securities().Result; });
            wa.MapGet("/v1/update_meta",                           () => { return update_meta().Result; });

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

            // database update : securities
            async Task<IResult> update_securities()
            {
                // truncate destination table
                Console.WriteLine("Truncating destination table!");
                (string result, bool success) = await utility.execute("truncate table securities_input");
                if (!success) return Results.NotFound(result);

                // security types
                foreach (string n in new string[] { $"stocks?country=USA", "forex_pairs", "cryptocurrencies", "commodities" })
                {
                    (success, List<security> securities) = await call<security>($"{n}");
                    if (!success) return Results.NotFound("Külső szolgáltató hiba!");

                    // bulk upload input data
                    (result, success) = await utility.import(securities, n.Replace("?country=USA", ""));
                    if (!success) return Results.NotFound(result);
                }

                // done
                return Results.Ok("DONE");
            }

            // database update : meta
            async Task<IResult> update_meta()
            {
                // countries
                Console.WriteLine("Truncating countries table!");
                (string result, bool success) = await utility.execute("truncate table countries");
                if (!success) return Results.NotFound(result);

                (success, List<country> countries) = await call<country>($"countries?apikey={mock_key}");
                if (!success) return Results.NotFound("Külső szolgáltató hiba!");

                Console.WriteLine("Uploading to database...");
                try
                {
                    foreach (country n in countries)
                    {
                        (result, success) = await utility.execute($"insert into countries (name, iso3) values ('{n.name.Replace("'", "''")}', '{n.iso3.Replace("'", "''")}')");
                        if (!success) return Results.NotFound(result);
                    }
                }
                catch { return Results.NotFound("Ismeretlen hiba!"); }

                // countries
                Console.WriteLine("Truncating exchanges table!");
                (result, success) = await utility.execute("truncate table exchanges");
                if (!success) return Results.NotFound(result);

                (success, List<exchange> exchanges) = await call<exchange>($"exchanges?apikey={mock_key}");
                if (!success) return Results.NotFound("Külső szolgáltató hiba!");

                Console.WriteLine("Uploading to database...");
                try
                {
                    foreach (exchange n in exchanges)
                    {
                        (result, success) = await utility.execute($"insert into exchanges (name, country) values ('{n.name.Replace("'", "''")}', '{n.country.Replace("'", "''")}')");
                        if (!success) return Results.NotFound(result);
                    }
                }
                catch { return Results.NotFound("Ismeretlen hiba!"); }

                return Results.Ok("OK");
            }

            // third party API call
            async Task<(bool, List<T>)> call<T>(string endpoint)
            {
                // get
                HttpClient http = new HttpClient();
                Console.WriteLine($"Retrieving {endpoint} data ({DateTime.Now})");

                string response = "";
                try
                {
                    response = await http.GetStringAsync($"https://api.twelvedata.com/{endpoint}");

                    Console.WriteLine("   Deserializing results...");
                    List<T> elements = JsonConvert.DeserializeObject<packet<T>>(response).data;

                    return (true, elements);
                }

                catch
                {
                    // error calling third party API
                    return (false, new List<T>());
                }
            }
        }
    }
}