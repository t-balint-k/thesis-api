namespace ThesisAPI
{
    internal class Program
    {
        private static Timer CheckMidnight;

        /* Entrypoint */

        static void Main()
        {
            // Auto update at midnight
            CheckMidnight = new Timer(DailyUpdate, null, 0, 1000);

            // Database init
            DBHelper.init();

            // Webserver
            StartWebServer();
        }

        /* Webserver */

        private static void StartWebServer()
        {
            /* Web application */

            WebApplicationBuilder wb = WebApplication.CreateBuilder();
            WebApplication wa = wb.Build();

            /* Endpoints */

            wa.MapGet("/v1/Heartbeat", () => { return Endpoint.Heartbeat(); });
            wa.MapGet("/v1/Login", (string? email, string? password) => { return Endpoint.Login(email, password); });
            wa.MapGet("/v1/Signup", (string? email, string? password) => { return Endpoint.Signup(email, password); });

            wa.MapGet("/v1/PortfolioCreate", (int? user, string? name, double? pool, string? currency) => { return Endpoint.PortfolioCreate(user, name, pool, currency); });
            wa.MapGet("/v1/PortfolioList", (int? user) => { return Endpoint.PortfolioList(user); });
            wa.MapGet("/v1/PortfolioRemove", (int? portfolio) => { return Endpoint.PortfolioRemove(portfolio); });

            wa.MapGet("/v1/TranzactionMake", (int? portfolio, int? instrument, double? amount, double? price) => { return Endpoint.TranzactionMake(portfolio, instrument, amount, price); });
            wa.MapGet("/v1/TranzactionList", (int? portfolio) => { return Endpoint.TranzactionList(portfolio); });

            wa.MapGet("/v1/GetData", (string? datatype) => { return Endpoint.GetData(datatype); });
            wa.MapGet("/v1/ForceUpdate", (string? target) => { return Endpoint.ForceUpdate(target); });

            /* Execute */

            wa.Run("http://0.0.0.0:5000");
        }

        /* Daily datasets update */

        private async static void DailyUpdate(object? state)
        {
            // fire at midnight
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
            {
                (bool success, string result) = await UpdateValueSets.ExecuteUpdate("all");

                // result
                if (!success) Console.WriteLine($"Automatic update failed ({DateTime.Now}):\n\n{result}");
                else Console.WriteLine($"Automtaic update successful ({DateTime.Now})");
            }
        }
    }
}
