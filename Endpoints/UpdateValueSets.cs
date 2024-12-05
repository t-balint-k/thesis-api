using Newtonsoft.Json;

namespace ThesisAPI
{
    public static partial class Endpoint
    {
        // Fored update (manually by admin)
        public static async Task<IResult> ForceUpdate(string? target)
        {
            if (string.IsNullOrEmpty(target))
            {
                Console.WriteLine("Target omitted, assuming full database update!");
                target = "all";
            }

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
            UpdateResult c = await GetMeta<Country>("country", "countries");
            UpdateResult e = await GetMeta<Exchange>("exchange", "exchanges");

            // done
            if (c.success && e.success) return new UpdateResult(true, "");
            return new UpdateResult(false, string.Join("\n\n", [c.message, e.message]));

            // funcion
            static async Task<UpdateResult> GetMeta<T>(string targetTable, string targetAPI)
            {
                // truncate
                (bool success, string result) = await DBHelper.DatabaseExecute($"truncate table {targetTable}");
                if (!success) return new UpdateResult(false, result);

                // acqury
                (success, List<T> elements) = await MakeCall<T>($"{targetAPI}?apikey={DBHelper.Masterkey}");
                if (!success) return new UpdateResult(false, "Külső szolgáltató hiba!");

                // insert commands
                string[] inserts = [];
                if (targetTable == "country")  inserts = elements.Cast<Country>() .Select(x => $"insert into country (name, iso3, currency) values ('{x.name.Replace("'", "''")}', '{x.iso3.Replace("'", "''")}', '{x.currency}')").ToArray();
                if (targetTable == "exchange") inserts = elements.Cast<Exchange>().Select(x => $"insert into exchange (name, country) values ('{x.name.Replace("'", "''")}', '{x.country.Replace("'", "''")}')").ToArray();

                // upload
                foreach (string n in inserts)
                {
                    (success, result) = await DBHelper.DatabaseExecute(n);
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
            (bool success1, string result1) = await DBHelper.DatabaseExecute("truncate table instrument_input");
            if (!success1) return new UpdateResult(false, result1);

            // security types
            foreach (string n in new string[] { "stocks", "forex_pairs", "cryptocurrencies", "indices", "commodities" /*", funds", "bonds", "etfs"*/ })
            {
                // third party API call
                (bool success2, List<Instrument> instruments) = await MakeCall<Instrument>($"{n}");
                if (!success2) return new UpdateResult(false, "Külső szolgáltató hiba!");

                // bulk upload input data
                (success2, string result2) = await DBHelper.DatabaseBulkImport(instruments, n);
                if (!success2) return new UpdateResult(false, result2);

            }

            // done
            return new UpdateResult(true, "");
        }

        // Database update : executing stored procedure to consolidate fresh input
        static async Task<UpdateResult> UpdateResultTable()
        {
            // consolidating data
            (bool success, string result) = await DBHelper.DatabaseExecute("call consolidate_instruments()");
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
                Instrument[] s = elements.Cast<Instrument>().SelectMany(x => (x.available_exchanges ?? []).Select(y => new Instrument()
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
            public required string currency;
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