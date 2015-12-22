using System;

namespace PricingLib
{
    public static class FittingModel
    {
        public static Parameter[] SviFit(double[,] data, double forwardPrice)
        {
            Parameter[] sVIRawPara = new Parameter[5];
            double[,] _dataVarLogMon = LogMoneynessVarData(data, forwardPrice);

            sVIRawPara = InitSviParams(_dataVarLogMon);//ordre: a, b, rho, sigma, m

            var k = new Parameter(0);
            Parameter[] observedParameters = { k };

            Func<double>[] regressionFunction =
            {
                () => sVIRawPara[0].Value + sVIRawPara[1].Value*(sVIRawPara[2].Value*(k-sVIRawPara[4].Value) + Math.Sqrt(Math.Pow((k-sVIRawPara[4].Value),2)+Math.Pow(sVIRawPara[3].Value,2)))
            };

            return LevenbergMarquardt.Compute(regressionFunction, sVIRawPara, observedParameters, _dataVarLogMon, 5);
        }

        // Generate array of logmoneyness / power of( vol ) 
        public static double[,] LogMoneynessVarData(double[,] data, double forwardPrice)
        {
            double[,] dataLogMoneyness = (double[,])data.Clone();

            for (int i = 0; i < dataLogMoneyness.GetLength(0); i++)
            {
                dataLogMoneyness[i, 0] = Math.Log(dataLogMoneyness[i, 0] / forwardPrice);
                dataLogMoneyness[i, 1] = Math.Pow(dataLogMoneyness[i, 1], 2);
            }
            return (dataLogMoneyness);
        }

        public static double[,] LogMoneynessVolData(double[,] data, double forwardPrice)
        {
            double[,] dataLogMoneyness = (double[,])data.Clone();
            for (int i = 0; i < dataLogMoneyness.GetLength(0); i++)
            {
                dataLogMoneyness[i, 0] = Math.Log(dataLogMoneyness[i, 0] / forwardPrice);
            }
            return (dataLogMoneyness);
        }

        //ordre: a, b, rho, sigma, m
        public static Parameter[] InitSviParams(double[,] _dataVarLogMon)
        {
            Parameter[] _iniParameters = new Parameter[5];//ordre: a, b, rho, sigma, m
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
            _iniParameters[4] = new Parameter((_iniParameters[0].Value - aG) / _iniParameters[1].Value / (_iniParameters[2].Value - 1));//approximation +/- tolerable, m, ou douteuse acceptable dirons nous

            double minimalObservedVariance = _dataVarLogMon[size - 1, 1];//=a+b*sig*sqrt(1-rho^2)->comme a et b faux, ben on va faire autre chose
            for (int i = size - 2; i >= 0; i--)
            {
                if (_dataVarLogMon[i, 1] < minimalObservedVariance)
                {
                    minimalObservedVariance = _dataVarLogMon[i, 1];
                }
            }
            _iniParameters[3] = new Parameter(minimalObservedVariance);//faux mais passable
            return (_iniParameters);
        }


        public static void CubicSplineFit(double[,] data, double forwardPrice)
        {
            Parameter[][] polynomes;

            polynomes = new Parameter[data.GetLength(0)][];
            Func<double, double>[] seriesFunc = new Func<double, double>[data.GetLength(0)];
            double b;
            for (int i = 0; i < data.GetLength(0); i++)
            {
                polynomes[i] = new Parameter[4];
                polynomes[i][0] = new Parameter(data[i, 1]);
                polynomes[i][1] = new Parameter(resPolyDerivative(i - 1, data[i, 0], polynomes, data));
                if (i == 0)
                {
                    Func<double, double>[] inter1 =
                    {
                        (t) => 0
                    };

                    seriesFunc[2 * i] = inter1[0];


                    //Polynomes[i][2] = new Parameter(0);
                }
                else
                {
                    Func<double, double>[] inter1 =
                    {
                        (t) => resPolyDerivative2(i - 1, data[i, 0], seriesFunc,t, data) / 2
                    };

                    seriesFunc[2 * i] = inter1[0];
                    //Polynomes[i][2] = new Parameter(resPolyDerivative2(i - 1, _data[i, 0]) / 2);
                }
                Func<double, double>[] inter2 =
                {
                    (t) => data[i+1, 1]- data[i, 0]- polynomes[i][1].Value*(data[i+1, 0]- data[i, 0])- seriesFunc[2 * i](t) * Math.Pow(data[i + 1, 0] - data[i, 0],2)
                };
                seriesFunc[2 * i + 1] = inter2[0];


                //Polynomes[i][3] = new Parameter(_data[i+1, 1]- _data[i, 0]- Polynomes[i][1].Value*(_data[i+1, 0]- _data[i, 0])- Polynomes[i][2].Value * Math.Pow(_data[i + 1, 0] - _data[i, 0],2));
            }

            var condIniTrans = new Parameter(0);
            Func<double>[] NRFunctions =
            {
                () =>seriesFunc[2 * (data.GetLength(0)-1) + 1](condIniTrans.Value)
            };

            Parameter[] NRParameters = { condIniTrans };

            NewtonRaphson NRclass = new NewtonRaphson(NRParameters, NRFunctions);

            for (int i = 0; i < 100; i++)
            {
                NRclass.IterateNewton();
            }

            b = NRclass._parameters[0].Value;

            for (int i = 0; i < data.GetLength(0); i++)
            {
                polynomes[i][2] = new Parameter(seriesFunc[2 * i](b));
                polynomes[i][3] = new Parameter(seriesFunc[2 * i + 1](b));
            }
        }

        public static double resPoly(int i, double x, Parameter[][] polynomes, double[,] data)
        {
            double res = polynomes[i][0].Value + polynomes[i][1].Value * (x - data[i, 0]) + polynomes[i][2].Value * Math.Pow(x - data[i, 0], 2) + polynomes[i][3].Value * Math.Pow(x - data[i, 0], 3);
            return (res);
        }
        public static double resPolyDerivative(int i, double x, Parameter[][] polynomes, double[,] data)
        {
            double res = polynomes[i][1].Value + 2 * polynomes[i][2].Value * (x - data[i, 0]) + 3 * polynomes[i][3].Value * Math.Pow(x - data[i, 0], 2);
            return (res);
        }
        public static double resPolyDerivative2(int i, double x, Func<double, double>[] seriesFunc, double t, double[,] data)
        {
            double res = 2 * seriesFunc[2 * i](t) + 6 * seriesFunc[2 * i + 1](t) * (x - data[i, 0]);
            return (res);
        }


        //ordre: a, b, rho, sigma, m
        public static Parameter[] InitGsviParams(double[,] _dataVarLogMon)
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
            _iniParameters[4] = new Parameter(2 * (_iniParameters[0].Value - aG) / _iniParameters[1].Value / (_iniParameters[2].Value - 1));//approximation +/- tolerable, m, ou douteuse acceptable dirons nous

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

        public static Parameter[] GsviFit(double[,] data, double forwardPrice)
        {
            Parameter[] GSVIPara = new Parameter[6];
            double[,] dataVarLogMon = LogMoneynessVarData(data, forwardPrice);

            GSVIPara = InitGsviParams(dataVarLogMon);//ordre: a, b, rho, sigma, m

            var k = new Parameter(0);
            Parameter[] observedParameters = { k };

            Func<double>[] regressionFunction =
            {
                () => GSVIPara[0].Value + GSVIPara[1].Value*(GSVIPara[2].Value*((k-GSVIPara[4].Value)/Math.Pow(GSVIPara[5].Value,Math.Abs(k-GSVIPara[4].Value))-GSVIPara[4].Value)+Math.Sqrt(Math.Pow(((k-GSVIPara[4].Value)/Math.Pow(GSVIPara[5].Value,Math.Abs(k-GSVIPara[4].Value))-GSVIPara[4].Value),2)+Math.Pow(GSVIPara[3].Value,2)))
            };

            return LevenbergMarquardt.Compute(regressionFunction, GSVIPara, observedParameters, dataVarLogMon, 5);
        }

        //ordre: a, b, rho, sigma, m
        public static Parameter[] InitSviAdjParam(double[,] dataVarLogMon)
        {
            Parameter[] _iniParameters = new Parameter[5];//ordre: a, b, rho, sigma, m
            int size = dataVarLogMon.GetLength(0);


            double aG = (dataVarLogMon[0, 0] * dataVarLogMon[1, 1] - dataVarLogMon[0, 1] * dataVarLogMon[1, 0]) / (dataVarLogMon[0, 0] - dataVarLogMon[1, 0]);
            //=a+(b*sqrt(blabla(k1))*k0-b*sqrt(blabla(k0))*k1)/(k0-k1)=presque a à gauche
            double bG = (dataVarLogMon[0, 1] - dataVarLogMon[1, 1]) / (dataVarLogMon[0, 0] - dataVarLogMon[1, 0]);
            //b*(rho*(k1-k0)+delta(sqrt(blabla)))/(k1-k0)

            double aD = (dataVarLogMon[size - 2, 0] * dataVarLogMon[size - 1, 1] - dataVarLogMon[size - 2, 1] * dataVarLogMon[size - 1, 0]) / (dataVarLogMon[size - 2, 0] - dataVarLogMon[size - 1, 0]);
            double bD = (dataVarLogMon[size - 2, 1] - dataVarLogMon[size - 1, 1]) / (dataVarLogMon[size - 2, 0] - dataVarLogMon[size - 1, 0]);


            _iniParameters[1] = new Parameter(Math.Abs(bD + bG) / 2);// b est positif
            _iniParameters[2] = new Parameter(0);//rho + doit impliquer que slope right plus grand que left, d'où la construction arbitraire de ce rho
            // http: //arxiv.org/pdf/1204.0646.pdf page 5


            _iniParameters[0] = new Parameter(aG + bG * (aD - aG) / (bG + bD));//au lieu de couper la poire en deux, on prends en compte la variation de slope pour le partage entre aG et aD
            _iniParameters[4] = new Parameter((_iniParameters[0].Value - aG) / _iniParameters[1].Value / (_iniParameters[2].Value - 1));//approximation +/- tolerable, m, ou douteuse acceptable dirons nous

            double minimalObservedVariance = dataVarLogMon[size - 1, 1];//=a+b*sig*sqrt(1-rho^2)->comme a et b faux, ben on va faire autre chose
            for (int i = size - 2; i >= 0; i--)
            {
                if (dataVarLogMon[i, 1] < minimalObservedVariance)
                {
                    minimalObservedVariance = dataVarLogMon[i, 1];
                }
            }
            _iniParameters[3] = new Parameter(minimalObservedVariance);//faux mais passable
            return (_iniParameters);
        }

        public static Parameter[] GsviAdjustedFit(double[,] data, double forwardPrice)
        {
            double[,] dataVarLogMon = FittingModel.LogMoneynessVarData(data, forwardPrice);
            Parameter[] SVIRawPara = InitSviAdjParam(dataVarLogMon);//ordre: a, b, rho, sigma, m

            var k = new Parameter(0);
            Parameter[] observedParameters = { k };

            Func<double>[] regressionFunction =
            {
                () => SVIRawPara[0].Value + SVIRawPara[1].Value*(SVIRawPara[2].Value*(k-SVIRawPara[4].Value)+Math.Sqrt(Math.Pow((k-SVIRawPara[4].Value),2)+Math.Pow(SVIRawPara[3].Value,2)))
            };

            return LevenbergMarquardt.Compute(regressionFunction, SVIRawPara, observedParameters, dataVarLogMon, 5);
        }

        //public double getVolSviAdusted(double strike, double coef, double newFwd, )
        //{
        //    double k = Math.Log(strike / forwardPrice);
        //    return (Math.Sqrt(SVIRawPara[0].Value + SVIRawPara[1].Value * (SVIRawPara[2].Value * (k - SVIRawPara[4].Value) + Math.Sqrt(Math.Pow((k - SVIRawPara[4].Value), 2) + Math.Pow(SVIRawPara[3].Value, 2))))+coef*(newFwd-_forwardPrice)/newFwd);
        //}
    }
}
