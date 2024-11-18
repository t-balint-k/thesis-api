using Newtonsoft.Json;

namespace thesis_api
{
    public static partial class Endpoint
    {
        // Fored update (manually by admin)
        public static async Task<IResult> ForceUpdate(string? target)
        {
            if (string.IsNullOrEmpty(target)) return SendResponse.BadRequest();

            // Execute update
            (bool success, string result) = await UpdateValueSets.ExecuteUpdate(target);

            // Error
            if (!success) return SendResponse.ThirdPartyError(result);

            // Ok
            return Results.Ok();
        }
    }

    public static class UpdateValueSets
    {
        // Main method : routes requests
        public static async Task<(bool, string)> ExecuteUpdate(string target)
        {
            List<UpdateResult> results = new List<UpdateResult>();

            // scopes
            switch (target)
            {
                case "data_only":
                    results.Add(await UpdateSecuritiesInputData());
                    break;
                case "procedure_only":
                    results.Add(await UpdateResultTable());
                    break;
                case "securities":
                    results.Add(await UpdateSecuritiesInputData());
                    results.Add(await UpdateResultTable());
                    break;
                case "meta":
                    results.Add(await UpdateMeta());
                    break;
                case "all":
                    results.Add(await UpdateMeta());
                    results.Add(await UpdateSecuritiesInputData());
                    results.Add(await UpdateResultTable());
                    break;
                default:
                    results = new List<UpdateResult>() { new UpdateResult(false, $"Unknown update target: {target}") };
                    break;
            }

            // response
            bool success = true;
            string response = "";

            // construct response
            foreach (UpdateResult n in results)
            {
                if (!n.success)
                {
                    success = false;
                    response = $"{response}{n.message}\n\n";
                }
            }

            if (!success) return (false, response);

            // done
            return (true, "");
        }

        // Database update : meta
        static async Task<UpdateResult> UpdateMeta()
        {
            // metadata for in-app filters
            UpdateResult c = await GetMeta<Country>("countries");
            UpdateResult e = await GetMeta<Exchange>("exchanges");

            // done
            if (c.success && e.success) return new UpdateResult(true, "");
            return new UpdateResult(false, string.Join("\n\n", [c.message, e.message]));

            // funcion
            static async Task<UpdateResult> GetMeta<T>(string target)
            {
                // truncate
                (string result, bool success) = await DBHelper.DatabaseExecute($"truncate table {target}");
                if (!success) return new UpdateResult(false, result);

                // acqury
                (success, List<T> elements) = await MakeCall<T>($"{target}?apikey={DBHelper.masterkey}");
                if (!success) return new UpdateResult(false, "Külső szolgáltató hiba!");

                // insert commands
                string[] inserts = [];
                if (target == "countries") inserts = elements.Cast<Country>().Select(x => $"insert into countries (name, iso3) values ('{x.name.Replace("'", "''")}', '{x.iso3.Replace("'", "''")}')").ToArray();
                if (target == "exchanges") inserts = elements.Cast<Exchange>().Select(x => $"insert into exchanges (name, country) values ('{x.name.Replace("'", "''")}', '{x.country.Replace("'", "''")}')").ToArray();

                // upload
                foreach (string n in inserts)
                {
                    (result, success) = await DBHelper.DatabaseExecute(n);
                    if (!success) return new UpdateResult(false, result);
                }

                // done
                return new UpdateResult(true, "");
            }
        }

        // Database update : acquireing securities data from third party endpoint
        static async Task<UpdateResult> UpdateSecuritiesInputData()
        {
            // truncate destination table
            (string result1, bool success1) = await DBHelper.DatabaseExecute("truncate table securities_input");
            if (!success1) return new UpdateResult(false, result1);

            // security types
            foreach (string n in new string[] { "stocks", "forex_pairs", "cryptocurrencies", "indices", "commodities" /*", funds", "bonds", "etfs"*/ })
            {
                // third party API call
                (bool success2, List<Security> securities) = await MakeCall<Security>($"{n}");
                if (!success2) return new UpdateResult(false, "Külső szolgáltató hiba!");

                // bulk upload input data
                (string result2, success2) = await DBHelper.DatabaseBulkImport(securities, n);
                if (!success2) return new UpdateResult(false, result2);

            }

            // done
            return new UpdateResult(true, "");
        }

        // Database update : executing stored procedure to consolidate fresh input
        static async Task<UpdateResult> UpdateResultTable()
        {
            // consolidating data
            (string result, bool success) = await DBHelper.DatabaseExecute("call consolidate_securities()");
            if (!success) return new UpdateResult(false, result);

            // done
            return new UpdateResult(true, "");
        }

        // Third party API call
        static async Task<(bool, List<T>)> MakeCall<T>(string endpoint)
        {
            // get
            HttpClient http = new HttpClient() { Timeout = new TimeSpan(0,0,30) };
            Console.WriteLine($"Retrieving {endpoint} data ({DateTime.Now})");

            string response = "";
            try
            {
                response = await http.GetStringAsync($"https://api.twelvedata.com/{endpoint}");

                Console.WriteLine("  - deserializing results");
                List<T> elements = JsonConvert.DeserializeObject<APIReturnPacket<T>>(response).data ?? new List<T>();

                // done
                if (endpoint != "cryptocurrencies") return (true, elements);

                // handling crypto's irregular structure
                Security[] s = elements.Cast<Security>().SelectMany(x => (x.available_exchanges ?? []).Select(y => new Security()
                {
                    symbol = x.symbol,
                    exchange = y,
                    currency_base = x.currency_base,
                    currency_quote = x.currency_quote
                })).ToArray();
                return (true, elements.Distinct().Cast<T>().ToList());
            }

            // Request timeout
            catch (TimeoutException ex)
            {
                Console.WriteLine($"API call timeout: {ex.Message}");
                return (false, new List<T>());
            }

            // error calling third party API
            catch (Exception ex)
            {
                Console.WriteLine($"API call exception: {ex.Message}");
                return (false, new List<T>());
            }
        }

        /*  Sata structures  */

        class UpdateResult
        {
            public bool success;
            public string message;

            public UpdateResult(bool _succes, string _message)
            {
                success = _succes;
                message = _message;
            }
        }

        class Country
        {
            public required string name;
            public required string iso3;
        }

        class Exchange
        {
            public required string name;
            public required string country;
        }

        class APIReturnPacket<T>
        {
            public List<T>? data;
        }
    }
}