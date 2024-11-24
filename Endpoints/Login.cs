using System.Web;

namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult Login(string? email, string? password)
        {
            // error: no inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return SendResponse.BadRequest();

            //query
            (string result, bool success) = DBHelper.DatabaseQuery($"select id, api_key from registered_users where email = '{HttpUtility.UrlDecode(email)}' and password = '{password}'").Result;

            // error: internal server error
            if (!success) return SendResponse.ServerError(result);

            string[] rows = result
                .Split('\n')
                .Where(x => x != "")
                .ToArray();

            // error: non unique record -> internal server error
            if (rows.Length > 1) return SendResponse.ServerError("query returned multiple records");

            // error: no match
            if (rows.Length == 0) return SendResponse.NotFound($"email-password combo '{email}' '{password}'");

            // OK: returning API key
            string response = rows[0];
            return Results.Ok(response);
        }
    }
}
