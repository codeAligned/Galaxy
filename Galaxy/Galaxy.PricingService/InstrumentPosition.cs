using System;
using Galaxy.DatabaseService;
using static System.Math;

namespace Galaxy.PricingService
{
    public class InstrumentPosition
    {
        protected int _quantity;
        protected double _avgPrice;

        protected double _mtmPrice;
        protected double _forwardPrice;
        protected double _fairPrice;
        protected double _impliedVol;
        protected double _modelVol;

        protected double _delta;
        protected double _stickyDelta;
        protected double _theta;
        protected double _rho;
        protected double _vega;
        protected double _gamma;
        protected double _vanna;
        protected double _vomma;
        protected double _charm;
        protected double _veta;
        protected double _color;
        protected double _ultima;
        protected double _speed;
    
        protected double _realisedPnl;

        public string InstruRic { get; }
        public string InstruDescription { get; }
        public string OptionType { get; }
        public string ExerciseType { get; }
        public int Strike { get; }
        public DateTime MaturityDate { get; }
        public int LotSize { get; }

        public string ForwardId { get; set; }
        public string FutureId { get; set; }
        public string Market { get; }
        public string TtInstruId { get; }
        public string Book { get; }
        public string ProductName { get; }
        public string InstruType { get; }

        public int Value => Quantity * LotSize;
        
        public string ObsDelta => Delta.ToString("N2");
        public string ObsStickyDelta => StickyDelta.ToString("N2");
        public string ObsTheta => Theta.ToString("N2");
        public string ObsRho => Rho.ToString("N2");
        public string ObsVega => Vega.ToString("N2");
        public string ObsGamma => Gamma.ToString("N2");
        public string ObsVanna => Vanna.ToString("N2");
        public string ObsVomma => Vomma.ToString("N2");
        public string ObsCharm => Charm.ToString("N2");
        public string ObsVeta => Veta.ToString("N2");
        public string ObsColor => Color.ToString("N2");
        public string ObsUltima => Ultima.ToString("N2");
        public string ObsSpeed => Speed.ToString("N2"); 

        public string ObsModelVol => (ModelVol * 100).ToString("N2") + "%";
        public string ObsImpliedVol => (ImpliedVol * 100).ToString("N2") + "%";
        public double ObsFairPrice => Round(FairPrice, 2);
        public double ObsMtmPrice => Round(MtmPrice, 2);
        public double ObsForwardPrice => Round(ForwardPrice, 2);
        public double UnrealisedPnl => MtmPrice != 0 ? Round(Quantity * LotSize * (MtmPrice - AvgPrice), 2) : 0;
        public string ObsLatentPnl => UnrealisedPnl.ToString("N2");
        public string ObsRealisedPnl => RealisedPnl.ToString("N2");
        public double FairUnrealisedPnl => FairPrice != 0 ? Round(Quantity * LotSize * (FairPrice - AvgPrice), 2) : 0;
        public double Expiry => Max((MaturityDate - DateTime.Today).TotalDays, 0);
        public string VolParamsId => ProductName + "_" + MaturityDate.ToString("MMyyyy");

        public virtual double ForwardPrice
        {
            get { return _forwardPrice; }
            set { _forwardPrice = value; }
        }

        public virtual double FairPrice
        {
            get { return _fairPrice; }
            set { _fairPrice = value; }
        }

        public virtual double ImpliedVol
        {
            get { return _impliedVol; }
            set { _impliedVol = value; }
        }

        public virtual double ModelVol
        {
            get { return _modelVol; }
            set { _modelVol = value; }
        }

        public virtual double Theta
        {
            get { return _theta; }
            set { _theta = value; }
        }

        public virtual double Vega
        {
            get { return _vega; }
            set { _vega = value; }
        }

        public virtual double Gamma
        {
            get { return _gamma; }
            set { _gamma = value; }
        }

        public virtual double Delta
        {
            get { return _delta; }
            set { _delta = value; }
        }

        public virtual int Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }

        public virtual double AvgPrice
        {
            get { return _avgPrice; }
            set { _avgPrice = value; }
        }

        public virtual double MtmPrice
        {
            get { return _mtmPrice; }
            set { _mtmPrice = value; }
        }

        public virtual double RealisedPnl
        {
            get { return _realisedPnl; }
            set { _realisedPnl = value; }
        }

        public virtual double Rho
        {
            get { return _rho; }
            set { _rho = value; }
        }

        public virtual double Vanna
        {
            get { return _vanna; }
            set { _vanna = value; }
        }

        public virtual double Vomma
        {
            get { return _vomma; }
            set { _vomma = value; }
        }

        public virtual double Charm
        {
            get { return _charm; }
            set { _charm = value; }
        }

        public virtual double Veta
        {
            get { return _veta; }
            set { _veta = value; }
        }

        public virtual double Color
        {
            get { return _color; }
            set { _color = value; }
        }

        public virtual double Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        public virtual double Ultima
        {
            get { return _ultima; }
            set { _ultima = value; }
        }

        public virtual double StickyDelta
        {
            get { return _stickyDelta; }
            set { _stickyDelta = value; }
        }


        public InstrumentPosition()
        {
        }

        public InstrumentPosition(string bookId)
        {
            InstruRic = bookId;
        }

        public InstrumentPosition(Deal deal)
        {
            InstruRic = deal.InstrumentId;
            ProductName = deal.Instrument.ProductId;
            InstruDescription = deal.Instrument.FullName;
            InstruType = deal.Instrument.Product.ProductType;
            ExerciseType = deal.Instrument.Product.ExerciseType;
            OptionType = deal.Instrument.OptionType;
            Strike = deal.Instrument.Strike ?? 0;
            MaturityDate = deal.Instrument.MaturityDate;
            LotSize = deal.Instrument.Product.LotSize;
            Quantity = deal.Quantity;
            AvgPrice = deal.ExecPrice;
            Market = deal.Instrument.Product.Market;
            ForwardId = deal.Instrument.RefForwardId;
            FutureId = deal.Instrument.RefFutureId;
            TtInstruId = deal.Instrument.TtCode;
            Book = deal.BookId;
        }
    }
}
