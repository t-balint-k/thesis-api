using System.Web;

namespace ThesisAPI
{
    public static partial class Endpoint
    {
        public static async Task<IResult> Login(string? email, string? password)
        {
            // error: no inputs
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return SendResponse.BadRequest();

            //query
            string whereClause = $"where email = '{HttpUtility.UrlDecode(email)}' and password = '{password}'";
            (bool success, string result, int records) = await DBHelper.DatabaseQuery("registered_user", "id,api_key", whereClause);

            // error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // error: non unique record -> internal server error
            if (records > 1) return SendResponse.ServerError("query returned multiple records");

            // error: no match
            if (records == 0) return SendResponse.NotFound($"email-password combo '{email}' '{password}'");

            // OK: returning API key
            return SendResponse.Ok(result);
        }
    }
}
