namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult ListTranzactions(int? portfolio_fk)
        {
            // Error : no inputs
            if (portfolio_fk == null) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select security_fk, creation_time, amount, price from tranzactions where portfolio_fk = {portfolio_fk}").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok(result);
        }
    }
}