using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using Galaxy.MarketFeedService;
using Galaxy.DatabaseService;
using Galaxy.PricingService;
using log4net;

namespace Pipe
{
    class NewInstruSnapshot : IProcess
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IMarketFeed _marketFeed;
        private readonly IDbManager _dbManager;
        private readonly string _ttlogin = ConfigurationManager.AppSettings["Login"];
        private readonly string _ttPassword = ConfigurationManager.AppSettings["PassWord"];
        public bool IsRunning { get; set; } = true;
        private static ConcurrentDictionary<string, Instrument> _instruInfoSafeDico;
        private readonly int _delayTime = int.Parse(ConfigurationManager.AppSettings["DelayTime"]);
        private readonly int _nbOfExpi = int.Parse(ConfigurationManager.AppSettings["NbOfExpi"]);

        private DateTime _delay;

        public NewInstruSnapshot(IMarketFeed marketFeed, IDbManager dbManager)
        {
            _marketFeed = marketFeed;
            _dbManager = dbManager;
            _instruInfoSafeDico = new ConcurrentDictionary<string, Instrument>();
        }

        /// <summary>
        /// 1) Retreive all instruments from TTAPI
        /// 2) Retreive all instruments from Database
        /// 3) Insert the delta into Database
        /// 4) Create default vol parameters for new maturity
        /// </summary>
        public void Start()
        {
        
            _marketFeed.Connect(_ttlogin, _ttPassword, ConnectionStatusHandler, null, DataUpdateHandler);
            Instrument[] instruCollection = _dbManager.GetAllInstruments(DateTime.Today);

            //wait 30sec to receive all the instruments info from MarketFeed
            _delay = DateTime.Now.AddSeconds(_delayTime);
            while (DateTime.Now < _delay)
            {
                Thread.Sleep(1000);
            }

            // remove from InstruInfoSafeDico instruments already present in DB
            foreach (var instru in instruCollection)
            {
                if (_instruInfoSafeDico.ContainsKey(instru.Id))
                {
                    Instrument suppresedInstru;
                    _instruInfoSafeDico.TryRemove(instru.Id, out suppresedInstru);
                }
            }

            // insert new instruments into DB
            List<Instrument> Newinstru = _instruInfoSafeDico.Values.ToList();
            if (Newinstru.Any())
            {
                _dbManager.AddInstruments(Newinstru);
            }

            // load db vol parameters
            Dictionary<string, VolParam> volParamDico = LoadVolParams();
            string[] optionList = _dbManager.GetOptionProductNames();
            DateTime[] maturityList = _dbManager.GetAvailableMaturity(DateTime.Today);

            foreach (var option in optionList)
            {
                foreach (var maturity in maturityList)
                {
                    string key = option + "_" + maturity.ToString("MMyyyy");
                    if (!volParamDico.ContainsKey(key))
                    {
                        // create default vol parameters if no params exist
                        var volParam = new VolParam() {A = 0,B = 0,Sigma = 0,Rho = 0, M = 0,MaturityDate = maturity,ProductId = option};
                        _dbManager.AddVolParameter(volParam);
                    }
                }
            }

            _marketFeed.Dispose();
        }

        private Dictionary<string, VolParam> LoadVolParams()
        {
            VolParam[] volParams = _dbManager.GetAllVolParams(DateTime.Today);
            var volParamDico = new Dictionary<string, VolParam>();
            foreach (var param in volParams)
            {
                string key = param.ProductId + "_" + param.MaturityDate.ToString("MMyyyy");

                if (!volParamDico.ContainsKey(key))
                {
                    volParamDico.Add(key, param);
                }
            }
            return volParamDico;
        }

        private void ConnectionStatusHandler(IMarketFeed sender, string connectionState)
        {
            if (connectionState == "Connection_Down")
            {
                IsRunning = false;
            }
            else if (connectionState == "Connection_Succeed")
            {
                Product[] products = _dbManager.GetAllProducts();

                foreach (var product in products)
                {
                    _marketFeed.SuscribeToProductInfo(product.ProductType, product.Id, product.Market);
                }
            }
        }

        private void DataUpdateHandler(IMarketFeed sender, string instrumentName, DateTime maturity, int strike, string optionType, string optionDescription, string productType, string productName)
        {
            _delay = DateTime.Now.AddSeconds(_delayTime);
            if (productType == "FUTURE")
            {
                DateTime nextMaturity = DateTime.Today.AddMonths(_nbOfExpi + 3);
                if (maturity > nextMaturity)
                {
                    return;
                }

                string reutersCode = Option.BuildForwardId("STXE", maturity);
                var newInstru = new Instrument() { MaturityDate = maturity, TtCode = instrumentName, Id = reutersCode, ProductId = productName, FullName = optionDescription, };
                _instruInfoSafeDico[reutersCode] = newInstru;
            }
            else if (productType == "OPTION")
            {
                DateTime nextMaturity = DateTime.Today.AddMonths(_nbOfExpi);
                if (maturity > nextMaturity)
                {
                    return;
                }

                string reutersCode = Option.BuildOptionId(optionType, maturity, "STXE" + (strike * 10),".EX");
                string forwardId = Option.BuildForwardId("STXE", maturity);
                string futureId = Option.GetNextFutureTtCode("FESX", maturity);
               
                var newInstru = new Instrument() { MaturityDate = maturity, TtCode = instrumentName, Id = reutersCode, OptionType = optionType, Strike = strike, ProductId = productName, FullName = optionDescription, RefForwardId = forwardId, RefFutureId = futureId };
                _instruInfoSafeDico[reutersCode] = newInstru;
            }
            else
            {
                log.Error($"Instruments: {instrumentName} not recognize");
            }   
        }
    }
}
