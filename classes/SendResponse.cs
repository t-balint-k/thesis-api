namespace thesis_api
{
    public static class SendResponse
    {
        public static IResult ServerError(string message)
        {
            Console.WriteLine($"Internal server error: {message}");
            return Results.StatusCode(500);
        }

        public static IResult NotFound(string message)
        {
            Console.WriteLine($"Resource not found: {message}");
            return Results.StatusCode(404);
        }

        public static IResult AlreadyExists(string message)
        {
            Console.WriteLine($"Resource elready exists: {message}");
            return Results.StatusCode(409);
        }

        public static IResult BadRequest()
        {
            Console.WriteLine($"Input parameter(s) missing");
            return Results.StatusCode(400);
        }

        public static IResult Denied(string message)
        {
            Console.WriteLine($"Request denied: {message}");
            return Results.StatusCode(409);
        }

        public static IResult ThirdPartyError(string message)
        {
            Console.WriteLine($"Error reaching third party API: {message}");
            return Results.StatusCode(500);
        }
    }
}