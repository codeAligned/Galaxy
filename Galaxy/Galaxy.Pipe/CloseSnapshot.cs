using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading;
using Galaxy.MarketFeedService;
using Galaxy.DatabaseService;
using Galaxy.PricingService;
using log4net;

namespace Pipe
{
    public class CloseSnapshot : IProcess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IMarketFeed _marketFeed;
        private readonly IDbManager _dbManager;
        private readonly ConcurrentDictionary<string, double> _instrumentPriceSafeDico;
        private readonly string _ttlogin = ConfigurationManager.AppSettings["Login"];
        private readonly string _ttPassword = ConfigurationManager.AppSettings["PassWord"];
        private readonly int _delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
        private DateTime _delay;

        private Instrument[] _instruCollection;
        public bool IsRunning { get; set; } = true;

        public CloseSnapshot(IMarketFeed marketFeed, IDbManager dbManager)
        {
            _marketFeed = marketFeed;
            _dbManager = dbManager;
            _instrumentPriceSafeDico = new ConcurrentDictionary<string, double>();
        }

        public void Start()
        {
            // Check if closing snapshot batch as run
            if (_dbManager.TestClosePrice(Option.PreviousWeekDay(DateTime.Today)))
            {
                log.Error("Close price already inserted");
                return;
            }

            _marketFeed.Connect(_ttlogin, _ttPassword, ConnectionStatusHandler, PriceUpdateHandler, null);

            _delay = DateTime.Now.AddSeconds(_delayTime);
            while (DateTime.Now < _delay)
            {
                Thread.Sleep(1000);
            }

            var histoCollection = new List<HistoricalPrice>();

            DateTime previousDay = Option.PreviousWeekDay(DateTime.Today);

            foreach (var instru in _instruCollection)
            {
                double closePrice;
                if (_instrumentPriceSafeDico.TryGetValue(instru.TtCode, out closePrice) && closePrice != 0)
                {
                    histoCollection.Add(new HistoricalPrice() { AsOfDate = previousDay, ClosePrice = closePrice, InstrumentId = instru.Id });
                }
                else
                {
                    log.Info($"Close missing instrument: {instru.TtCode}");
                }
            }

            _dbManager.AddClosePrice(histoCollection);
            _marketFeed.Dispose();
        }


        private void ConnectionStatusHandler(IMarketFeed sender, string connectionState)
        {
            if (connectionState == "Connection_Down")
            {
                IsRunning = false;
            }
            else if (connectionState == "Connection_Succeed")
            {
                _delay = DateTime.Now.AddSeconds(_delayTime);
                _instruCollection = _dbManager.GetAllInstruments(Option.PreviousWeekDay(DateTime.Today));

                foreach (var instru in _instruCollection)
                {
                    _marketFeed.SuscribeToInstrumentPrice(instru.TtCode, instru.Product.ProductType, instru.ProductId,instru.Product.Market, "Close");
                }
            }
        }

        private void PriceUpdateHandler(IMarketFeed sender, string instrumentName, double midPrice)
        {
            _instrumentPriceSafeDico[instrumentName] = midPrice;
            _delay = DateTime.Now.AddSeconds(_delayTime);
        }
    }
}
