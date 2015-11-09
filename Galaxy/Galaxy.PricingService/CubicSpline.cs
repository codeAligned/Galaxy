using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PricingLib
{
    class CubicSpline : fittageVol
    {

        private double[,] _data;
        public Parameter[][] Polynomes;
        private double _forwardPrice;



        public CubicSpline(double[,] data, double forwardPrice)
        {
            _data = data;
            _forwardPrice = forwardPrice;
            polyCreation();

        }

        public void polyCreation()
        {
            Polynomes = new Parameter[_data.GetLength(0)][];
            Func<double, double>[] seriesFunc=new Func<double, double>[_data.GetLength(0)];
            double b;
            for (int i = 0; i < _data.GetLength(0); i++)
            {
                Polynomes[i] = new Parameter[4];
                Polynomes[i][0] = new Parameter(_data[i, 1]);
                Polynomes[i][1] = new Parameter(resPolyDerivative(i-1, _data[i, 0]));
                if (i==0)
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
                        (t) => resPolyDerivative2(i - 1, _data[i, 0], seriesFunc,t) / 2
                    };

                    seriesFunc[2 * i] = inter1[0];
                    //Polynomes[i][2] = new Parameter(resPolyDerivative2(i - 1, _data[i, 0]) / 2);
                }
                Func<double, double>[] inter2 =
                {
                    (t) => _data[i+1, 1]- _data[i, 0]- Polynomes[i][1].Value*(_data[i+1, 0]- _data[i, 0])- seriesFunc[2 * i](t) * Math.Pow(_data[i + 1, 0] - _data[i, 0],2)
                };
                seriesFunc[2 * i + 1] = inter2[0];


                //Polynomes[i][3] = new Parameter(_data[i+1, 1]- _data[i, 0]- Polynomes[i][1].Value*(_data[i+1, 0]- _data[i, 0])- Polynomes[i][2].Value * Math.Pow(_data[i + 1, 0] - _data[i, 0],2));
            }

            var condIniTrans = new Parameter(0);
            Func<double>[] NRFunctions =
            {
                () =>seriesFunc[2 * (_data.GetLength(0)-1) + 1](condIniTrans.Value)
            };

            Parameter[] NRParameters = { condIniTrans };

            NewtonRaphson NRclass = new NewtonRaphson(NRParameters, NRFunctions);

            for (int i = 0; i < 100; i++)
            {
                NRclass.Iterate();
            }

            b = NRclass._parameters[0].Value;

            for (int i = 0; i < _data.GetLength(0); i++)
            {
                Polynomes[i][2] = new Parameter(seriesFunc[2 * i](b));
                Polynomes[i][3] = new Parameter(seriesFunc[2 * i+1](b));
            }


        }

        public double resPoly( int i, double x)
        {
            double res = Polynomes[i][0].Value + Polynomes[i][1].Value * (x - _data[i, 0]) + Polynomes[i][2].Value * Math.Pow(x - _data[i, 0], 2) + Polynomes[i][3].Value * Math.Pow(x - _data[i, 0], 3);
            return (res);
        }
        public double resPolyDerivative( int i, double x)
        {
            double res = Polynomes[i][1].Value + 2* Polynomes[i][2].Value * (x - _data[i, 0]) + 3* Polynomes[i][3].Value * Math.Pow(x - _data[i, 0], 2);
            return (res);
        }
        public double resPolyDerivative2( int i, double x, Func<double, double>[] seriesFunc,double t)
        {
            double res =  2 * seriesFunc[2*i](t) + 6 * seriesFunc[2 * i+1](t) * (x - _data[i, 0]);
            return (res);
        }

    }
}
