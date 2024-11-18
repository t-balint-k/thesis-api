namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult ListTranzactions(int? portfolio_fk, int? security_fk)
        {
            // Error : no inputs
            if (portfolio_fk == null || security_fk == null) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select * from tranzactions where portfolio_fk = {portfolio_fk} and security_fk = {security_fk} order by creation_time desc").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok(result);
        }
    }
}