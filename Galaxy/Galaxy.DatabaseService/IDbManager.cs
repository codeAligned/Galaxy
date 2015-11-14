using System;
using System.Collections.Generic;

namespace Galaxy.DatabaseService
{
    public interface IDbManager
    {
        bool TestConnection();
        bool TestClosePrice(DateTime snapshotDate);
        Deal[] GetAllDeals();
        Deal[] GetAllExpiredDeals();
        void AddDeal(Deal newDeal);
        void AddVolParameter(VolParam newVolparam);
        void UpdateDeal(Deal updatedDeal);
        void RemoveDeal(int dealId);
        string[] GetAllOptionNames(DateTime asOfDate);
        string[] GetAllFrontUserIds();
        Instrument[] GetAllFutures(DateTime asOfDate);
        Instrument GetFuture(string futureId);
        Instrument[] GetCallOptions(string productId, DateTime maturity);
        Instrument[] GetAllInstruments(DateTime asOfDate);
        Product[] GetAllProducts();
        string[] GetOptionProductNames();
        double GetSpotClose(DateTime closeDate, string futureTtCode);
        HistoricalPrice[] GetOptionsClosePrice(int upStrike, int downStrike, DateTime maturityDate, DateTime asOfDate);
        double GetOptionsClosePrice(int Strike, string optionType, DateTime maturityDate, DateTime asOfDate);
        VolParam[] GetAllVolParams(DateTime asOfDate);
        VolParam GetVolParams(string product, DateTime maturity);
        void UpdateVolParams(VolParam updatedParam);
        void AddClosePrice(List<HistoricalPrice> priceCollection);
        void AddInstruments(List<Instrument> newInstruments);
        DateTime[] GetAvailableMaturity(DateTime asOfDate);
        Dictionary<string, double> GetAllClosePrices(DateTime asOfDate);
        string[] GetDailyReportMailingList();
        Deal[] GetIntradayDeals(DateTime bookingDate);
        Product GetUnderlying(string productRef);
    }
}
