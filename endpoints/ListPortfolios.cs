namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult ListPortfolios(int? user_fk)
        {
            // Error : no inputs
            if (user_fk == null) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select * from portfolios where user_fk = {user_fk} order by creation_time desc").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok(result);
        }
    }
}