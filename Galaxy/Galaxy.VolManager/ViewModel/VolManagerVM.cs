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
using Galaxy.VolManager.Model;
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

        private readonly ConcurrentDictionary<string, double> _instrumentPriceSafeDico;
        private readonly Dictionary<string, double> _fwdBaseOffsetDico;

        private readonly IMarketFeed _marketFeed;
        private readonly IDbManager _dbManager;
        private string _marketFeedStatus = "DOWN";

        private bool marketConnectionUp = false;
        public ObservableCollection<DateTime> AvailableMaturity { get; set; }

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


        public List<VolatilityData> ImpliedVolData { get; }
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

            LoadVolCmd = new RelayCommand(LoadImpliedVolData);
            FitCurveCmd = new  RelayCommand(FitCurve);
            WindowsLoadedCmd = new RelayCommand(WindowLoaded);
            InsertNewParamsCmd = new RelayCommand(InsertParamToDb);
            BeforeCloseCmd = new RelayCommand(ExecuteBeforeClose);

            ImpliedVolPoints = new ObservableCollection<Point>();
            ModelVolPoints = new ObservableCollection<Point>();
            AvailableMaturity = new ObservableCollection<DateTime>();
            ImpliedVolData = new List<VolatilityData>();
            _strikeList = new List<double>();
            _instrumentPriceSafeDico = new ConcurrentDictionary<string, double>();
            _fwdBaseOffsetDico = new Dictionary<string, double>();

            _marketFeed = marketFeed;
            _dbManager = dbManager;

            SelectedMinStrike = 2000;
            SelectedMaxStrike = 5000;
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
            DispatcherTimer fastTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            fastTimer.Tick += RefreshRtData;
            fastTimer.Start();
        }

        private void RefreshRtData(object sender, EventArgs e)
        {
            if(ImpliedVolData.Count == 0 || !marketConnectionUp)
            {
                return;
            }

            foreach (VolatilityData option in ImpliedVolData)
            {
                // if forward already computed
                double forwardPrice;
                if (_instrumentPriceSafeDico.TryGetValue(option.ForwardName, out forwardPrice))
                {
                    UpdateForwardPrices(option.ForwardName, option.FutureName);
                }
                else
                {
                // if new forward: compute base offset
                    DateTime previousDay = Option.PreviousWeekDay(DateTime.Today);
                    double futureClose = _dbManager.GetSpotClose(previousDay, option.FutureName);
                    double forwardClose = Option.GetForwardClose(option.ForwardName, option.Maturity, futureClose);
                    double forwardBaseOffset = forwardClose - futureClose;
                    _fwdBaseOffsetDico.Add(option.ForwardName, forwardBaseOffset);
                    _instrumentPriceSafeDico.TryAdd(option.ForwardName, 0);
                    continue;
                }
                
                if (forwardPrice != 0)
                {
                // Compute implied volatility
                    string targetOptionType = GetOtmOptionType(option.Strike, forwardPrice);
                    double targetOptionPrice = GetOutTheMoneyOptionPrice(option, targetOptionType);
                    double timeLeft = Option.GetTimeToExpiration(DateTime.Today, option.Maturity);
                    option.Volatility = Option.BlackScholesVol(targetOptionPrice, forwardPrice, option.Strike, targetOptionType, timeLeft);
                }
            }

            //Refresh observable collection of points
            ImpliedVolPoints.Clear();
            foreach (VolatilityData val in ImpliedVolData)
            {
                if (val.Volatility != 0)
                {
                    ImpliedVolPoints.Add(new Point(val.Strike, val.Volatility));
                }
            }
            DisplayModelVol();
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
        private double GetOutTheMoneyOptionPrice(VolatilityData pos, string optionType)
        {
            // build option tt code
            string targetOptionTtCode = Option.GetOptionTtCode(pos.ProductName, optionType, pos.Maturity, pos.Strike);
            // get or suscribe option price
            double targetOptionPrice;
            if (!_instrumentPriceSafeDico.TryGetValue(targetOptionTtCode, out targetOptionPrice))
            {
                _marketFeed.SuscribeToInstrumentPrice(targetOptionTtCode, pos.ProductType, pos.ProductName, pos.Market, "Mid");
                _instrumentPriceSafeDico.TryAdd(targetOptionTtCode, 0);
            }

            return targetOptionPrice;
        }

        // compute forward price based on future price and base offset
        private void UpdateForwardPrices(string forwardRic, string futureTtCode)
        {
            double spotPrice;
            if (_instrumentPriceSafeDico.TryGetValue(futureTtCode, out spotPrice) && spotPrice != 0)
            {
                double baseofset;
                if (_fwdBaseOffsetDico.TryGetValue(forwardRic, out baseofset) && baseofset != -1 && spotPrice != 0)
                {
                    _instrumentPriceSafeDico[forwardRic] = spotPrice + baseofset;
                }
            }
        }

        private void LoadImpliedVolData(object obj)
        {
            if(string.IsNullOrEmpty(SelectedProduct) || SelectedMaturity == DateTime.MinValue)
            {
                MessageBox.Show("Select product and maturity");
                return;
            }

            string futureId = Option.GetNextFutureTtCode("FESX", SelectedMaturity);

            _marketFeed.SuscribeToInstrumentPrice(futureId, "FUTURE", "FESX", "Eurex","Mid");
            _instrumentPriceSafeDico.TryAdd(futureId, 0);

            Param = _dbManager.GetVolParams(SelectedProduct, SelectedMaturity);

            Instrument[] options = _dbManager.GetCallOptions(SelectedProduct, SelectedMaturity);

            ImpliedVolData.Clear();
            foreach (var option in options)
            {
                DateTime maturity = option.MaturityDate;
                string productName = option.ProductId;
                int strike = option.Strike ?? 0;
                string  productType = option.Product.ProductType;
                string market = option.Product.Market;
                string exerciseType = option.Product.ExerciseType;
                string underlyingName = option.RefForwardId;
                string futureName = option.RefFutureId; 

                if(strike >= SelectedMinStrike && strike<= SelectedMaxStrike)
                {
                    ImpliedVolData.Add(new VolatilityData(strike, maturity, productType, productName, market, exerciseType, underlyingName, futureName));
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

        private void DisplayModelVol()
        {
            ModelVolPoints.Clear();
            foreach (var data in ImpliedVolData)
            {
                double forwardPrice;
                if (_instrumentPriceSafeDico.TryGetValue(data.ForwardName, out forwardPrice))
                {
                    UpdateForwardPrices(data.ForwardName, data.FutureName);
                }

                if (forwardPrice != 0)
                {
              //      double time = Option.GetTimeToExpiration(DateTime.Today, data.Maturity);
                    double modelVol = Option.SviVolatility(data.Strike, forwardPrice, Param.A, Param.B, Param.Sigma, Param.Rho, Param.M);

                    ModelVolPoints.Add(new Point(data.Strike, modelVol));
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
