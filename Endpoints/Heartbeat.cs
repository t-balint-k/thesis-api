﻿namespace thesis_api
{
    public static partial class Endpoint
    {
        public static IResult Heartbeat()
        {
            return Results.Ok();
        }
    }
}