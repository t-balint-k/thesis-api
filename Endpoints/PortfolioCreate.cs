namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> PortfolioCreate(int? user_fk, string? name, double? pool, string? currency)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(name) || user_fk == null || pool == null || string.IsNullOrEmpty(currency)) return SendResponse.BadRequest();

            // Query
            string whereClause = $"where user_fk = {user_fk} and lower(name) = '{name.ToLower()}'";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("portfolio", "id", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : portfolio exists
            if (records != 0) return SendResponse.AlreadyExists($"portfolio '{name}' under user {user_fk}");

            // Query
            (success, result) = await DBHelper.DatabaseExecute($"insert into portfolio (user_fk, name, pool, currency) values ({user_fk}, '{name}', {pool}, '{currency}')");

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok("{}");
        }
    }
}