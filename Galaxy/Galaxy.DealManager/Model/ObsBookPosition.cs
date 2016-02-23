using System.ComponentModel;
using Galaxy.PricingService;

namespace Galaxy.DealManager.Model
{
    public class ObsBookPosition : BookPosition,  INotifyPropertyChanged
    {
        public override double Cash
        {
            get { return _cash; }
            set { _cash = value; OnPropertyChanged(nameof(Cash)); }
        }

        public override string Book
        {
            get { return _book; }
            set { _book = value; OnPropertyChanged(nameof(Book)); }
        }

        public override double RealisedPnl
        {
            get { return _realisedPnl; }
            set { _realisedPnl = value; OnPropertyChanged(nameof(RealisedPnl)); }
        }

        public override double UnrealisedPnl
        {
            get { return _unrealisedPnl; }
            set { _unrealisedPnl = value; OnPropertyChanged(nameof(UnrealisedPnl)); }
        }

        public override double FairUnrealisedPnl
        {
            get { return _fairUnrealisedPnl; }
            set { _fairUnrealisedPnl = value; OnPropertyChanged(nameof(FairUnrealisedPnl)); }
        }

        public override double Delta
        {
            get { return _delta; }
            set { _delta = value; OnPropertyChanged(nameof(_delta)); }
        }

        public override double StickyDelta
        {
            get { return _stickyDelta; }
            set { _stickyDelta = value; OnPropertyChanged(nameof(_stickyDelta)); }
        }

        public override double Gamma
        {
            get { return _gamma; }
            set { _gamma = value; OnPropertyChanged(nameof(_gamma)); }
        }

        public override double Vega
        {
            get { return _vega; }
            set { _vega = value; OnPropertyChanged(nameof(_vega)); }
        }

        public override double Theta
        {
            get { return _theta; }
            set { _theta = value; OnPropertyChanged(nameof(_theta)); }
        }

        public ObsBookPosition(ObsInstruPosition pos) : base(pos) {}

        public void ResetData()
        {
            UnrealisedPnl = 0;
            RealisedPnl = 0;
            YtdRealisedPnl = 0;
            Cash = 0;
            FairUnrealisedPnl = 0;
        }

        public void ResetGreeks()
        {
            Delta = 0;
            StickyDelta = 0;
            Gamma = 0;
            Vega = 0;
            Theta = 0;
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
