using System.Web;

namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult Signup(string? email, string? password)
        {
            // Error : no inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return SendResponse.BadRequest();

            // Query
            (string result, bool success) = DBHelper.DatabaseQuery($"select id from registered_users where email = '{HttpUtility.UrlDecode(email)}'").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Error : user already registered
            if (result != "") return SendResponse.AlreadyExists(email);

            // Query
            (result, success) = DBHelper.DatabaseExecute($"insert into registered_users (email, password, api_key) values ('{HttpUtility.UrlDecode(email)}', '{password}', '{DBHelper.masterkey}')").Result;

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok();
        }
    }
}