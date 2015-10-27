using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galaxy.PricingService
{
    public class BookPosition
    {
        protected double _cash;
        protected string _book;
        protected double _realisedPnl;
        protected double _unrealisedPnl;
        protected double _fairUnrealisedPnl;
        protected double _delta;
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
            set { _book = value;}
        }

        public virtual double RealisedPnl
        {
            get { return _realisedPnl; }
            set { _realisedPnl = value;}
        }

        public virtual double UnrealisedPnl
        {
            get { return _unrealisedPnl; }
            set { _unrealisedPnl = value;}
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

        public string ObsCash => Cash.ToString("N2");
        public string ObsRealisedPnl => RealisedPnl.ToString("N2");
        public string ObsLatentPnl => UnrealisedPnl.ToString("N2");
        public string ObsFairLatentPnl => FairUnrealisedPnl.ToString("N2");
        public string ObsTheoPnl => TheoricalPnl.ToString("N2");
        public string ObsBookDelta => Delta.ToString("N2");
        public string ObsBookGamma => Gamma.ToString("N2");
        public string ObsBookVega => Vega.ToString("N2");
        public string ObsBookTheta => Theta.ToString("N2");

        public BookPosition(InstrumentPosition pos)
        {
            Book = pos.Book;
            RealisedPnl = pos.RealisedPnl;
            UnrealisedPnl = pos.UnrealisedPnl;
            FairUnrealisedPnl = pos.FairUnrealisedPnl;
        }
    }
}
