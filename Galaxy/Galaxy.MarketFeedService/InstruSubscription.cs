namespace Galaxy.MarketFeedService
{
    internal class InstruSubscription
    {
        public InstruSubscription(string priceType, string instruType, string productName, string market, string instrumentId)
        {
            IsSuscribed = false;
            PriceType = priceType;
            InstruType = instruType;
            ProductName = productName;
            Market = market;
            InstruId = instrumentId;
        }

        public bool IsSuscribed { get; set; }
        public string PriceType { get; set; }
        public double ClosePrice { get; set; }

        public string InstruType { get; set; }
        public string ProductName { get; set; }
        public string Market { get; set; }
        public string InstruId { get; set; }
    }
}
