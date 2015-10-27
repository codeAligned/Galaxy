using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PricingLib
{
    public class WingModel : fittageVol
    {
        private double[,] _data;
        private double[,] _dataVolLogMoy;
        private double _forwardPrice;
        private double downSm;             //the down smoothing range
        private double upSm;               //the up smoothing range
        private double downCutoff;
        private double UpCutoff;



        private double SSR;                //Skew swimmingness rate
        private double VCR;                //Volatility change rate
        private double SCR;                //Slope change rate
        private double synthFwd;           //the synthetic forward
        private double refPrice;
        private double volRef;
        private double slopeRef;

        public Parameter[] WingPutPara = new Parameter[3];//ordre: cste, slope, ordre 2
        public Parameter[] WingCallPara = new Parameter[3];//ordre: cste, slope, ordre 2
        public Parameter[] WingCenterPara = new Parameter[4];//ordre: cste, slope, put ordre 2, call ordre 2


        public WingModel(double[,] data, double forwardPrice)
        {
            _data = data;
            _forwardPrice = forwardPrice;
            upSm = 0.5;                                                                //default value
            downSm = 0.5;
            _dataVolLogMoy = LogMoneynessVolData(_data, _forwardPrice);
            downCutoff = _dataVolLogMoy[0, 0];
            UpCutoff = _dataVolLogMoy[_dataVolLogMoy.GetLength(0)-1, 0];




            //VCR = 0;                                                                   //default value
            //SCR = 0;
            //synthFwd = Math.Pow(_forwardPrice, SSR / 100) * Math.Pow(refPrice, 1 - SSR / 100);//attention, il faut des pointeurs, ref ou QQCH
            //volRef;
            //slopeRef;
            //SSR=100;
            //refPrice;


            InitialisationPara();//ordre: cste, slope, put ordre 2, call ordre 2

            var k = new Parameter(0);
            Parameter[] observedParameters = { k };

            Func<double>[] Function =
            {
                () => WingCenterPara[0].Value + WingCenterPara[1].Value * k + Math.Min(1,Math.Truncate(Math.Exp(k)))* WingCenterPara[2].Value * k*k + (1-Math.Min(1,Math.Truncate(Math.Exp(k))))* WingCenterPara[3].Value * k*k
            };


            var LMregressor = new LevenbergMarquardt(Function, WingCenterPara, observedParameters, _dataVolLogMoy, 5);//for point of derivatives, regarder codeWWWfavouriteP
            LMregressor.compute();

            WingCenterPara = LMregressor.regressionParameters;
            interpolationLagrangeExt();
        }

        public double getVol(double strike)
        {
            double res;
            double strikeM = Math.Log(strike / synthFwd);
            res = 1.0;
            if (strikeM < downCutoff * (1 + downSm)) //a faire
            {
                res = WingPutPara[0].Value + WingPutPara[1].Value * downCutoff * (1 + downSm) + WingPutPara[2].Value * downCutoff * (1 + downSm) * downCutoff * (1 + downSm);
            }
            else if (strikeM < downCutoff)  // a faire
            {
                res = WingPutPara[0].Value + WingPutPara[1].Value * strikeM + WingPutPara[2].Value * strikeM * strikeM;
            }
            else if (strikeM < 0)
            {
                res = WingCenterPara[0].Value + WingCenterPara[1].Value * strikeM + WingCenterPara[2].Value * strikeM * strikeM;
            }
            else if (strikeM < UpCutoff)
            {
                res = WingCenterPara[0].Value + WingCenterPara[1].Value * strikeM + WingCenterPara[3].Value * strikeM * strikeM;
            }
            else if (strikeM < UpCutoff * (1 + upSm)) // à faire
            {
                res = WingCallPara[0].Value + WingCallPara[1].Value * strikeM + WingCallPara[2].Value * strikeM * strikeM;
            }
            else // à faire
            {
                res = WingCallPara[0].Value + WingCallPara[1].Value * UpCutoff * (1 + upSm) + WingCallPara[2].Value * UpCutoff * (1 + upSm) * UpCutoff * (1 + upSm);
            }
            return res;
        }
        public void InitialisationPara()
        {
            int i = _dataVolLogMoy.GetLength(0) / 2;
            if (_dataVolLogMoy[i, 0] < 0)
            {
                while (_dataVolLogMoy[i, 0] < 0)
                {
                    i++;
                }
            }
            else
            {
                while (_dataVolLogMoy[i, 0] > 0)
                {
                    i--;
                }
            }

            double[,] points = new double[3, 2];
            System.Array.Copy(_dataVolLogMoy, 2 * (i - 1), points, 0, 6);


            WingCenterPara = interpolationLagrangeIni(points);


        }

        public Parameter[] interpolationLagrangeIni(double[,] points)
        {
            Parameter[] res=new Parameter[4];
            res[0] = new Parameter(0);
            res[1] = new Parameter(0);
            res[2] = new Parameter(0);
            for (int i=0; i<3; i++)
            {
                if (i == 0)
                {
                    res[2].Value += points[i, 1] / (points[i, 0] - points[i + 1, 0]) / (points[i, 0] - points[i + 2, 0]);
                    res[1].Value += -points[i, 1]* (points[i + 1, 0] + points[i + 2, 0]) / (points[i, 0] - points[i + 1, 0]) / (points[i, 0] - points[i + 2, 0]);
                    res[0].Value += points[i, 1]* points[i + 1, 0]* points[i + 2, 0] / (points[i, 0] - points[i + 1, 0]) / (points[i, 0] - points[i + 2, 0]);
                }
                if (i == 1)
                {
                    res[2].Value += points[i, 1] / (points[i, 0] - points[i -1, 0]) / (points[i, 0] - points[i + 1, 0]);
                    res[1].Value += -points[i, 1]*(points[i + 1, 0] + points[i - 1, 0]) / (points[i, 0] - points[i - 1, 0]) / (points[i, 0] - points[i + 1, 0]);
                    res[0].Value += points[i, 1] * points[i + 1, 0] * points[i -1, 0] / (points[i, 0] - points[i - 1, 0]) / (points[i, 0] - points[i + 1, 0]);
                }
                if (i == 2)
                {
                    res[2].Value += points[i, 1] / (points[i, 0] - points[i - 1, 0]) / (points[i, 0] - points[i - 2, 0]);
                    res[1].Value += -points[i, 1] * (points[i - 1, 0] + points[i - 2, 0]) / (points[i, 0] - points[i - 1, 0]) / (points[i, 0] - points[i - 2, 0]);
                    res[0].Value += points[i, 1] * points[i - 1, 0] * points[i - 2, 0] / (points[i, 0] - points[i - 1, 0]) / (points[i, 0] - points[i - 2, 0]);
                }
            }
            res[3] = new Parameter(res[2].Value);
            return res;
        }

        public void interpolationLagrangeExt()
        {
            double valInter;
            valInter = (WingCenterPara[1].Value+2*downCutoff*WingCenterPara[2].Value)/2/(downCutoff*downSm);
            WingPutPara[2] = new Parameter(valInter);
            WingPutPara[1] = new Parameter(-valInter*2*downCutoff*(1+downSm));
            WingPutPara[0] = new Parameter(WingCenterPara[0].Value + WingCenterPara[1].Value * downCutoff + WingCenterPara[2].Value * downCutoff * downCutoff- WingPutPara[1].Value*downCutoff-WingPutPara[2].Value*downCutoff*downCutoff);
            valInter = (WingCenterPara[1].Value + 2 * UpCutoff * WingCenterPara[3].Value) / 2 / (UpCutoff * upSm);
            WingCallPara[2] = new Parameter(valInter);
            WingCallPara[1] = new Parameter(-valInter * 2 * UpCutoff * (1 + upSm));
            WingCallPara[0] = new Parameter(WingCenterPara[0].Value + WingCenterPara[1].Value * UpCutoff + WingCenterPara[3].Value * UpCutoff * UpCutoff - WingCallPara[1].Value * UpCutoff - WingCallPara[2].Value * UpCutoff * UpCutoff);

        }

    }
}
