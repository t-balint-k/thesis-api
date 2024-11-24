namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult CreatePortfolio(int? user_fk, string? name, double? pool, string? currency)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(name) || user_fk == null || pool == null || string.IsNullOrEmpty(currency)) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select id from portfolios where user_fk = {user_fk} and lower(name) = '{name.ToLower()}'").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : portfolio exists
            if (result != "") return SendResponse.AlreadyExists($"portfolio '{name}' under user {user_fk}");

            // Query
            (result, success) = DBHelper.DatabaseExecute($"insert into portfolios (user_fk, name, pool, currency) values ({user_fk}, '{name}', {pool}, '{currency}')").Result;

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok();
        }
    }
}