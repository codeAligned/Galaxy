using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Galaxy.DatabaseService;
using log4net;

namespace Galaxy.DatabaseService
{
    public class DbManager : IDbManager
    {
        private readonly ILog log = LogManager.GetLogger (MethodBase.GetCurrentMethod().DeclaringType);

        public bool TestConnection()
        {
            using (var db = new DevDbContext())
            {
                DbConnection conn = db.Database.Connection;
                try
                {
                    conn.Open();   // check the database connection
                    log.Info("Database connection succeed");
                    return true;
                }
                catch
                {
                    log.Error("Database connection failed");
                    return false;
                }
            }
        }

        public bool TestClosePrice(DateTime snapshotDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from b in db.HistoricalPrice
                            where b.AsOfDate == snapshotDate
                            select b;
                int res = query.ToArray().Count();

                return (res != 0);
            }
        }

        public Deal[] GetAllDeals()
        {
            var stopwatch = new Stopwatch();
            Deal[] res;
            stopwatch.Start();

            using (var db = new DevDbContext())
            {
                var query = from b in db.Deal
                            where b.Status != "Deleted" && b.Status != "Expired"
                            select b;

                query = query.Include(i => i.Instrument);
                query = query.Include(p => p.Instrument.Product);

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} deals loaded in: {stopwatch.Elapsed}" );
            return res;
        }

        public Deal[] GetAllExpiredDeals()
        {
            var stopwatch = new Stopwatch();
            Deal[] res;

            using (var db = new DevDbContext())
            {
                var query = from b in db.Deal
                            where b.Status == "Expired"
                            select b;

                query = query.Include(i => i.Instrument);
                query = query.Include(p => p.Instrument.Product);

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} expired deals loaded in: {stopwatch.Elapsed}");
            return res;
        }

        public void AddDeal(Deal newDeal)
        {
            using (var db = new DevDbContext())
            {
                db.Deal.Add(newDeal);
                db.SaveChanges();
            }

            log.Info($"New deal instru: {newDeal.InstrumentId} price: {newDeal.ExecPrice} " +
                     $"quantity: {newDeal.Quantity} by: {newDeal.UserProfil}");
        }

        public void AddVolParameter(VolParam newVolparam)
        {
            using (var db = new DevDbContext())
            {
                db.VolParam.Add(newVolparam);
                db.SaveChanges();
            }

            log.Info($"Add New Vol product: {newVolparam.Product} price: {newVolparam.MaturityDate}");
        }
    

        public void UpdateDeal(Deal updatedDeal)
        {
            using (var db = new DevDbContext())
            {
                Deal deal = db.Deal.First(i => i.DealId == updatedDeal.DealId);
                deal.TraderId = updatedDeal.TraderId;
                deal.Quantity = updatedDeal.Quantity;
                deal.ExecPrice = updatedDeal.ExecPrice;
                deal.BookId = updatedDeal.BookId;
                deal.InstrumentId = updatedDeal.InstrumentId;
                deal.ClearingFee = updatedDeal.ClearingFee;
                deal.TransactionFee = updatedDeal.TransactionFee;
                deal.Broker = updatedDeal.Broker;
                deal.Counterparty = updatedDeal.Counterparty;
                deal.Comment = updatedDeal.Comment;
                deal.Status = updatedDeal.Status;
                db.SaveChanges();
            }

            log.Info($"Update deal instru: {updatedDeal.InstrumentId} price: {updatedDeal.ExecPrice} " +
                     $"quantity: {updatedDeal.Quantity} by: {updatedDeal.TraderId}");

        }

        public void RemoveDeal(int dealId)
        {
            using (var db = new DevDbContext())
            {
                var deal = db.Deal.First(i => i.DealId == dealId);
                deal.Status = "Deleted";
                db.SaveChanges();
            }
            log.Info($"Remove deal ID: {dealId}");
        }

        public string[] GetAllOptionNames(DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from a in db.Instrument
                            where a.Product.ProductType == "OPTION" && a.MaturityDate >= asOfDate
                            select a.Id;

                return query.ToArray();
            }
        }

        public string[] GetAllFrontUserIds()
        {
            using (var db = new DevDbContext())
            {
                var query = from a in db.UserProfil
                            where a.Job == "Trader" || a.Job == "Developer"
                            select a.UserId;

                return query.ToArray();
            }
        }


        public Instrument[] GetAllFutures(DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from a in db.Instrument
                            where a.Product.ProductType == "FUTURE" && a.MaturityDate > asOfDate
                            select a;

                query = query.Include(a => a.Product);

                return query.ToArray();
            }
        }

        public Instrument GetFuture(string futureId)
        {
            using (var db = new DevDbContext())
            {
                var query = from a in db.Instrument
                            where a.TtCode == futureId
                            select a;

                query = query.Include(a => a.Product);
                Instrument[] instrCollection = query.ToArray();

                if (instrCollection.Length != 1)
                {
                    log.Error($" {futureId} is missing");
                    return null;
                }
                return instrCollection[0];
            }
        }

        public Instrument[] GetCallOptions(string productId, DateTime maturity)
        {
            using (var db = new DevDbContext())
            {
                var query = from ins in db.Instrument
                            where
                                ins.Product.ProductType == "OPTION" && ins.ProductId == productId &&
                                ins.MaturityDate == maturity && ins.OptionType == "CALL"
                            select ins;

                query = query.Include(a => a.Product);

                return query.ToArray();
            }
        }

        public Instrument[] GetAllInstruments(DateTime asOfDate)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Instrument[] res;

            using (var db = new DevDbContext())
            {
                var query = from a in db.Instrument
                            where a.MaturityDate >= asOfDate
                            select a;

                query = query.Include(a => a.Product);

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} instruments loaded in: {stopwatch.Elapsed}");
            return res;
        }

        public Product[] GetAllProducts()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Product[] res;

            using (var db = new DevDbContext())
            {
                var query = from a in db.Product
                            select a;

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} products loaded in: {stopwatch.Elapsed}");
            return res;
        }

        public string[] GetOptionProductNames()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string[] res;

            using (var db = new DevDbContext())
            {
                var query = from a in db.Product
                            where a.ProductType == "OPTION"
                            select a.Id;

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} products loaded in: {stopwatch.Elapsed}");
            return res;
        }

        public double GetSpotClose(DateTime closeDate, string futureTtCode)
        {
            using (var db = new DevDbContext())
            {
                var query = from hp in db.HistoricalPrice
                    where hp.AsOfDate == closeDate && hp.Instrument.TtCode == futureTtCode
                    select hp.ClosePrice;

                double[] closeCollection = query.ToArray();

                if (closeCollection.Length != 1)
                {
                    log.Error($" {futureTtCode} close price is missing");
                    return 0;
                }
                return closeCollection[0];
            }
        }
        //todo must be removed
        public HistoricalPrice[] GetOptionsClosePrice(int upStrike, int downStrike, DateTime maturityDate, DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from hp in db.HistoricalPrice
                            where
                                hp.AsOfDate == asOfDate && hp.Instrument.MaturityDate == maturityDate &&
                                (hp.Instrument.Strike == downStrike || hp.Instrument.Strike == upStrike)
                            select hp;
                query = query.Include(a => a.Instrument);

                return query.ToArray();
            }
        }

        public double GetOptionsClosePrice(int Strike, string optionType, DateTime maturityDate, DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from hp in db.HistoricalPrice
                            where
                                hp.AsOfDate == asOfDate && 
                                hp.Instrument.MaturityDate == maturityDate &&
                                hp.Instrument.Strike == Strike &&
                                hp.Instrument.OptionType == optionType
                            select hp.ClosePrice;

                double[] closeCollection = query.ToArray();

                if (closeCollection.Length != 1)
                {
                    log.Error($"Close price is missing");
                    return 0;
                }
                return closeCollection[0];
            }
        }

        public VolParam[] GetAllVolParams(DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = from vp in db.VolParam
                            where vp.MaturityDate > asOfDate
                            select vp;

                return query.ToArray();
            }
        }

        public VolParam GetVolParams(string product, DateTime maturity)
        {
            using (var db = new DevDbContext())
            {
                var query = from vp in db.VolParam
                            where vp.MaturityDate == maturity && vp.ProductId == product
                            select vp;

                VolParam[] res = query.ToArray();
                return res[0];
            }
        }

        public void UpdateVolParams(VolParam updatedParam)
        {
            using (var db = new DevDbContext())
            {
                VolParam param = db.VolParam.First(i => i.MaturityDate == updatedParam.MaturityDate && i.ProductId == updatedParam.ProductId);
                param.A = updatedParam.A;
                param.B = updatedParam.B;
                param.Sigma = updatedParam.Sigma;
                param.Rho = updatedParam.Rho;
                param.M = updatedParam.M;
                param.Accuracy = updatedParam.Accuracy;
                param.Guess = updatedParam.Guess;
                param.LowerBound = updatedParam.LowerBound;
                param.UpperBound = updatedParam.UpperBound;
                db.SaveChanges();
            }

            log.Info($"Volatility parameters updated for maturity: {updatedParam.MaturityDate} product: {updatedParam.ProductId}");
            log.Info($"New parameters:  A={updatedParam.A} B={updatedParam.B} Sigma={updatedParam.Sigma} Rho={updatedParam.Rho} M={updatedParam.M}" );
        }

        public void AddClosePrice(List<HistoricalPrice> priceCollection)
        {
            using (var db = new DevDbContext())
            {
                foreach (var price in priceCollection)
                {
                    db.HistoricalPrice.Add(price);
                }
                
                db.SaveChanges();
            }

            log.Info($"{priceCollection.Count} Closing prices inserted");
        }

        public void AddInstruments(List<Instrument> newInstruments)
        {
            using (var db = new DevDbContext())
            {
                foreach (var price in newInstruments)
                {
                    db.Instrument.Add(price);
                }

                db.SaveChanges();
            }

            log.Info($"{newInstruments.Count} new instruments inserted");
        }

        public DateTime[] GetAvailableMaturity(DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = (from ins in db.Instrument
                    where ins.MaturityDate >= asOfDate && ins.Product.ProductType != "FUTURE"
                    select ins.MaturityDate).Distinct().OrderBy(MaturityDate => MaturityDate);

                DateTime[] res = query.ToArray();
                return res;
            }
        }

        public Dictionary<string, double> GetAllClosePrices(DateTime asOfDate)
        {
            using (var db = new DevDbContext())
            {
                var query = (from hp in db.HistoricalPrice
                    where hp.AsOfDate == asOfDate
                    select hp);


                Dictionary<string,double> closePriceDico = new Dictionary<string, double>();
                foreach (var price in query.ToArray())
                {
                    closePriceDico.Add(price.InstrumentId,price.ClosePrice);
                }
    
                return closePriceDico;
            }
        }

        public string[] GetDailyReportMailingList()
        {
            using (var db = new DevDbContext())
            {
                var query = (   from up in db.UserProfil
                                where up.DailyReport == true 
                                select up.Email);

                return query.ToArray();
            }
        }

        public Deal[] GetIntradayDeals(DateTime bookingDate)
        {
            var stopwatch = new Stopwatch();
            Deal[] res;

            using (var db = new DevDbContext())
            {
                var query = from b in db.Deal
                    where
                        b.TradeDate.Value.Day == bookingDate.Day && b.TradeDate.Value.Month == bookingDate.Month &&
                        b.TradeDate.Value.Year == bookingDate.Year
                    orderby b.TradeDate.Value ascending
                    select b;

                res = query.ToArray();
            }

            stopwatch.Stop();
            log.Info($"{res.Length} intraday deals loaded in: {stopwatch.Elapsed}");
            return res;
        }
    }
}
