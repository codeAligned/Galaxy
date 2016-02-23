
namespace Galaxy.PricingService
{
    public class BookPosition
    {
        protected double _cash;
        protected string _book;
        protected double _realisedPnl;
        protected double _ytdRealisedPnl;
        protected double _unrealisedPnl;
        protected double _fairUnrealisedPnl;
        protected double _delta;
        protected double _stickyDelta;
        protected double _gamma;
        protected double _vega;
        protected double _theta;

        public virtual double Cash
        {
            get { return _cash; }
            set { _cash = value; }
        }

        public virtual string Book
        {
            get { return _book; }
            set { _book = value; }
        }

        public virtual double RealisedPnl
        {
            get { return _realisedPnl; }
            set { _realisedPnl = value; }
        }

        public virtual double YtdRealisedPnl
        {
            get { return _ytdRealisedPnl; }
            set { _ytdRealisedPnl = value; }
        }

        public virtual double UnrealisedPnl
        {
            get { return _unrealisedPnl; }
            set { _unrealisedPnl = value; }
        }

        public virtual double FairUnrealisedPnl
        {
            get { return _fairUnrealisedPnl; }
            set { _fairUnrealisedPnl = value; }
        }

        public virtual double Delta
        {
            get { return _delta; }
            set { _delta = value; }
        }

        public virtual double StickyDelta
        {
            get { return _stickyDelta; }
            set { _stickyDelta = value; }
        }

        public virtual double Gamma
        {
            get { return _gamma; }
            set { _gamma = value; }
        }

        public virtual double Vega
        {
            get { return _vega; }
            set { _vega = value; }
        }

        public virtual double Theta
        {
            get { return _theta; }
            set { _theta = value; }
        }

        public double TheoricalPnl => RealisedPnl + UnrealisedPnl;
        public double YearToDatePnl => YtdRealisedPnl + UnrealisedPnl;

        public string ObsCash => Cash.ToString("N2");
        public string ObsRealisedPnl => RealisedPnl.ToString("N2");
        public string ObsLatentPnl => UnrealisedPnl.ToString("N2");
        public string ObsFairLatentPnl => FairUnrealisedPnl.ToString("N2");
        public string ObsTheoPnl => TheoricalPnl.ToString("N2");
        public string ObsYtdPnl => YearToDatePnl.ToString("N2");
        public string ObsBookDelta => Delta.ToString("N2");
        public string ObsBookStickyDelta => StickyDelta.ToString("N2");
        public string ObsBookGamma => Gamma.ToString("N2");
        public string ObsBookVega => Vega.ToString("N2");
        public string ObsBookTheta => Theta.ToString("N2");

        public BookPosition(InstrumentPosition pos)
        {
            Book = pos.Book;
            RealisedPnl = pos.RealisedPnl;
            YtdRealisedPnl = pos.YtdRealisedPnl;
            UnrealisedPnl = pos.UnrealisedPnl;
            FairUnrealisedPnl = pos.FairUnrealisedPnl;
        }
    }
}
