namespace thesis_api
{
    internal class Program
    {
        private static Timer CheckMidnight;

        static void Main()
        {
            // Auto update at midnight
            CheckMidnight = new Timer(DailyUpdate, null, 0, 1000);

            // Web application
            WebApplicationBuilder wb = WebApplication.CreateBuilder();
            WebApplication wa = wb.Build();

            // Endpoints
            wa.MapGet("/v1/heartbeat", () => { return Endpoint.Heartbeat(); });
            wa.MapGet("/v1/login", (string? email, string? password) => { return Endpoint.Login(email, password); });
            wa.MapGet("/v1/signup", (string? email, string? password) => { return Endpoint.Signup(email, password); });

            wa.MapGet("/v1/get_data", (string? datatype) => { return Endpoint.GetData(datatype); }); // for later

            wa.MapGet("/v1/list_portfolios", (int? user) => { return Endpoint.ListPortfolios(user); });
            wa.MapGet("/v1/list_tranzactions", (int? portfolio) => { return Endpoint.ListTranzactions(portfolio); });

            wa.MapGet("/v1/create_portfolio", (int? user, string? name, double? pool, string? currency) => { return Endpoint.CreatePortfolio(user, name, pool, currency); });
            wa.MapGet("/v1/remove_portfolio", (int? portfolio) => { return Endpoint.RemovePortfolio(portfolio); });
            wa.MapGet("/v1/do_tranzaction", (int? portfolio, int? security, int? amount, int? price) => { return Endpoint.DoTranzaction(portfolio, security, amount, price); });

            // Admin
            wa.MapGet("/v1/force_update", (string? target) => { return Endpoint.ForceUpdate(target); });

            // Start
            wa.Run("http://0.0.0.0:5000");

            // Daily datasets update
            async static void DailyUpdate(object? state)
            {
                // Fire at midnight
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
                {
                    (bool success, string result) = await UpdateValueSets.ExecuteUpdate("all");

                    // Result
                    if (!success) Console.WriteLine($"Automatic update failed ({DateTime.Now}):\n\n{result}");
                    else Console.WriteLine($"Automtaic update successful ({DateTime.Now})");
                }    
            }
        }
    }
}
