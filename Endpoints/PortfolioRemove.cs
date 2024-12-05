namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> PortfolioRemove(int? portfolio_id)
        {
            // Error : no inputs
            if (portfolio_id == null) return Results.BadRequest();

            // Query
            string whereClause = $"where id = {portfolio_id}";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("portfolio", "id", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : portfolio does not exist
            if (records == 0) return SendResponse.NotFound($"portforio {portfolio_id}");

            // Query
            (success, result) = await DBHelper.DatabaseExecute($"delete from portfolio where id = {portfolio_id}");

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok("{}");
        }
    }
}