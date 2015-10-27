
using System;

namespace Galaxy.MarketFeedService
{
    public delegate void ConnectionDelegate(IMarketFeed sender, string connectionState);
    public delegate void PriceUpdateDelegate(IMarketFeed sender, string instrumentId, double price);
    public delegate void DataUpdateDelegate(IMarketFeed sender, string instrumentId, DateTime maturity, int strike, string optionType, string optionName, string productType, string productName);

    public interface IMarketFeed
    {
        event PriceUpdateDelegate PriceUpdateEvent;
        event ConnectionDelegate ConnectionStatusEvent;

        void Connect(string login, string password, ConnectionDelegate ConnectionUpdate, PriceUpdateDelegate PriceUpdate,DataUpdateDelegate DataUpdateHandler );
        void SuscribeToInstrumentPrice(string ttInstrumentId, string instruType, string productName, string market, string priceType);
        void SuscribeToProductInfo(string instruType, string productName, string market);
        void Dispose();
    }
}