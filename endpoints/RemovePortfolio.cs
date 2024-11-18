namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult RemovePortfolio(int? portfolio_id)
        {
            // Error : no inputs
            if (portfolio_id == null) return Results.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select id from portfolios where id = {portfolio_id}").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : portfolio does not exist
            if (result == "") return SendResponse.NotFound($"portforio {portfolio_id}");

            // Query
            (result, success) = DBHelper.DatabaseExecute($"delete from portfolios where id = {portfolio_id}").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok();
        }
    }
}