using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Galaxy.DatabaseService;
using Galaxy.MarketFeedService;
using Galaxy.PricingService;
using Galaxy.VolManager.Commands;
using log4net;
using PricingLib;


namespace Galaxy.VolManager.ViewModel
{
    public class VolManagerVM : INotifyPropertyChanged
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Parameters
        private VolParam _param;

        private readonly string _ttlogin = ConfigurationManager.AppSettings["Login"];
        private readonly string _ttPassword = ConfigurationManager.AppSettings["PassWord"];
        private readonly string _environment = ConfigurationManager.AppSettings["Environment"];
        private readonly string _version = ConfigurationManager.AppSettings["Version"];

        private readonly ConcurrentDictionary<string, double> _instrumentPriceSafeDico;
        private readonly Dictionary<string, double> _fwdBaseOffsetDico;

        private readonly IMarketFeed _marketFeed;
        private readonly IDbManager _dbManager;
        private string _marketFeedStatus = "DOWN";

        private bool marketConnectionUp = false;
        public ObservableCollection<DateTime> AvailableMaturity { get; set; }

        public string AppTitle { get; set; }

        private Instrument[] _options;

        #endregion

        #region Assessors
        public ICommand LoadVolCmd { get;}
        public ICommand FitCurveCmd { get; }
        public ICommand WindowsLoadedCmd { get; }
        public ICommand BeforeCloseCmd { get; }
        public ICommand InsertNewParamsCmd { get; }

        public int SelectedMinStrike { get; set; }
        public int SelectedMaxStrike { get; set; }

        public ObservableCollection<Point> ImpliedVolPoints { get; }
        public ObservableCollection<Point> ModelVolPoints { get; }

        public List<double> _strikeList;

        public DateTime SelectedMaturity { get; set; }
        public string SelectedProduct { get; set; } = "OESX";

        public VolParam Param
        {
            get { return _param; }
            set { _param = value; OnPropertyChanged(nameof(Param)); }
        }

        #endregion
        
        public VolManagerVM(IMarketFeed marketFeed, IDbManager dbManager)
        {
            var currentThread = Thread.CurrentThread;
            currentThread.Name = "UI Thread";

            LoadVolCmd = new RelayCommand(LoadData);
            FitCurveCmd = new  RelayCommand(FitCurve);
            WindowsLoadedCmd = new RelayCommand(WindowLoaded);
            InsertNewParamsCmd = new RelayCommand(InsertParamToDb);
            BeforeCloseCmd = new RelayCommand(ExecuteBeforeClose);

            ImpliedVolPoints = new ObservableCollection<Point>();
            ModelVolPoints = new ObservableCollection<Point>();
            AvailableMaturity = new ObservableCollection<DateTime>();
            _strikeList = new List<double>();
            _instrumentPriceSafeDico = new ConcurrentDictionary<string, double>();
            _fwdBaseOffsetDico = new Dictionary<string, double>();

            _marketFeed = marketFeed;
            _dbManager = dbManager;

            SelectedMinStrike = 2000;
            SelectedMaxStrike = 5000;

            AppTitle = $"Volatility Manager {_version} {_environment}";
        }

        private void WindowLoaded(object o)
        {
            log.Info("VolManager started");

            //Check Database connection
            if (!_dbManager.TestConnection())
            {
                MessageBox.Show("Enable to access to database. Check VPN connection");
                return;
            }

            // Check if closing snapshot as inserted prices
            if (!_dbManager.TestClosePrice(Option.PreviousWeekDay(DateTime.Today)))
            {
                MessageBox.Show("Close price missing. Check database insertion");
                return;
            }

            DateTime[] maturityArray = _dbManager.GetAvailableMaturity(DateTime.Today);
            foreach (var maturity in maturityArray)
            {
                AvailableMaturity.Add(maturity);
            }

            _marketFeed.Connect(_ttlogin, _ttPassword, ConnectionStatusHandler, PriceUpdateHandler, null);
            InitializeTimer();
        }

        // price update Callback from market feed service
        private void PriceUpdateHandler(IMarketFeed sender, string instrumentName, double midPrice)
        {
            _instrumentPriceSafeDico[instrumentName] = midPrice;
        }

        // connection state Callback from market feed service
        private void ConnectionStatusHandler(IMarketFeed sender, string connectionState)
        {
            if (connectionState == "Connection_Succeed")
            {
                marketConnectionUp = true;
            }
            else if (connectionState == "Connection_Down")
            {
                marketConnectionUp = false;
            }
        }
        
        private void InitializeTimer()
        {
            DispatcherTimer fastTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(2)};
            fastTimer.Tick += RefreshRtData;
            fastTimer.Start();
        }

        private void RefreshRtData(object sender, EventArgs e)
        {
            if ( !marketConnectionUp)
            {
                return;
            }

            if (_options == null || _options.Length == 0 || string.IsNullOrEmpty(SelectedProduct) || SelectedMaturity == DateTime.MinValue)
            {
             //   MessageBox.Show("Select product and maturity");
                return;
            }

            LoadImpliedVol(false);
            LoadModelVol(false);
        }


        private string GetOtmOptionType(int strike, double spot)
        {
            if (spot == 0)
            {
                var e = new Exception("Spot price cannot be zero");
                log.Error(e);
                throw e;
            }
            if (spot >= strike)
            {
                return "PUT";  //Target price: Put OTM
            }
            return "CALL";     //Target price: Call OTM
        }

        // For a specific strike and maturity, we use the option(call or put) which is out of the money
        // this methode is in charge to retreive the current option ric out of the money 
        private double GetOutTheMoneyOptionPrice(Instrument pos, string optionType)
        {
            // build option tt code
            string targetOptionTtCode = Option.GetOptionTtCode(pos.ProductId, optionType, pos.MaturityDate, pos.Strike.Value);
            // get or suscribe option price
            double targetOptionPrice;
            if (!_instrumentPriceSafeDico.TryGetValue(targetOptionTtCode, out targetOptionPrice))
            {
                _marketFeed.SuscribeToInstrumentPrice(targetOptionTtCode, pos.Product.ProductType, pos.ProductId, pos.Product.Market, "Mid");
                _instrumentPriceSafeDico.TryAdd(targetOptionTtCode, 0);
            }

            return targetOptionPrice;
        }

        private void LoadData(object obj)
        {
            if(string.IsNullOrEmpty(SelectedProduct) || SelectedMaturity == DateTime.MinValue)
            {
                MessageBox.Show("Select product and maturity");
                return;
            }

            //Task impliedVolTask = new Task(LoadImpliedVol);
            //impliedVolTask.Start();
            //impliedVolTask.Wait();

            LoadImpliedVol(true);
            LoadModelVol(true);

            //Task modelVolTask = new Task(LoadModelVol);
            //modelVolTask.Start();
            //modelVolTask.Wait();
        }

        private void LoadImpliedVol(bool reloadData)
        {
            if (reloadData)
            {
                Product underlying = _dbManager.GetUnderlying(SelectedProduct);
                string futureId = Option.GetNextFutureTtCode(underlying.Id, SelectedMaturity);

                // suscribe to future price feed
                if (!_instrumentPriceSafeDico.ContainsKey(futureId))
                {
                    _marketFeed.SuscribeToInstrumentPrice(futureId, underlying.ProductType, underlying.Id, underlying.Market, "Mid");
                    _instrumentPriceSafeDico.TryAdd(futureId, 0);
                }

                _options = _dbManager.GetCallOptions(SelectedProduct, SelectedMaturity);

            }

            ImpliedVolPoints.Clear();
            foreach (var data in _options)
            {
                
                if(data.Strike.Value < SelectedMinStrike || data.Strike.Value > SelectedMaxStrike)
                    continue;

                double forwardPrice = GetForwardPrice(data);

                if (forwardPrice != 0)
                {
                    // Compute implied volatility
                    string targetOptionType = GetOtmOptionType(data.Strike.Value, forwardPrice);
                    double targetOptionPrice = GetOutTheMoneyOptionPrice(data, targetOptionType);
                    double timeLeft = Option.GetTimeToExpiration(DateTime.Today, data.MaturityDate);
                    double volatility = Option.BlackScholesVol(targetOptionPrice, forwardPrice, data.Strike.Value, targetOptionType, timeLeft);

                    if (volatility != 0)
                    {
                        ImpliedVolPoints.Add(new Point(data.Strike.Value, volatility));
                    }
                }
            }
        }

        private void LoadModelVol(bool reloadData)
        {
            if (reloadData)
            {
                // retrieve fit params form db
                Param = _dbManager.GetVolParams(SelectedProduct, SelectedMaturity);
                //   Dispatcher.CurrentDispatcher.Invoke(() => ModelVolPoints.Clear());
            }

            ModelVolPoints.Clear();

            foreach (var data in _options)
            {
                if (data.Strike.Value < SelectedMinStrike || data.Strike.Value > SelectedMaxStrike)
                    continue;

                double forwardPrice = GetForwardPrice(data);
                
                if (forwardPrice != 0)
                {
                    double modelVol = Option.SviVolatility(data.Strike.Value, forwardPrice, Param.A, Param.B, Param.Sigma, Param.Rho, Param.M);
              //      Dispatcher.CurrentDispatcher.Invoke(() => ModelVolPoints.Add(new Point(data.Strike.Value, modelVol)));
                    ModelVolPoints.Add(new Point(data.Strike.Value, modelVol));
                }
            }
        }

        private void FitCurve(object obj)
        {

            string futureId = Option.GetNextFutureTtCode("FESX", SelectedMaturity);
            string forwardId = Option.BuildForwardId("STXE", SelectedMaturity);

            double forward;
            if (_instrumentPriceSafeDico.TryGetValue(forwardId, out forward))
            {
                UpdateForwardPrices(forwardId, futureId);
            }

            double[,] data = new double[ImpliedVolPoints.Count, 2];

            for (int i = 0; i < ImpliedVolPoints.Count; i++)
            {
                data[i, 0] = ImpliedVolPoints[i].X;
                data[i, 1] = ImpliedVolPoints[i].Y;
            }

            Parameter[] outParams = SVI.Fit(data, forward);

            Param.A = Math.Round(outParams[0].Value, 4);
            Param.B = Math.Round(outParams[1].Value, 4);
            Param.Rho = Math.Round(outParams[2].Value, 4);
            Param.Sigma = Math.Round(outParams[3].Value, 4);
            Param.M = Math.Round(outParams[4].Value, 4);
        }


        private double GetForwardPrice(Instrument data)
        {
            double forwardPrice;
            if (_instrumentPriceSafeDico.TryGetValue(data.RefForwardId, out forwardPrice))
            {
                UpdateForwardPrices(data.RefForwardId, data.RefFutureId);
                return forwardPrice;
            }
            else
            {
                // if new forward: compute base offset
                DateTime previousDay = Option.PreviousWeekDay(DateTime.Today);
                double futureClose = _dbManager.GetSpotClose(previousDay, data.RefFutureId);
                double forwardClose = Option.GetForwardClose(data.RefForwardId, data.MaturityDate, futureClose);
                double forwardBaseOffset = forwardClose - futureClose;
                _fwdBaseOffsetDico.Add(data.RefForwardId, forwardBaseOffset);
                _instrumentPriceSafeDico.TryAdd(data.RefForwardId, 0);
                return 0;
            }
        }

        // compute forward price based on future price and base offset
        private void UpdateForwardPrices(string forwardId, string futureId)
        {
            double spotPrice;
            if (_instrumentPriceSafeDico.TryGetValue(futureId, out spotPrice) && spotPrice != 0)
            {
                double baseofset;
                if (_fwdBaseOffsetDico.TryGetValue(forwardId, out baseofset) && baseofset != -1 && spotPrice != 0)
                {
                    _instrumentPriceSafeDico[forwardId] = spotPrice + baseofset;
                }
            }
        }

        private void InsertParamToDb(object obj)
        {
            _dbManager.UpdateVolParams(Param);
        }

        private void ExecuteBeforeClose(object o)
        {
            Task taskA = new Task(() => _marketFeed.Dispose());
            taskA.Start();

            while (marketConnectionUp)
            {
                Thread.Sleep(1000);
            }

            log.Info("VolManager closed");
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
