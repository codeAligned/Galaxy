using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DealManager.Commands;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm;
using Galaxy.DatabaseService;
using Galaxy.DealManager.Model;
using Galaxy.DealManager.View;
using Galaxy.MarketFeedService;
using Galaxy.PricingService;
using log4net;

namespace Galaxy.DealManager.ViewModel
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class DealManagerVM : INotifyPropertyChanged
    {
        #region Parameters

        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _ttlogin = ConfigurationManager.AppSettings["Login"];
        private readonly string _ttPassword = ConfigurationManager.AppSettings["PassWord"];

        private readonly IMarketFeed _marketFeed;
        private readonly IDbManager _dbManager;

        public Deal _selectedDeal;

        private readonly Dictionary<string, ObsInstruPosition> _currentPosDico;
        private readonly Dictionary<string, ObsInstruPosition> _expiredPosDico;
        private readonly Dictionary<string, ObsBookPosition> _bookPositionDico;
        private readonly Dictionary<string, VolParam> _volParamDico;
        private readonly Dictionary<string, double> _fwdBaseOffsetDico;
        private readonly ConcurrentDictionary<string, double> _instrumentPriceSafeDico;
        private Deal[] _deals;
        private string _productType = "OPTION";
        private DateTime _maturity;
        private const double _basisPoint = 0.001;

        private bool marketConnectionUp = false;

        #endregion

        #region Assessors

        public ICommand OpenDealFormCmd { get;}
        public ICommand InsertDealCmd { get;  }
        public ICommand UpdateDealCmd { get; }
        public ICommand RemoveDealCmd { get; }
        public ICommand RowDoubleClickCmd { get; }
        public ICommand InstruViewClickCmd { get; }
        public ICommand BeforeCloseCommand { get; }
        public ICommand WindowsLoadedCmd { get; }
        public ICommand OpenSettleDealCmd { get; }

        public Action CloseAction { get; set; }
        public ObservableCollection<string> InstrumentNames { get; set; }
        private Instrument[] _instruments;
        
        public string[] Users { get; set; }
        public string[] Books { get; } = { "VS Equity", "VS FX" };
        public string[] Brokers { get; } = { "Aurel BGC", "Platform" };
        public string[] Status { get; } = {"Front Booking", "Expired"};
        public ObservableCollection<DateTime> Maturitys { get; set; }
        public ObservableCollection<Deal> ObsDeals { get; set; }
        public ObservableCollection<ObsInstruPosition> ObsPositions { get; set; }
        public ObservableCollection<ObsBookPosition> ObsBookPosition { get; set; }

        public Deal SelectedDeal
        {
            get { return _selectedDeal; }
            set { _selectedDeal = value; OnPropertyChanged(nameof(SelectedDeal)); }
        }

        public ObsInstruPosition SelectedObsInstruPosition { get; set; }

        public string ProductType
        {
            get { return _productType; }
            set { _productType = value;
                FilterInstrumentList();
            }
        }

        public DateTime Maturity
        {
            get { return _maturity; }
            set { _maturity = value; FilterInstrumentList(); }
        }

        #endregion
        
        public DealManagerVM(IMarketFeed marketFeed, IDbManager dbManager)
        {
            _marketFeed = marketFeed;
            _dbManager = dbManager;

            OpenDealFormCmd = new RelayCommand(OpenDealForm);
            InsertDealCmd = new RelayCommand(InsertDealIntoDb);
            UpdateDealCmd = new RelayCommand(UpdateDeal);
            RemoveDealCmd = new RelayCommand(RemoveDeal);
            BeforeCloseCommand = new RelayCommand(ExecuteBeforeClose);
            WindowsLoadedCmd = new RelayCommand(WindowLoaded);
            OpenSettleDealCmd = new RelayCommand(OpenSettleDealForm);

            ObsDeals = new ObservableCollection<Deal>();
            ObsPositions = new ObservableCollection<ObsInstruPosition>();
            ObsBookPosition = new ObservableCollection<ObsBookPosition>();
            InstrumentNames = new ObservableCollection<string>();
            Maturitys = new ObservableCollection<DateTime>();
            _currentPosDico = new Dictionary<string, ObsInstruPosition>();
            _expiredPosDico = new Dictionary<string, ObsInstruPosition>();
            _bookPositionDico = new Dictionary<string, ObsBookPosition>();
            _volParamDico = new Dictionary<string, VolParam>();
            _fwdBaseOffsetDico = new Dictionary<string, double>();

            _instrumentPriceSafeDico = new ConcurrentDictionary<string, double>();
            RowDoubleClickCmd = new DelegateCommand<object>(DisplayUpdateRmFormWin);
            InstruViewClickCmd = new DelegateCommand<object>(DisplayDealView); 
        }

        private void WindowLoaded(object o)
        {
            log.Info("DealManager started");

            //Check Database connection
            if (!_dbManager.TestConnection())
            {
                MessageBox.Show("Enable to access to database. Check VPN connection");
                return;
            }

            // Check if closing snapshot batch as run
            if (!_dbManager.TestClosePrice(Option.PreviousWeekDay(DateTime.Today)))
            {
                MessageBox.Show("Close price missing. Check database insertion");
                return;
            }

            _marketFeed.Connect(_ttlogin,_ttPassword, ConnectionStatusHandler, PriceUpdateHandler,null);
            InitializeFastTimer();
            InitializeSlowTimer();

            LoadExpiredDeals();
            LoadCurrentDeals();
            LoadVolParams();
        }

        public static void BuildMetadata(MetadataBuilder<DealManagerVM> builder)
        {
          //  builder.Property(x => x.Broker).Required(() => "Please enter an instrument.");
            //PriceCheck(builder.Property(x => x.Price)).Required(() => "Please enter the price.");
            //builder.Property(x => x.Broker).Required(() => "Please enter the last name.");
        }

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

        // Call back from TT API for instrument price update
        private void PriceUpdateHandler(IMarketFeed sender, string instrumentName, double midPrice)
        {
            _instrumentPriceSafeDico[instrumentName] = midPrice;
        }

        // for market data
        private void InitializeFastTimer()
        {
            var fastTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            fastTimer.Tick += RefreshRtData;
            fastTimer.Start();
        }

        // for database query
        private void InitializeSlowTimer()
        {
            var slowTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(30)};
            slowTimer.Tick += RefreshAllData;
            slowTimer.Start();
        }

        public void OpenDealForm(object obj)
        {
            _instruments = _dbManager.GetAllInstruments(DateTime.Today);
            FilterInstrumentList();

            
            Users = _dbManager.GetAllFrontUserIds();
            SelectedDeal = new Deal {Status = "Front Booking", TradeDate = DateTime.Now};
            var bookingAddForm = new AddFormWin {DataContext = this};
            bookingAddForm.Show();
            CloseAction = bookingAddForm.Close;
        }

        private void FilterInstrumentList()
        {
            DateTime[] instruMaturitys = (from i in _instruments where i.Product.ProductType == ProductType select i.MaturityDate).Distinct().ToArray();
            
            Maturitys.Clear();
            foreach (var matu in instruMaturitys)
            {
                Maturitys.Add(matu);
            }

            if (Maturity == DateTime.MinValue && instruMaturitys.Count() != 0)
            {
                Maturity = instruMaturitys[0];
            }

            string[] instruNames = (from i in _instruments where i.Product.ProductType == ProductType && i.MaturityDate == Maturity select i.Id).ToArray();
            InstrumentNames.Clear();
            foreach (var instru in instruNames)
            {
                InstrumentNames.Add(instru);
            }
        }

        public void OpenSettleDealForm(object obj)
        {
            var query = from b in ObsPositions
                          where b.Expiry == 0 && b.Quantity != 0
                          select b.InstruRic;

            string[]instruList = query.ToArray();

            InstrumentNames.Clear();
            foreach (var instru in instruList)
            {
                InstrumentNames.Add(instru);
            }

  
            Users = _dbManager.GetAllFrontUserIds();
            SelectedDeal = new Deal { Status = "Front Booking", TradeDate = DateTime.Now, Comment = "Settlement Deal"};
            var settleDealWin = new CloseDealWin { DataContext = this };
            settleDealWin.Show();
            CloseAction = settleDealWin.Close;
        }

        public void DisplayUpdateRmFormWin(object obj)
        {
            SelectedDeal = null;
            SelectedDeal = (Deal) obj;

            if(SelectedDeal == null)
                return;

            var query = from b in ObsPositions
                        select b.InstruRic;

            string[] instruList = query.ToArray();

            InstrumentNames.Clear();
            foreach (var instru in instruList)
            {
                InstrumentNames.Add(instru);
            }

            Users = _dbManager.GetAllFrontUserIds();

            var bookingUpdateForm = new UpdateRmFormWin { DataContext = this };
            bookingUpdateForm.Show();
            CloseAction = bookingUpdateForm.Close;
        }

        private void DisplayDealView (object obj)
        {
            SelectedObsInstruPosition = (ObsInstruPosition)obj;

            if (SelectedObsInstruPosition == null)
                return;

            ObsDeals.Clear();

            foreach (var deal in _deals)
            {
                if (deal?.InstrumentId == SelectedObsInstruPosition?.InstruRic)
                {
                   // ObsDeals.Add(new Deal(deal));
                    ObsDeals.Add(deal);
                }
            }

            var positionDetails = new PositionDetailsWin { DataContext = this };
            positionDetails.Show();
        }

        public void InsertDealIntoDb(object obj)
        {
            if (!CheckDealData())
            {
                return;
            }

            SelectedDeal.TradeDate = DateTime.Now;
            _dbManager.AddDeal(SelectedDeal);

            CloseAction();
            LoadCurrentDeals();
        }

        private bool CheckDealData()
        {
            if (string.IsNullOrEmpty(SelectedDeal.InstrumentId))
            {
                MessageBox.Show("Invalid Instrument");
                return false;
            }

            if (SelectedDeal.Quantity == 0)
            {
                MessageBox.Show("Invalid Quantity");
                return false;
            }

            if (SelectedDeal.ExecPrice == 0 && SelectedDeal.Comment != "Settlement Deal")
            {
                MessageBox.Show("Invalid Price");
                return false;
            }

            if (string.IsNullOrEmpty(SelectedDeal.TraderId))
            {
                MessageBox.Show("Invalid TraderId");
                return false;
            }

            if (string.IsNullOrEmpty(SelectedDeal.BookId))
            {
                MessageBox.Show("Invalid BookId");
                return false;
            }

            if (string.IsNullOrEmpty(SelectedDeal.Broker))
            {
                MessageBox.Show("Invalid Broker");
                return false;
            }
            return true;
        }

        public void UpdateDeal(object obj)
        {
            if (!CheckDealData())
            {
                return;
            }

            _dbManager.UpdateDeal(SelectedDeal);
            CloseAction();
            LoadCurrentDeals();
        }

        public void RemoveDeal(object obj)
        {
            _dbManager.RemoveDeal(SelectedDeal.DealId);
            CloseAction();
            _currentPosDico.Clear();
            ObsPositions.Clear();
            ObsDeals.Clear();
            LoadCurrentDeals();

            foreach (var deal in _deals)
            {
                if (deal?.InstrumentId == SelectedObsInstruPosition?.InstruRic)
                {
                    //ObsDeals.Add(new ObsDeal(deal));
                    ObsDeals.Add(deal);
                }
            }
        }

        private void RefreshAllData(object sender, EventArgs e)
        {
            LoadCurrentDeals();
            LoadVolParams();
        }

        private void RefreshRtData(object sender, EventArgs e)
        {
            if (marketConnectionUp)
            {
                ComputeInstrumentData();
                ComputeBookRisk();
            }
        }

        private void LoadVolParams()
        {
            VolParam[] volParams = _dbManager.GetAllVolParams(DateTime.Today);

            _volParamDico.Clear();
            foreach ( var param in volParams)
            {
                string key = param.ProductId + "_" + param.MaturityDate.ToString("MMyyyy");

                if (!_volParamDico.ContainsKey(key))
                {
                    _volParamDico.Add(key, param);
                }
            }
        }

        private void LoadExpiredDeals()
        {
            Deal[] expiredDeals = _dbManager.GetAllExpiredDeals();
            foreach (var deal in expiredDeals)
            {
                ObsInstruPosition pos;
                if (!_expiredPosDico.TryGetValue(deal.InstrumentId, out pos))
                {
                    _expiredPosDico.Add(deal.InstrumentId, new ObsInstruPosition(deal));
                    continue;
                }
                Pnl.ComputeInstrumentPosition(pos, deal);
            }
        }

        public void LoadCurrentDeals()
        {
            _deals = _dbManager.GetAllDeals();

            ResetInstrumentPosition();
            foreach (var deal in _deals)
            {
                ObsInstruPosition pos;
                if (!_currentPosDico.TryGetValue(deal.InstrumentId, out pos))
                {
                    var newPos = new ObsInstruPosition(deal);
                    _currentPosDico.Add(deal.InstrumentId, newPos);

                    ObsPositions.Add(newPos);
                    continue;
                }

                Pnl.ComputeInstrumentPosition(pos,deal);
            }

            ResetBookPosition();
            foreach (ObsInstruPosition instruPos in _currentPosDico.Values)
            {
                ObsBookPosition pos;
                if (!_bookPositionDico.TryGetValue(instruPos.Book, out pos))
                {
                    var newPos = new ObsBookPosition(instruPos);
                    _bookPositionDico.Add(instruPos.Book, newPos);
                    ObsBookPosition.Add(newPos);
                    return;
                }
                if (instruPos.InstruType != "FUTURE")
                {
                    Pnl.ComputeBookPosition(pos, instruPos);
                }
            }

            foreach (ObsInstruPosition instruPos in _expiredPosDico.Values)
            {
                ObsBookPosition pos;
                if (!_bookPositionDico.TryGetValue(instruPos.Book, out pos))
                {
                    var newPos = new ObsBookPosition(instruPos);
                    _bookPositionDico.Add(instruPos.Book, newPos);
                    ObsBookPosition.Add(newPos);
                    return;
                }
                Pnl.ComputeBookPosition(pos,instruPos);
            }
        }

        private void ResetInstrumentPosition()
        {
            foreach (ObsInstruPosition pos in _currentPosDico.Values)
            {
                pos.ResetData();   
            }
        }

        private void ResetBookPosition()
        {
            foreach (ObsBookPosition pos in _bookPositionDico.Values)
            {
                pos.ResetData();
            }
        }

        private void ResetBookGreeks()
        {
            foreach (ObsBookPosition pos in _bookPositionDico.Values)
            {
                pos.ResetGreeks();
            }
        }

        private void ComputeInstrumentData()
        {
            foreach (ObsInstruPosition pos in _currentPosDico.Values)
            {
                // retreive option price
                double instruPrice;
                if (_instrumentPriceSafeDico.TryGetValue(pos.TtInstruId, out instruPrice) && instruPrice != 0)
                {
                    pos.MtmPrice = instruPrice;
                }
                else
                {
                    _marketFeed.SuscribeToInstrumentPrice(pos.TtInstruId, pos.InstruType, pos.ProductName, pos.Market, "Mid");
                    _instrumentPriceSafeDico.TryAdd(pos.TtInstruId, 0);
                }

                if (pos.InstruType == "FUTURE")
                {
                    pos.ForwardPrice = instruPrice;
                    pos.FairPrice = instruPrice;
                    pos.Delta = pos.Quantity;
                    pos.StickyDelta = pos.Quantity;
                }
                else
                {
                    //retreive forward price
                  
                    if (_instrumentPriceSafeDico.ContainsKey(pos.ForwardId))
                    {
                        UpdateForwardPrices(pos.ForwardId, pos.FutureId);
                        pos.ForwardPrice = _instrumentPriceSafeDico[pos.ForwardId];
                    }
                    else
                    {
                        //check if instrument is expired and if Forward as been created correctly
                        if (pos.MaturityDate >= DateTime.Today)
                        {
                            DateTime previousDay = Option.PreviousWeekDay(DateTime.Today);
                            double futureClose = _dbManager.GetSpotClose(previousDay, pos.FutureId);
                            double forwardClose = Option.GetForwardClose(pos.ForwardId, pos.MaturityDate, futureClose);
                            double forwardBaseOffset = forwardClose - futureClose;
                            _fwdBaseOffsetDico.Add(pos.ForwardId, forwardBaseOffset);
                            _instrumentPriceSafeDico.TryAdd(pos.ForwardId, 0);
                        }
                        continue;
                    }

                    //retreive vol param
                    VolParam param;
                    if (!_volParamDico.TryGetValue(pos.VolParamsId, out param))
                    {
                        continue;
                    }

                    if (pos.ForwardPrice != 0)
                    {
                        // Target option is the option out of the money  
                        // strike > spot = Call ; strike < spot = put
                        string targetOptionType = GetOtmOptionType(pos.Strike, pos.ForwardPrice);
                        double targetOptionPrice = GetOutTheMoneyOptionPrice(pos, targetOptionType);
                        double time = Option.GetTimeToExpiration(DateTime.Today, pos.MaturityDate);
                        pos.ImpliedVol = Option.BlackScholesVol(targetOptionPrice, pos.ForwardPrice, pos.Strike, targetOptionType, time);
                        pos.ModelVol = Option.SviVolatility(pos.Strike, pos.ForwardPrice, param.A,param.B,param.Sigma,param.Rho,param.M,time);
                        pos.FairPrice = Option.BlackScholes(pos.OptionType, pos.ForwardPrice, pos.Strike, time,pos.ModelVol);
                        pos.Delta = pos.Quantity * Option.Delta(pos.OptionType, pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.StickyDelta = Option.DegueulasseDelta(pos.OptionType, pos.ForwardPrice, pos.ModelVol,pos.Strike, time, pos.Quantity, param.A, param.B, param.Sigma, param.Rho, param.M);
                        pos.Gamma = (pos.Quantity * Math.Pow(pos.ForwardPrice/100, 2) * Option.Gamma(pos.ForwardPrice, pos.Strike, pos.ModelVol, time)) / _basisPoint;
                        pos.Vega = pos.Quantity * _basisPoint * Option.Vega(pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Theta = pos.Quantity * pos.LotSize * Option.Theta(pos.OptionType, pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Rho = Option.Rho(pos.OptionType, pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Vanna = Option.Vanna(pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Vomma = Option.Vomma(pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Charm = Option.Charm(pos.OptionType, pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Veta = Option.Veta(pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Color = (pos.Quantity * Math.Pow(pos.ForwardPrice / 100, 2) * Option.Color(pos.ForwardPrice, pos.Strike, pos.ModelVol, time)) / _basisPoint;
                        pos.Ultima = Option.Ultima(pos.ForwardPrice, pos.Strike, pos.ModelVol, time);
                        pos.Speed = (pos.Quantity * Math.Pow(pos.ForwardPrice / 100, 2) * Option.Speed(pos.ForwardPrice, pos.Strike, pos.ModelVol, time)) / _basisPoint;
                    }
                }
            }
        }

        // For a specific strike and maturity, we use the option(call or put) which is out of the money
        // this methode is in charge to retreive the current option ric out of the money 
        private double GetOutTheMoneyOptionPrice(ObsInstruPosition pos, string optionType)
        {
            // build option tt code
            string targetOptionTtCode = Option.GetOptionTtCode(pos.ProductName, optionType, pos.MaturityDate, pos.Strike);
            // get or suscribe option price
            double targetOptionPrice;
            if (!_instrumentPriceSafeDico.TryGetValue(targetOptionTtCode, out targetOptionPrice))
            {
                _marketFeed.SuscribeToInstrumentPrice(targetOptionTtCode, pos.InstruType, pos.ProductName, pos.Market, "Mid");
                _instrumentPriceSafeDico.TryAdd(targetOptionTtCode, 0);
            }

            return targetOptionPrice;
        }

        private void ComputeBookRisk()
        {
            ResetBookGreeks();
            foreach (ObsInstruPosition instruPos in _currentPosDico.Values)
            {
                ObsBookPosition pos;
                if (!_bookPositionDico.TryGetValue(instruPos.Book, out pos))
                {
                    //var newPos = new ObsBookPosition(instruPos);
                    //_bookPositionDico.Add(instruPos.Book, newPos);
                    //ObsBookPosition.Add(newPos);
                    return;
                }

                Pnl.ComputeBookRisk(pos, instruPos);
            }
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
                return  "PUT";  //Target price: Put OTM
            }
            return  "CALL";     //Target price: Call OTM
        }

        private void UpdateForwardPrices(string forwardId, string futureId)
        {
            double spotPrice;
            if(_instrumentPriceSafeDico.TryGetValue(futureId, out spotPrice))
            {
                double baseofset; //= _optionLib.UniqueInstance.GetForwardBaseOffset(forwardId);
                if(_fwdBaseOffsetDico.TryGetValue(forwardId, out baseofset) && baseofset != -1 && spotPrice != 0)
                {
                    _instrumentPriceSafeDico[forwardId] = spotPrice + baseofset;
                }
            }
            else
            {
                Instrument future = _dbManager.GetFuture(futureId);
                var instruType = future.Product.ProductType;
                var productName = future.Product.Id;
                var market = future.Product.Market;
                var ttCode = future.TtCode;
                _marketFeed.SuscribeToInstrumentPrice(ttCode, instruType, productName, market, "Mid");
                _instrumentPriceSafeDico.TryAdd(ttCode, 0);
            }
        }

        private void ExecuteBeforeClose(object o)
        {
            Task taskA = new Task(() => _marketFeed.Dispose());
            taskA.Start();

            while (marketConnectionUp)
            {
                Thread.Sleep(1000);
            }

            log.Info("DealManager closed");
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