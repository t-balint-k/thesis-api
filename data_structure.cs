namespace thesis_api
{
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
    }
    public class packet
    {
        public List<security> data;
    }
}