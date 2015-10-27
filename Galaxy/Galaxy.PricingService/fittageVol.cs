using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PricingLib
{
    public class fittageVol
    {
        public fittageVol() { }  

        public double[,] LogMoneynessVarData(double[,] data, double forwardPrice)
        {
            double[,] dataLogMoneyness =(double[,]) data.Clone();
            for (int i=0; i<dataLogMoneyness.GetLength(0) ;i++)
            {
                dataLogMoneyness[i, 0] = Math.Log(dataLogMoneyness[i, 0] / forwardPrice);
                dataLogMoneyness[i, 1] = Math.Pow(dataLogMoneyness[i, 1], 2);
            }
            return (dataLogMoneyness);
        }

        public double[,] LogMoneynessVolData(double[,] data, double forwardPrice)
        {
            double[,] dataLogMoneyness = (double[,])data.Clone();
            for (int i = 0; i < dataLogMoneyness.GetLength(0); i++)
            {
                dataLogMoneyness[i, 0] = Math.Log(dataLogMoneyness[i, 0] / forwardPrice);
            }
            return (dataLogMoneyness);
        }


    }
}
