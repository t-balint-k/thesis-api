using Newtonsoft.Json.Linq;

namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> TranzactionMake(int? portfolio_fk, int? instrument_fk, double? amount, double? price, double? rate)
        {
            // Error : no inputs
            if (portfolio_fk == null || instrument_fk == null || amount == null || price == null || rate == null) return SendResponse.BadRequest();

            // Query : pool size
            string whereClause = $"where id = {portfolio_fk}";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("portfolio", "pool", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);
            
            // Error : non unique portfolio
            if (records != 1) return SendResponse.ServerError($"non unique ({records}) portfolio {portfolio_fk}");

            // Parsing results
            JObject json = JObject.Parse(result)["data"].ToObject<JObject[]>()[0];
            double pool = double.Parse((string)json["pool"]);

            // Query : tranzaction history
            whereClause = $"where portfolio_fk = {portfolio_fk}";
            (success, result, records) = await DBHelper.DatabaseQuery("tranzaction", "sum(amount*price*rate) as product", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Parsing results
            json = JObject.Parse(result)["data"].ToObject<JObject[]>()[0];

            string? product = (string)json["product"];
            double exp = records == 0 || string.IsNullOrEmpty(product) ? 0 : double.Parse(product);

            // Checking pool depletion
            double available = pool - exp;
            double requsest = (double)amount * (double)price * (double)rate;

            // Error : portfolio pool depleted
            if (available < requsest) return SendResponse.Denied($"insufficient funds in portfolio pool ({available} < {requsest})");

            // Query : inserting new tranzaction record
            (success, result) = DBHelper.DatabaseExecute($"insert into tranzaction (portfolio_fk, instrument_fk, amount, price, rate) values ({portfolio_fk}, {instrument_fk}, {amount}, {price}, {rate})").Result;

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok("{}");
        }
    }
}