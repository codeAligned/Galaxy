using System;
using System.ComponentModel;
using Galaxy.DatabaseService;
using Galaxy.PricingService;

namespace Galaxy.DealManager.Model
{
    public class ObsInstruPosition : InstrumentPosition , INotifyPropertyChanged
    {
        public override double ForwardPrice
        {
            get { return _forwardPrice; }
            set { _forwardPrice = value; OnPropertyChanged(nameof(ObsForwardPrice)); }
        }

        public override double FairPrice
        {
            get { return _fairPrice; }
            set { _fairPrice = value; OnPropertyChanged(nameof(ObsFairPrice)); }
        }

        public override double ImpliedVol
        {
            get { return _impliedVol; }
            set { _impliedVol = value; OnPropertyChanged(nameof(ObsImpliedVol)); }
        }

        public override double ModelVol
        {
            get { return _modelVol; }
            set { _modelVol = value; OnPropertyChanged(nameof(ObsModelVol)); }
        }

        public override double Delta
        {
            get { return _delta; }
            set { _delta = value; OnPropertyChanged(nameof(ObsDelta)); }
        }

        public override double StickyDelta
        {
            get { return _stickyDelta; }
            set { _stickyDelta = value; OnPropertyChanged(nameof(ObsStickyDelta)); }
        }

        public override double Theta
        {
            get { return _theta; }
            set { _theta = value; OnPropertyChanged(nameof(ObsTheta)); }
        }

        public override double Rho
        {
            get { return _rho; }
            set { _rho = value; OnPropertyChanged(nameof(ObsRho)); }
        }

        public override double Vega
        {
            get { return _vega; }
            set { _vega = value; OnPropertyChanged(nameof(ObsVega)); }
        }

        public override double Gamma
        {
            get { return _gamma; }
            set { _gamma = value; OnPropertyChanged(nameof(ObsGamma)); }
        }

        public override double Vanna
        {
            get { return _vanna; }
            set { _vanna = value; OnPropertyChanged(nameof(ObsVanna)); }
        }

        public override double Vomma
        {
            get { return _vomma; }
            set { _vomma = value; OnPropertyChanged(nameof(ObsVomma)); }
        }

        public override double Charm
        {
            get { return _charm; }
            set { _charm = value; OnPropertyChanged(nameof(ObsCharm)); }
        }

        public override double Veta
        {
            get { return _veta; }
            set { _veta = value; OnPropertyChanged(nameof(ObsVeta)); }
        }

        public override double Color
        {
            get { return _color; }
            set { _color = value; OnPropertyChanged(nameof(ObsColor)); }
        }

        public override double Ultima
        {
            get { return _ultima; }
            set { _ultima = value; OnPropertyChanged(nameof(ObsUltima)); }
        }

        public override double Speed
        {
            get { return _speed; }
            set { _speed = value; OnPropertyChanged(nameof(ObsSpeed)); }
        }

        public override int Quantity
        {
            get { return _quantity; }
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public override double AvgPrice
        {
            get { return _avgPrice; }
            set { _avgPrice = value; OnPropertyChanged(nameof(AvgPrice)); }
        }

        public override double MtmPrice
        {
            get { return _mtmPrice; }
            set { _mtmPrice = value; OnPropertyChanged(nameof(MtmPrice)); }
        }

        public override double RealisedPnl
        {
            get { return _realisedPnl; }
            set { _realisedPnl = value; OnPropertyChanged(nameof(RealisedPnl)); }
        }

        public ObsInstruPosition(Deal deal) : base(deal){}

        public void ResetData()
        {
            RealisedPnl = 0;
            AvgPrice = 0;
            Quantity = 0;
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
