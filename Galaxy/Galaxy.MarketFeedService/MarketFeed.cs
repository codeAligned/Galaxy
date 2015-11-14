using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TradingTechnologies.TTAPI;
using log4net;

namespace Galaxy.MarketFeedService
{
    public class MarketFeed : IDisposable, IMarketFeed
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private WorkerDispatcher _disp;
        private UniversalLoginTTAPI _apiInstance;

        private MarketCatalog _marketCatalog;
        private ProductCatalogSubscription _prodCat;

        private readonly Dictionary<string, Market> _availableMarketsDico;
        private readonly Dictionary<string, Product> _availableProductsDico;
        private readonly Dictionary<string, Instrument> _availableInstrumentDico;

        private readonly Dictionary<string, bool> _marketToSubscribe;
        private readonly Dictionary<string, bool> _productToSubscribe;
        private readonly Dictionary<string, InstruSubscription> _instrumentToSubscribe;
        private readonly ConcurrentQueue<InstruSubscription> _instruQueue;
        private readonly List<InstrumentLookupSubscription> m_lreq = new List<InstrumentLookupSubscription>();
        private readonly List<PriceSubscription> m_ltsSub = new List<PriceSubscription>();
        private InstrumentLookupSubscription priceSub = null;

        private string _username;
        private string _password;

        public event PriceUpdateDelegate PriceUpdateEvent;
        public event ConnectionDelegate ConnectionStatusEvent;
        public event DataUpdateDelegate DataUpdateEvent;

        private bool _disposed;
        private readonly object _lock = new object();

        private bool subscriptionstarted = false; 

        public MarketFeed()
        {
            _marketToSubscribe = new Dictionary<string, bool>();
            _productToSubscribe = new Dictionary<string, bool>();
            _instrumentToSubscribe = new Dictionary<string, InstruSubscription>();

            _availableMarketsDico = new Dictionary<string, Market>();
            _availableProductsDico = new Dictionary<string, Product>();
            _availableInstrumentDico = new Dictionary<string, Instrument>();

            _instruQueue = new ConcurrentQueue<InstruSubscription>();
        }

        public void Connect(string login, string password, ConnectionDelegate connUpdateHandler, PriceUpdateDelegate PriceUpdateHandler, DataUpdateDelegate DataUpdateHandler)
        {
            _username = login;
            _password = password;

            if (connUpdateHandler != null)
            {
                ConnectionStatusEvent += connUpdateHandler;
            }
            if (PriceUpdateHandler != null)
            {
                PriceUpdateEvent += PriceUpdateHandler;
            }
            if (DataUpdateHandler != null)
            {
                DataUpdateEvent += DataUpdateHandler;
            }


            var marketFeedThread = new Thread(Initialize) {IsBackground = true, Name = "TT API Thread"};
            marketFeedThread.Start();
        }

        /// <summary>
        /// Create and start the Dispatcher
        /// </summary>
        public void Initialize()
        {
            if (CheckArchitecture())
            {
                // Attach a WorkerDispatcher to the current thread
                _disp = Dispatcher.AttachWorkerDispatcher();
                _disp.BeginInvoke(Init);
                _disp.Run();

                var delay = DateTime.Now.AddSeconds(30);

                while (DateTime.Now.CompareTo(delay) > 0)
                {
                    Dispose();
                }
            }
        }

        private bool CheckArchitecture()
        {
            var archCheck = new TTAPIArchitectureCheck();
            if (archCheck.validate())
            {
                log.Info("Architecture check passed.");
                return true;
            }

            log.Error($"Architecture check failed.  {archCheck.ErrorString}");
            return false;
        }

        /// <summary>
        /// Initialize TT API
        /// </summary>
        private void Init()
        {
            log.Info("Connecting to TT API...");
            try
            {
                // Use "Universal Login" Login Mode
                ApiInitializeHandler h = InitComplete;
                TTAPI.CreateUniversalLoginTTAPI(Dispatcher.Current, _username, _password, h);
            }
            catch (Exception)
            {
                log.Info("Connection failed");
            }
          
        }

        /// <summary>
        /// Event notification for status of TT API initialization
        /// </summary>
        private void InitComplete(TTAPI api, ApiCreationException ex)
        {
            if (ex == null)
            {
                // Authenticate your credentials
                _apiInstance = (UniversalLoginTTAPI)api;
                _apiInstance.AuthenticationStatusUpdate += AuthenticationStatusUpdate;
                _apiInstance.Start();
            }
            else
            {
                log.Info($"Connection to TT API Failed: {ex.Message}");
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for status of authentication
        /// </summary>
        private void AuthenticationStatusUpdate(object sender, AuthenticationStatusUpdateEventArgs e)
        {
            if (e.Status.IsSuccess)
            {
                log.Info("TT Login Succed");
                ConnectionStatusEvent?.Invoke(this, "Connection_Succeed");
            }
            else
            {
                log.Info($"TT Login failed: {e.Status.StatusMessage}");
                ConnectionStatusEvent?.Invoke(this, "Connection_Failed");
                Dispose();
            }
        }

        public void SuscribeToInstrumentPrice(string ttInstrumentId, string instruType, string productName, string market, string priceType)
        {
            if (!_instrumentToSubscribe.ContainsKey(ttInstrumentId))
            {
                var newInstruSub = new InstruSubscription(priceType, instruType, productName, market, ttInstrumentId);
                _instrumentToSubscribe.Add(ttInstrumentId, newInstruSub);
                _instruQueue.Enqueue(newInstruSub);
                _disp.BeginInvoke(UpdateInstrumentSubscription);
                _disp.Run();
            }
        }

        public void SuscribeToProductInfo(string instruType, string productName, string market)
        {
            if (!_marketToSubscribe.ContainsKey(market))
            {
                _marketToSubscribe.Add(market, false);
            }

            string productKey = market + "_" + instruType + "_" + productName;
            if (!_productToSubscribe.ContainsKey(productKey))
            {
                _productToSubscribe.Add(productKey, false);
            }

            if (!Dispatcher.IsAttached)
            {
                UIDispatcher mDisp = Dispatcher.AttachUIDispatcher();
                mDisp.Invoke(UpdateMarketsubscription, market);
                mDisp.Invoke(UpdateProductSubscription, productKey);
            }
            else
            {
                UpdateMarketsubscription(market);
                UpdateProductSubscription(productKey);
            }

            if(!subscriptionstarted)
                StartSubscription();
        }

        private void StartSubscription()
        {
            _marketCatalog = _apiInstance.Session.MarketCatalog;
            _marketCatalog.MarketsUpdated += MarketsUpdated;

            // CheckArchitecture the order and fill feeds so that they will be displayed in the "Market Feed Status" window.
            _apiInstance.StartOrderFeed();
            _apiInstance.StartFillFeed();
        }

        private void MarketsUpdated(object sender, MarketCatalogUpdatedEventArgs e)
        {
            foreach (Market market in _apiInstance.Session.MarketCatalog.Markets.Values)
            {
                if (!_availableMarketsDico.ContainsKey(market.Name))
                {
                    _availableMarketsDico.Add(market.Name, market);
                }
                UpdateMarketsubscription(market.Name);
            }
        }

        private void UpdateMarketsubscription(string market)
        {
            if (_availableMarketsDico.ContainsKey(market))
            {
                if (_marketToSubscribe.ContainsKey(market) && _marketToSubscribe[market] == false)
                {
                    SuscribeToProductCatalogue(_availableMarketsDico[market]);
                    _marketToSubscribe[market] = true;
                }
            }
        }

        private void SuscribeToProductCatalogue(Market market)
        {
            log.Info($"Suscribe to market: {market.Name}");
            _prodCat = market.CreateProductCatalogSubscription(Dispatcher.Current);
            _prodCat.ProductsUpdated += ProductsUpdated;
            _prodCat.Start();
        }

        /// <summary>
        /// ProductCatalogSubscription ProductsUpdated event.
        /// This will update the option list in the option TreeView.
        /// </summary>
        /// <param productName="sender">ProductCatalogSubscription</param>
        /// <param productName="e">ProductCatalogUpdatedEventArgs</param>
        private void ProductsUpdated(object sender, ProductCatalogUpdatedEventArgs e)
        {
            var sub = (ProductCatalogSubscription)sender;
            IEnumerable<Product> products = from Product product in sub.Products.Values
                                            select product;

            foreach (var product in products)
            {
                string productKey = product.Market + "_" + product.Type + "_" + product.Name;

                if (!_availableProductsDico.ContainsKey(productKey))
                {
                    _availableProductsDico.Add(productKey, product);
                }

                UpdateProductSubscription(productKey);
            }
        }

        private void UpdateProductSubscription(string productKey)
        {
            if (_availableProductsDico.ContainsKey(productKey))
            {
                if (_productToSubscribe.ContainsKey(productKey) && _productToSubscribe[productKey] == false)
                {
                    SubscribeToInstrumentCatalog(_availableProductsDico[productKey]);
                    _productToSubscribe[productKey] = true;
                }
            }
        }

        /// <summary>
        /// Subscribe to the instrument catalog for a given option.
        /// </summary>
        /// <param productName="product">Product to subscribe to.</param>
        private void SubscribeToInstrumentCatalog(Product product)
        {
            log.Info($"Suscribe to product: {product.Name}");
            InstrumentCatalogSubscription instrumentCatalogSubscription = new InstrumentCatalogSubscription(product, Dispatcher.Current);
            instrumentCatalogSubscription.InstrumentsUpdated += InstrumentsUpdated;
            instrumentCatalogSubscription.Start();
        }

        /// <summary>
        /// InstrumentCatalogSubscription InstrumentsUpdated event.
        /// </summary>
        private void InstrumentsUpdated(object sender, InstrumentCatalogUpdatedEventArgs e)
        {
            InstrumentCatalogSubscription instrumentCatalogSubscription = sender as InstrumentCatalogSubscription;

            if (instrumentCatalogSubscription?.Instruments?.Values != null)
            {
                foreach (Instrument instru in instrumentCatalogSubscription.Instruments.Values)
                {
                    if (!_availableInstrumentDico.ContainsKey(instru.Key.SeriesKey))
                    {
                        _availableInstrumentDico.Add(instru.Key.SeriesKey, instru);
                        InstrumentDetails instruDetails = instru.InstrumentDetails;
                        DataUpdateEvent?.Invoke(this, instruDetails.Key.SeriesKey, instruDetails.ExpirationDate.ToDateTime(),instruDetails.StrikePrice,instruDetails.OptionType.ToString().ToUpper(),instruDetails.Name, instru.Product.Type.Name,instru.Product.Name);
                    }
                }
            }
        }

        private void UpdateInstrumentSubscription()
        {
            InstruSubscription instru;
            if (_instruQueue.TryDequeue(out instru))
            {
                log.Info($"Suscribe to instrument: {instru.InstruId}");
                priceSub = new InstrumentLookupSubscription(_apiInstance.Session, Dispatcher.Current, new InstrumentKey(instru.Market, instru.InstruType, instru.ProductName, instru.InstruId));

                priceSub.Update += ReqUpdate;
                m_lreq.Add(priceSub);
                priceSub.Start();
            }
        }


        private void ReqUpdate(object sender, InstrumentLookupSubscriptionEventArgs e)
        {
            if (e.Instrument != null && e.Error == null)
            {
                // Subscribe for Inside Market Data
                var priceSub = new PriceSubscription(e.Instrument, Dispatcher.Current);
                priceSub.Settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);
                priceSub.FieldsUpdated += PriceFieldsUpdated;
                m_ltsSub.Add(priceSub);
                priceSub.Start();
            }
            else if (e.IsFinal)
            {
                // Instrument was not found and TT API has given up looking for it
                log.Info($"Cannot find instrument: {e.Error.Message}");
                Dispose();
            }
        }

        private void PriceFieldsUpdated(object sender, FieldsUpdatedEventArgs e)
        {
            Price bid = e.Fields.GetBestBidPriceField().Value;
            Price ask = e.Fields.GetBestAskPriceField().Value;
            Price close = e.Fields.GetSettlementPriceField().Value; // closeField is contains wrong values

            string instrumentTtCode = e.Fields.Instrument.Key.SeriesKey;

            if (_instrumentToSubscribe[instrumentTtCode].PriceType == "Mid")
            {
                if (e.Error == null && bid.IsTradable && ask.IsTradable)
                {
                    log.Debug($"instru: {instrumentTtCode} mid: {(bid.ToDouble() + ask.ToDouble()) / 2}");
                    PriceUpdateEvent?.Invoke(this, instrumentTtCode, (bid.ToDouble() + ask.ToDouble()) / 2);
                }
            }
            else if (_instrumentToSubscribe[instrumentTtCode].PriceType == "Close")
            {
                if (_instrumentToSubscribe[instrumentTtCode].ClosePrice != close.ToDouble())
                {
                    log.Debug($"instru: {instrumentTtCode} close: {close.ToDouble()}");

                    if (e.Error == null && close.IsValid)
                    {
                        PriceUpdateEvent?.Invoke(this, instrumentTtCode, close.ToDouble());
                        _instrumentToSubscribe[instrumentTtCode].ClosePrice = close.ToDouble();
                    }
                }
            }
        }

        /// <summary>
        /// Shuts down the TT API
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    foreach (InstrumentLookupSubscription req in m_lreq)
                    {
                        if (req != null)
                        {
                            req.Update -= ReqUpdate;
                            req.Dispose();
                        }
                    }

                    foreach (PriceSubscription tsSub in m_ltsSub)
                    {
                        if (tsSub != null)
                        {
                            tsSub.FieldsUpdated -= PriceFieldsUpdated;
                            tsSub.Dispose();
                        }
                    }
                   
                    // Begin shutdown the TT API
                    TTAPI.ShutdownCompleted += ShutdownCompleted;
                    TTAPI.Shutdown();
           

                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Event notification for completion of TT API shutdown
        /// </summary>
        private void ShutdownCompleted(object sender, EventArgs e)
        {
            // Shutdown the Dispatcher
            if (_disp != null)
            {
                _disp.InvokeShutdown();
                _disp = null;
            }

            ConnectionStatusEvent?.Invoke(this, "Connection_Down");

            // Dispose of any other objects / resources
        }
    }
}