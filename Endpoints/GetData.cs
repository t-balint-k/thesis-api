namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public async static Task<IResult> GetData(string? datatype)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(datatype)) return SendResponse.BadRequest();

            // Building the query string
            string fields = "";
            string table = "";

            switch (datatype)
            {
                case "Instrument"  :
                    fields = "id,valid_from,valid_to,instrument_type,symbol,name,exchange,currency,country,type,currency_base,currency_quote";
                    table = "instrument";
                    break;
                case "Portfolio"  :
                    fields = "id,user_fk,creation_time,name,pool,currency";
                    table = "portfolio";
                    break;
                case "Tranzaction":
                    fields = "id,portfolio_fk,instrument_fk,creation_time,amount,price";
                    table = "tranzaction";
                    break;
                case "Country":
                    fields = "id,name,iso3,currency";
                    table = "country";
                    break;
                case "Exchange":
                    fields = "id,name,country";
                    table = "exchange";
                    break;
                default:
                    return SendResponse.NotFound($"Table '{table} not found'");
            }

            // Query : pool size
            (bool success, string result, int records) = await DBHelper.DatabaseQuery(table, fields);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok(result);
        }
    }
}