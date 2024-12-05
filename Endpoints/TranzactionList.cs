namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> TranzactionList(int? portfolio_fk)
        {
            // Error : no inputs
            if (portfolio_fk == null) return SendResponse.BadRequest();

            // Query
            string whereClause = $"where portfolio_fk = {portfolio_fk}";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("tranzaction", "portfolio_fk,instrument_fk,creation_time,amount,price", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok(result);
        }
    }
}