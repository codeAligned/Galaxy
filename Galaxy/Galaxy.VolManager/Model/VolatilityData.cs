using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galaxy.VolManager.Model
{
    public class VolatilityData
    {
        public VolatilityData (int strike, DateTime maturity, string productType, string productName, string market, string exerciceType, string forwardName, string futureName )
        {
            Strike = strike;
            Maturity = maturity;
            ProductType = productType;
            ProductName = productName;
            Market = market;
            ExerciceType = exerciceType;
            ForwardName = forwardName;
            FutureName = futureName;
        }

        public int Strike { get; private set; }
        public DateTime Maturity { get; private set; }
        public string ProductType { get; private set; }
        public string ProductName { get; private set; }
        public string Market { get; private set; }
        public string ExerciceType { get; private set; }
        public string ForwardName { get; private set; }
        public string FutureName { get; private set; }

        public double Volatility { get; set; }

    }
}
