using System.Web;

namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> Signup(string? email, string? password)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return SendResponse.BadRequest();

            // Query
            string whereClause = $"where email = '{HttpUtility.UrlDecode(email)}'";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("registered_user", "id", whereClause);

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : user already registered
            if (records != 0) return SendResponse.AlreadyExists(email);

            // Query
            (success, result) = await DBHelper.DatabaseExecute($"insert into registered_user (email, password, api_key) values ('{HttpUtility.UrlDecode(email)}', '{password}', '{DBHelper.Masterkey}')");

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return SendResponse.Ok("{}");
        }
    }
}