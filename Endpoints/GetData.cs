namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult GetData(string? datatype)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(datatype)) return SendResponse.BadRequest();

            // Building the query string
            string[] fields = [];
            string table = "";

            switch (datatype)
            {
                case "Security"  :
                    fields = ["id", "security_type", "symbol", "name", "exchange", "currency", "country", "type", "currency_base", "currency_quote"];
                    table = "securities";
                    break;
                case "Portfolio"  :
                    fields = ["id", "user_fk", "creation_time", "name", "pool", "currency"];
                    table = "portfolios";
                    break;
                case "Tranzaction":
                    fields = ["id", "portfolio_fk", "security_fk", "creation_time", "amount", "price"];
                    table = "tranzactions";
                    break;
                case "Country":
                    fields = ["id", "name", "iso3", "currency"];
                    table = "countries";
                    break;
                case "Exchange":
                    fields = ["id", "name", "country"];
                    table = "exchanges";
                    break;
            }

            // Query : pool size
            string query = $"select {string.Join(',', fields)} from {table}";
            Console.WriteLine(query);
            (string result, bool success) = DBHelper.DatabaseQuery(query).Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Empty set
            if (result.Trim('\n') == "") return Results.Ok("{\"data\": []}");

            // Parsing
            string[] records = result.Split('\n').Where(x => x != "").ToArray();

            string json = "{\"data\":[";
            foreach (string n in records)
            {
                string entity = "{";
                for (int i = 0; i < fields.Length; i++)
                {
                    string[] record = n.Split(';');
                    entity = $"{entity}\"{fields[i]}\":\"{record[i]}\",";
                }

                entity = entity.Substring(0, entity.Length - 1) + "}";

                json = $"{json}{entity},";
            }

            json = json.Substring(0, json.Length - 1) + "]}";

            // Ok
            return Results.Text(json, "application/json");
        }
    }
}