using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PricingLib
{
    public class GSVI:fittageVol
    {
        public Parameter[] GSVIPara = new Parameter[6];
        private double[,] _data;
        private double _forwardPrice;
        private double[,] _dataVarLogMon;


        //ordre: a, b, rho, sigma, m
        public Parameter[] InitialisationPara()
        {
            Parameter[] _iniParameters = new Parameter[6];//ordre: a, b, rho, sigma, m
            int size = _dataVarLogMon.GetLength(0);


            double aG = (_dataVarLogMon[0, 0] * _dataVarLogMon[1, 1] - _dataVarLogMon[0, 1] * _dataVarLogMon[1, 0]) / (_dataVarLogMon[0, 0] - _dataVarLogMon[1, 0]);
            //=a+(b*sqrt(blabla(k1))*k0-b*sqrt(blabla(k0))*k1)/(k0-k1)=presque a à gauche
            double bG = (_dataVarLogMon[0, 1] - _dataVarLogMon[1, 1]) / (_dataVarLogMon[0, 0] - _dataVarLogMon[1, 0]);
            //b*(rho*(k1-k0)+delta(sqrt(blabla)))/(k1-k0)



            double aD = (_dataVarLogMon[size - 2, 0] * _dataVarLogMon[size - 1, 1] - _dataVarLogMon[size - 2, 1] * _dataVarLogMon[size - 1, 0]) / (_dataVarLogMon[size - 2, 0] - _dataVarLogMon[size - 1, 0]);
            double bD = (_dataVarLogMon[size - 2, 1] - _dataVarLogMon[size - 1, 1]) / (_dataVarLogMon[size - 2, 0] - _dataVarLogMon[size - 1, 0]);


            _iniParameters[1] = new Parameter(Math.Abs(bD + bG) / 2);// b est positif
            _iniParameters[2] = new Parameter(0);//rho + doit impliquer que slope right plus grand que left, d'où la construction arbitraire de ce rho
            // http: //arxiv.org/pdf/1204.0646.pdf page 5


            _iniParameters[0] = new Parameter(aG + bG * (aD - aG) / (bG + bD));//au lieu de couper la poire en deux, on prends en compte la variation de slope pour le partage entre aG et aD
            _iniParameters[4] = new Parameter(2*(_iniParameters[0].Value - aG) / _iniParameters[1].Value / (_iniParameters[2].Value - 1));//approximation +/- tolerable, m, ou douteuse acceptable dirons nous




            double minimalObservedVariance = _dataVarLogMon[size - 1, 1];//=a+b*sig*sqrt(1-rho^2)->comme a et b faux, ben on va faire autre chose
            for (int i = size - 2; i >= 0; i--)
            {
                if (_dataVarLogMon[i, 1] < minimalObservedVariance)
                {
                    minimalObservedVariance = _dataVarLogMon[i, 1];
                }
            }
            _iniParameters[3] = new Parameter(minimalObservedVariance);//faux mais passable
            _iniParameters[5] = new Parameter(1);


            return (_iniParameters);
        }

        public GSVI(double[,] data, double forwardPrice)
        {
            _data = data;
            _forwardPrice = forwardPrice;
            _dataVarLogMon = LogMoneynessVarData(data, forwardPrice);


            GSVIPara = InitialisationPara();//ordre: a, b, rho, sigma, m

            var k = new Parameter(0);
            Parameter[] observedParameters = { k };

            Func<double>[] regressionFunction =
            {
                () => GSVIPara[0].Value + GSVIPara[1].Value*(GSVIPara[2].Value*((k-GSVIPara[4].Value)/Math.Pow(GSVIPara[5].Value,Math.Abs(k-GSVIPara[4].Value))-GSVIPara[4].Value)+Math.Sqrt(Math.Pow(((k-GSVIPara[4].Value)/Math.Pow(GSVIPara[5].Value,Math.Abs(k-GSVIPara[4].Value))-GSVIPara[4].Value),2)+Math.Pow(GSVIPara[3].Value,2)))
            };


       //     var LMregressor = new LevenbergMarquardt(regressionFunction, GSVIPara, observedParameters, _dataVarLogMon, 5);
            GSVIPara =  LevenbergMarquardt.Compute(regressionFunction, GSVIPara, observedParameters, _dataVarLogMon, 5);

      //      GSVIPara = LMregressor._regressionParameters;
        }

        public double getVol(double strike)
        {
            double k = Math.Log(strike / _forwardPrice);
            return (GSVIPara[0].Value + GSVIPara[1].Value * (GSVIPara[2].Value * ((k - GSVIPara[4].Value) / Math.Pow(GSVIPara[5].Value, Math.Abs(k - GSVIPara[4].Value)) - GSVIPara[4].Value) + Math.Sqrt(Math.Pow(((k - GSVIPara[4].Value) / Math.Pow(GSVIPara[5].Value, Math.Abs(k - GSVIPara[4].Value)) - GSVIPara[4].Value), 2) + Math.Pow(GSVIPara[3].Value, 2))));
        }


    }
}
