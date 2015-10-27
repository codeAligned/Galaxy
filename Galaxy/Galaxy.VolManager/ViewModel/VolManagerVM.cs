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
        public DateTime[] AvailableMaturity { get; }

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
            ImpliedVolData = new List<VolatilityData>();
            _strikeList = new List<double>();
            _instrumentPriceSafeDico = new ConcurrentDictionary<string, double>();
            _fwdBaseOffsetDico = new Dictionary<string, double>();

            _marketFeed = marketFeed;
            _dbManager = dbManager;

            SelectedMinStrike = 2500;
            SelectedMaxStrike = 4000;

            AvailableMaturity = _dbManager.GetAvailableMaturity(DateTime.Today);
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
            string futureId = Option.GetNextFutureTtCode("FESX",SelectedMaturity);
            string forwardId = Option.BuildForwardId("STXE",SelectedMaturity);

            double forward;
            if (_instrumentPriceSafeDico.TryGetValue(forwardId, out forward))
            {
                UpdateForwardPrices(forwardId, futureId);
            }
            
            double timeToExpi = Option.GetTimeToExpiration(DateTime.Today, SelectedMaturity);

            var x = new double[ImpliedVolPoints.Count];
            var y = new double[ImpliedVolPoints.Count];

            for (int i = 0; i < ImpliedVolPoints.Count; i++)
            {
                x[i] = ImpliedVolPoints[i].X;
                y[i] = ImpliedVolPoints[i].Y;
            }

            double[] p = { 1, 1, 1, 1, 1, forward, timeToExpi };

            // Parameter constraints
            var param = new[] {     new LmParams(), 
                                    new LmParams(),
                                    new LmParams(),
                                    new LmParams(),
                                    new LmParams(),
                                    new LmParams {isFixed = 1},  // Fix parameter 1
                                    new LmParams {isFixed = 1}   // Fix parameter 1
                                };

            var result = new LmResult(p.Length);
            const double error = 0.07;
            var ey = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ey[i] = error;
            }

            var v = new CustomUserVariable { X = x, Y = y, Ey = ey };
            LmAlgo.Solve(FunctionModel.GatheralFunc, x.Length, p.Length, p, param, null, v, ref result);

            Param.A = Math.Round(p[0],4);
            Param.B = Math.Round(p[1], 4);
            Param.Sigma = Math.Round(p[2], 4);
            Param.Rho = Math.Round(p[3], 4);
            Param.M = Math.Round(p[4], 4);
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
                else
                {
                    DateTime previousDay = Option.PreviousWeekDay(DateTime.Today);
                    double futureClose = _dbManager.GetSpotClose(previousDay, data.FutureName);
                    double forwardClose = Option.GetForwardClose(data.ForwardName, data.Maturity, futureClose);
                    double forwardBaseOffset = forwardClose - futureClose;
                    _fwdBaseOffsetDico.Add(data.ForwardName, forwardBaseOffset);
                    _instrumentPriceSafeDico.TryAdd(data.ForwardName, 0);
                    continue;
                }

                if (forwardPrice != 0)
                {
                    double time = Option.GetTimeToExpiration(DateTime.Today, data.Maturity);
                    double modelVol = Option.SviVolatility(data.Strike, forwardPrice, Param.A, Param.B, Param.Sigma, Param.Rho,Param.M, time);

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
