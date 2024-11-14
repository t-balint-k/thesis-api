namespace thesis_api
{
    // universal security entitiy holding all possible fields
    public class security
    {
        public string symbol;
        public string name;
        public string exchange;
        public string currency;
        public string country;
        public string type;
        public string currency_base;
        public string currency_quote;
        public string[] available_exchanges;
    }

    // countries list
    public class country
    {
        public string name;
        public string iso3;
    }

    // exchanges list
    public class exchange
    {
        public string name;
        public string country;
    }

    // packet type 1: stocks, forex_pairs, cryptocurrencies, commodities
    public class packet<T>
    {
        public List<T> data;
    }
}