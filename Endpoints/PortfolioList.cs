namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> PortfolioList(int? user_fk)
        {
            // Error : no inputs
            if (user_fk == null) return SendResponse.BadRequest();

            // Query
            string whereClause = $"where user_fk = {user_fk} order by creation_time desc";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("portfolio", "id,creation_time,name,pool,currency", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok(result);
        }
    }
}