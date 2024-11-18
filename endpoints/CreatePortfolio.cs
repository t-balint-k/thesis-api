namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult CreatePortfolio(int? user_fk, string? name, int? pool)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(name) || user_fk == null || pool == null) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select id from portfolios where user_fk = {user_fk} and lower(name) = '{name.ToLower()}'").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : portfolio exists
            if (result != "") return SendResponse.AlreadyExists($"portfolio '{name}' under user {user_fk}");

            // Query
            (result, success) = DBHelper.DatabaseExecute($"insert into portfolios (user_fk, name, pool) values ({user_fk}, '{name}', {pool})").Result;

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok();
        }
    }
}