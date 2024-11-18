namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult DoTranzaction(int? portfolio_fk, int? securitiy_fk, double? amount, double? price)
        {
            // Error : no inputs
            if (portfolio_fk == null || securitiy_fk == null || amount == null || price == null) return SendResponse.BadRequest();

            // Query : pool size
            (string result, bool success) = DBHelper.DatabaseQuery($"select pool from portfolios where id = {portfolio_fk}").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Parsing result
            string[] pools = result.Split('\n');
            if (pools.Length != 1) return SendResponse.ServerError($"non unique ({pools.Length}) portfolio {portfolio_fk}");
            double pool = double.Parse(result.Trim(';'));

            // Query : tranzaction history
            (result, success) = DBHelper.DatabaseQuery($"select sum(amount*price) from tranzactions where portfolio_fk = {portfolio_fk}").Result;

            // Error : internal server error
            if (!success) return SendResponse.ServerError(result);

            // Parsing results
            result = result.Trim(';');
            double exp = result == "" ? 0 : double.Parse(result);

            // Checking pool depletion
            double available = pool - exp;
            double requsest = (double)amount * (double)price;

            // Error : portfolio pool depleted
            if (available < requsest) return SendResponse.Denied($"insufficient funds in portfolio pool ({available} < {requsest})");

            // Query : inserting new tranzaction record
            (result, success) = DBHelper.DatabaseExecute($"insert into tranzactions (portfolio_fk, security_fk, amount, price) values ({portfolio_fk}, {securitiy_fk}, {amount}, {price})").Result;

            // Error: internal server error
            if (!success) return SendResponse.ServerError(result);

            // Ok
            return Results.Ok();
        }
    }
}