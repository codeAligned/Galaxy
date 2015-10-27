//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Linq;
//using Galaxy.DatabaseService;
//using log4net;

//namespace Galaxy.PricingService
//{
//    public sealed class OptionLib
//    {
//        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

//        private readonly int _strikeBase = 50;
//        private readonly int _timeBasis  = 365;
//        private readonly double _spotBump = 0.001;
//        private readonly double _volBump = 0.001;

//        private int _global_npar = 5;
//        private int _global_nobs;

//        private double _global_Forward;
//        private double _global_Time;

//        private int _maxIterations = 25;
//        private double _res0 = Math.Pow(10, 9);
//        private int _nu0 = 1000;

//        private readonly IDbManager _dbManager;

//        private Dictionary<string, MaturityData> _fwdPricingDataDico;

//        private static readonly OptionLib instance = new OptionLib();

//        public static OptionLib UniqueInstance
//        {
//            get { return instance; }
//        }

//        private OptionLib()
//        {
//            _dbManager = new DbManager();
//            _fwdPricingDataDico = new Dictionary<string, MaturityData>();
//        }

//        /// <summary>
//        /// Return Days between maturity and Today
//        /// </summary>
//        public double GetTimeToExpiration(DateTime asOfDate, DateTime maturity)
//        {
//            return (maturity - asOfDate).TotalDays / _timeBasis;
//        }

//        /// <summary>
//        /// Compute dividend based on strike and Close price of options ATM
//        /// </summary>
//        public double GetImpliedDividend(double strike1, double strike2, double closeSpot, double downCallPrice,
//                                         double downPutPrice, double upCallPrice, double upPutPrice, double t)
//        {

//            if (upCallPrice > downCallPrice || upPutPrice < downPutPrice)
//            {
//                Exception e = new Exception("Wrong Close prices");
//                log.Error(e);
//                throw e;
//            }

//            return
//                -Math.Log((strike2 * (downCallPrice - downPutPrice) - strike1 * (upCallPrice - upPutPrice)) /
//                          (closeSpot * (strike2 - strike1))) / t;
//        }

//        /// <summary>
//        /// Compute Interest Rate based on strike and Close price of options ATM
//        /// </summary>
//        public double GetImpliedRate(double div, double strike, double closeSpot, double callPrice, double putPrice, double t)
//        {
//            /// todo add check on input value to avoid nan
//            return -Math.Log((closeSpot * Math.Exp(-div * t) - callPrice + putPrice) / strike) / t;
//        }

//        /// <summary>
//        /// Compute Forward Close price using interest rate and dividend
//        /// </summary>
//        public double GetFwdClosePrice(double spotClose, double impliedRate, double impliedDiv, double timeToExpi)
//        {
//            return spotClose * Math.Exp((impliedRate - impliedDiv) * timeToExpi);
//        }

//        /// <summary>
//        /// Returns delta between Forward close price and Front future Close price
//        /// </summary>
//        public double GetFwdBaseOffset(double forwardClosePrice, double futureClosePrice)
//        {
//            return forwardClosePrice - futureClosePrice;
//        }

//        /// <summary>
//        /// Implied Volatility for market prices Put and Call OTM & ATM exclusively
//        /// Exercice type: "EU" or "US"
//        /// Option type: "CALL" or "PUT"
//        /// </summary>
//        public double ImpliedVolBisection(double accuracy, double optionTragetPrice, double firstGuess,
//                                          double lowerBound, double upperbound, double underlyingPrice,
//                                          double strike, string optionType, string exerciceType, string forwardRic,
//                                          DateTime maturity,string futureId)
//        {

//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                data = BuildForward(maturity, forwardRic,futureId);

//                if (data == null)
//                    return 0;
//            }

//            double functionReturnValue = 0;

//            double fFirstGuess = 0;
//            double i = 0;
//            double diff = 0;
//            double fMid = 0;

//            if (lowerBound > upperbound)
//            {
//                return functionReturnValue;
//            }

//            if (firstGuess > upperbound | firstGuess < lowerBound)
//            {
//                return functionReturnValue;
//            }

//            if (exerciceType == "EU")
//            {
//                fFirstGuess = BlackScholes(optionType, underlyingPrice, strike, data.ImpliedRate, data.ImpliedDiv, firstGuess, data.TimeToExpi);
//            }

//            if (exerciceType == "US")
//            {
//                fFirstGuess = BjerksundStensland(optionType, underlyingPrice, strike, data.TimeToExpi, data.ImpliedRate, data.ImpliedDiv, firstGuess);
//            }
//            diff = optionTragetPrice - fFirstGuess;


//            if (diff < 0)
//            {
//                upperbound = firstGuess;
//            }
//            else if (diff > 0)
//            {
//                lowerBound = firstGuess;
//            }

//            for (i = 0; i <= 100; i++)
//            {
//                var mid = (upperbound + lowerBound) / 2;
//                if (exerciceType == "EU")
//                    fMid = BlackScholes(optionType, underlyingPrice, strike, data.ImpliedRate, data.ImpliedDiv, mid,
//                                        data.TimeToExpi);
//                if (exerciceType == "US")
//                    fMid = BjerksundStensland(optionType, underlyingPrice, strike, data.TimeToExpi, data.ImpliedRate,
//                                              data.ImpliedDiv, mid);

//                if (Math.Abs(fMid - optionTragetPrice) <= accuracy)
//                {
//                    return mid;
//                }

//                diff = optionTragetPrice - fMid;
//                if (diff < 0)
//                {
//                    upperbound = mid;
//                }
//                else if (diff > 0)
//                {
//                    lowerBound = mid;
//                }
//            }
//            return functionReturnValue;
//        }

//        public double OldImpliedVolBisection(double accuracy, double tragetPrice, double spot,
//                                        double strike, string optionType, double rate, double div, double time)
//        {

//            double upperbound = 1;
//            double lowerBound = 0;
//            double firstGuess = (upperbound + lowerBound) / 2;

//            double fFirstGuess = BlackScholes(optionType, spot, strike, rate, div, firstGuess, time);

//            double diff = tragetPrice - fFirstGuess;


//            if (diff < 0)
//            {
//                upperbound = firstGuess;
//            }
//            else if (diff > 0)
//            {
//                lowerBound = firstGuess;
//            }

//            for (int i = 0; i <= 100; i++)
//            {
//                var mid = (upperbound + lowerBound) / 2;

//                double fMid = BlackScholes(optionType, spot, strike, rate, div, mid, time);

//                if (Math.Abs(fMid - tragetPrice) <= accuracy)
//                {
//                    return mid;
//                }

//                diff = tragetPrice - fMid;
//                if (diff < 0)
//                {
//                    upperbound = mid;
//                }
//                else if (diff > 0)
//                {
//                    lowerBound = mid;
//                }
//            }
//            return 0;
//        }


//        /// <summary>
//        /// Return the option type which is outside the market 
//        /// return value: "CALL" or "PUT"
//        /// </summary>
//        public string GetOtmOptionType(int strike, double underlyingPrice)
//        {
//            string targetOptionType = "";

//            if (underlyingPrice == 0)
//            {
//                var e = new Exception("Spot price cannot be zero");
//                log.Error(e);
//                throw e;
//            }

//            if (underlyingPrice >= strike)
//            {
//                //Target price: Put OTM
//                targetOptionType = "PUT";

//            }
//            else
//            {
//                //Target price: Call OTM
//                targetOptionType = "CALL";
//            }

//            return targetOptionType;
//        }


//        public double ComputeBlackScholes(string callPut, double spot, double strike, double vol, string forwardRic)
//        {
//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return 0;
//            }

//            double rate = 0;
//            double div = 0;

//            return BlackScholes(callPut, spot, strike, rate, div, vol, data.TimeToExpi);
//        }



//        public double BlackScholes(string callPut, double spot, double strike, double rate, double div, double vol,
//                                    double timeToExpi)
//        {


//            if (callPut == "CALL")
//                return (European_Call(spot, strike, timeToExpi, rate, div, vol));
//            else if (callPut == "PUT")
//                return (European_Put(spot, strike, timeToExpi, rate, div, vol));
//            else
//                return (0);
//        }

//        //BS Model European Call 
//        private double European_Call(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double dt = 0;
//            double d1 = 0;
//            double d2 = 0;
//            double Nd1 = 0;
//            double Nd2 = 0;

//            dt = vol * Math.Sqrt(t);
//            d1 = (Math.Log(spot / strike) + (rate - div + 0.5 * Math.Pow(vol, 2)) * t) / dt;
//            d2 = d1 - dt;

//            Nd1 = NormalDistrib(d1);
//            Nd2 = NormalDistrib(d2);

//            return (Math.Exp(-div * t) * spot * Nd1) - (strike * Math.Exp(-rate * t) * Nd2);
//        }

//        //BS Model European Put
//        private double European_Put(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double dt = 0;
//            double d1 = 0;
//            double d2 = 0;
//            double NNd1 = 0;
//            double NNd2 = 0;

//            dt = vol * Math.Sqrt(t);
//            d1 = (Math.Log(spot / strike) + (rate - div + 0.5 * Math.Pow(vol, 2)) * t) / dt;
//            d2 = d1 - dt;


//            NNd1 = NormalDistrib(-d1);
//            NNd2 = NormalDistrib(-d2);

//            return (-spot * Math.Exp(-div * t) * NNd1) + (strike * Math.Exp(-rate * t) * NNd2);
//        }

//        //normal distribution
//        private double NormalDistrib(double z)
//        {
//            double sign = (z < 0) ? -1 : 1;
//            return 0.5 * (1.0 + sign * erf(Math.Abs(z) / Math.Sqrt(2)));
//        }

//        private double erf(double x)
//        {
//            //A&S formula 7.1.26
//            double a1 = 0.254829592;
//            double a2 = -0.284496736;
//            double a3 = 1.421413741;
//            double a4 = -1.453152027;
//            double a5 = 1.061405429;
//            double p = 0.3275911;
//            x = Math.Abs(x);
//            double t = 1 / (1 + p * x);
//            //Direct calculation using formula 7.1.26 is absolutely correct
//            //But calculation of nth order polynomial takes O(n^2) operations
//            //return 1 - (a1 * timeToExpi + a2 * timeToExpi * timeToExpi + a3 * timeToExpi * timeToExpi * timeToExpi + a4 * timeToExpi * timeToExpi * timeToExpi * timeToExpi + a5 * timeToExpi * timeToExpi * timeToExpi * timeToExpi * timeToExpi) * Math.Exp(-1 * x * x);

//            //Horner's method, takes O(n) operations for nth order polynomial
//            return 1 - ((((((a5 * t + a4) * t) + a3) * t + a2) * t) + a1) * t * Math.Exp(-1 * x * x);
//        }

//        public double BjerksundStensland(string exerciceType, double spot, double strike, double t, double rate,
//                                         double div, double vol)
//        {
//            double functionReturnValue = 0;

//            double Binfinity, BB, dt, ht, I, alpha, beta, b1, rr = 0, dd = 0, dd2 = 0, assetnew, drift, v2, z = 0;

//            if (exerciceType == "CALL")
//            {
//                z = 1;
//                rr = rate;
//                dd2 = div;
//            }
//            else if (exerciceType == "PUT")
//            {
//                z = -1;
//                rr = div;
//                dd = rr;
//                dd2 = 2 * dd - rr;
//                assetnew = spot;
//                spot = strike;
//                strike = assetnew;
//            }

//            dt = vol * Math.Sqrt(t);
//            drift = rate - div;
//            v2 = Math.Pow(vol, 2);

//            if ((z * (rate - div) >= rr))
//            {
//                if (exerciceType == "CALL")
//                {
//                    functionReturnValue = European_Call(spot, strike, rr, div, t, vol);
//                }
//                else
//                {
//                    functionReturnValue = European_Call(spot, strike, rr, dd, t, vol);
//                }
//            }
//            else
//            {
//                b1 = Math.Sqrt(Math.Pow((z * drift / v2 - 0.5), 2) + 2 * rr / v2);
//                beta = (0.5 - z * drift / v2) + b1;
//                Binfinity = beta / (beta - 1) * strike;
//                BB = Math.Max(strike, rr / dd2 * strike);
//                ht = -(z * drift * t + 2 * dt) * BB / (Binfinity - BB);
//                I = BB + (Binfinity - BB) * (1 - Math.Exp(ht));
//                alpha = (I - strike) * Math.Pow(I, (-beta));

//                if ((spot >= I))
//                {
//                    functionReturnValue = spot - strike;
//                }
//                else
//                {
//                    functionReturnValue = alpha * Math.Pow(spot, beta) - alpha * Phi(spot, t, beta, I, I, rr, z * drift, vol) +
//                                          Phi(spot, t, 1, I, I, rr, z * drift, vol) -
//                                          Phi(spot, t, 1, strike, I, rr, z * drift, vol) -
//                                          strike * Phi(spot, t, 0, I, I, rr, z * drift, vol) +
//                                          strike * Phi(spot, t, 0, strike, I, rr, z * drift, vol);
//                }
//            }
//            return functionReturnValue;
//        }

//        //Phi function used into BjerksundStensland pricing model
//        public double Phi(double spot, double t, double gamma, double h, double i, double r, double a, double V)
//        {
//            double Lambda, k, dd, dt;

//            dt = V * Math.Sqrt(t);
//            Lambda = (-r + gamma * a + 0.5 * gamma * (gamma - 1) * Math.Pow(V, 2)) * t;
//            dd = -(Math.Log(spot / h) + (a + (gamma - 0.5) * Math.Pow(V, 2)) * t) / dt;
//            k = 2 * a / Math.Pow(V, 2) + (2 * gamma - 1);

//            return (Math.Exp(Lambda) * Math.Pow(spot, gamma) *
//                    (NormalDistrib(dd) - Math.Pow((i / spot), k) * NormalDistrib(dd - 2 * Math.Log(i / spot) / dt)));
//        }

//        public double Gatheral_Vola(double strike, double forward, double a, double b, double sigma, double rho,
//                                    double m, string forwardRic)
//        {
//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return 0;
//            }

//            double k = Math.Log(strike / forward);
//            return Math.Sqrt(Math.Abs(Gatheral_Variance(k, a, b, sigma, rho, m) / data.TimeToExpi));
//        }

//        public double Gatheral_Vola(double strike, double forward, double a, double b, double sigma, double rho, double m, double timeToExpi)
//        {
//            double k = 0;

//            k = Math.Log(strike / forward);
//            return Math.Sqrt(Math.Abs(Gatheral_Variance(k, a, b, sigma, rho, m) / timeToExpi));
//        }

//        private double Gatheral_Variance(double k, double a, double b, double sigma, double rho, double m)
//        {
//            // variance function from the paper, parameters given individually


//        //   double k_m = (k - m);
//            return a + b * (rho * (k - m) + Math.Sqrt((k - m) * (k - m) + sigma * sigma));
//        }

//        /// <summary>
//        /// Return option maturity code
//        /// Option type: "CALL" or "PUT"
//        /// </summary>
//        public string GetOptionMaturityId(string optionType, DateTime maturityDate)
//        {
//            int value = maturityDate.Month;
//            string letter = "";

//            if (optionType == "CALL")
//            {
//                switch (value)
//                {
//                    case 1:{  letter = "A"; break; }
//                    case 2:{  letter = "B"; break; }
//                    case 3:{  letter = "C"; break; }
//                    case 4:{  letter = "D"; break; }
//                    case 5:{  letter = "E"; break; }
//                    case 6:{  letter = "F"; break; }
//                    case 7:{  letter = "G"; break; }
//                    case 8:{  letter = "H"; break; }
//                    case 9:{  letter = "I"; break; }
//                    case 10:{ letter = "J"; break; }
//                    case 11:{ letter = "K"; break; }
//                    case 12:{ letter = "L"; break; }
//                    default:{ letter = "?"; break; }
//                }
//            }
//            else if (optionType == "PUT")
//            {
//                switch (value)
//                {
//                    case 1:{  letter = "M"; break; }
//                    case 2:{  letter = "N"; break; }
//                    case 3:{  letter = "O"; break; }
//                    case 4:{  letter = "P"; break; }
//                    case 5:{  letter = "Q"; break; }
//                    case 6:{  letter = "R"; break; }
//                    case 7:{  letter = "S"; break; }
//                    case 8:{  letter = "T"; break; }
//                    case 9:{  letter = "U"; break; }
//                    case 10:{ letter = "V"; break; }
//                    case 11:{ letter = "W"; break; }
//                    case 12:{ letter = "X"; break; }
//                    default:{ letter = "?"; break; }
//                }
//            }

//            string year = maturityDate.Year.ToString();
//            char yearNb = year[3];

//            return letter + yearNb;
//        }

//        /// <summary>
//        /// Return future maturity code
//        /// </summary>
//        public string GetFutureMaturityId( DateTime maturityDate)
//        {
//            int value = maturityDate.Month;
//            string letter = "";
//            switch (value)
//            {
//                case 1:{ letter = "F"; break; }
//                case 2:{ letter = "G"; break; }
//                case 3:{ letter = "H"; break; }
//                case 4:{ letter = "J"; break; }
//                case 5:{ letter = "K"; break; }
//                case 6:{ letter = "M"; break; }
//                case 7:{ letter = "N"; break; }
//                case 8:{ letter = "Q"; break; }
//                case 9:{ letter = "U"; break; }
//                case 10:{letter = "V"; break; }
//                case 11:{letter = "X"; break; }
//                case 12:{letter = "Z"; break; }
//                default:{letter = "?"; break; } 
//            }

//            string year = maturityDate.Year.ToString();
//            char yearNb = year[3];

//            return letter + yearNb;
//        }


//        public string GetNextFutureTtCode(DateTime OptionMaturity)
//        {
//            int month = OptionMaturity.Month;
//            int year = OptionMaturity.Year;
//            string res = "";
//            switch (month)
//            {
//                case 1:
//                case 2:
//                case 3:
//                    {
//                        res = "03" + year;
//                        break;
//                    }
//                case 4:
//                case 5:
//                case 6:
//                    {
//                        res = "06" + year;
//                        break;

//                    }
//                case 7:
//                case 8:
//                case 9:
//                    {
//                        res = "09" + year;
//                        break;

//                    }
//                case 10:
//                case 11:
//                case 12:
//                    {
//                        res = "12" + year;
//                        break;

//                    }
//                default:
//                    {
//                        res = "?";
//                        break;
//                    }
//            }

//            return res;
//        }


//        /// <summary>
//        ////Numerical Delta per option on volatility smile (in nb of Future) 
//        /// approx div = 0 rate = 0
//        /// </summary>
//        public double NumericalDelta(string optionType, string exerciceType, double forward, double modelVol, 
//            double strike,double quantity, string forwardRic,double a,double b,double sigma,double rho, double m)
//        {
//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return 0;
//            }

//            double div = 0;
//            double rate = 0;

//            double fwdBumpUp = forward * (1 + _spotBump);
//            double fwdBumpDown = forward * (1 - _spotBump);

//            double volBumpUp = Gatheral_Vola(strike, fwdBumpUp, a, b, sigma, rho, m, forwardRic);
//            double volBumpDown = Gatheral_Vola(strike, fwdBumpDown, a, b, sigma, rho, m, forwardRic);

//            if (exerciceType == "EU")
//            {
//                double fairBumpUp = BlackScholes(optionType, fwdBumpUp, strike, rate, div, volBumpUp, data.TimeToExpi);
//                double fairBumpDown = BlackScholes(optionType, fwdBumpDown, strike, rate, div, volBumpDown, data.TimeToExpi);
//                return ((fairBumpUp - fairBumpDown)* quantity) / (2 * _spotBump * forward);
//            }

//            if (exerciceType == "US")
//            {
//                double fairBumpUp = BjerksundStensland(optionType, fwdBumpUp, strike, data.TimeToExpi, rate, div, volBumpUp);
//                double fairBumpDown = BjerksundStensland(optionType, fwdBumpDown, strike, data.TimeToExpi, rate, div, volBumpDown);
//                return ((fairBumpUp - fairBumpDown) * quantity) / (2 * _spotBump * forward);
//            }
//            return (0);
//        }

//        /// <summary>
//        ////Numerical Gamma per option on volatility smile (in nb of Future) 
//        /// approx div = 0 rate = 0
//        /// </summary>
//        public double NumericalGamma(string optionType, string exerciceType, double underlyingPrice,
//                                     double modelVol, double strike,double quantity, string forwardRic, 
//                                     double a, double b, double sigma, double rho, double m)
//        {

//            double spotBump = (1 + _volBump) * underlyingPrice;
//            double delta = NumericalDelta(optionType, exerciceType, underlyingPrice, modelVol, strike, quantity, forwardRic,a,b,sigma,rho,m);
//            double deltaBump = NumericalDelta(optionType, exerciceType, spotBump, modelVol, strike, quantity, forwardRic, a, b, sigma, rho, m);

//            return ((deltaBump - delta));
//        }

//        public double Gamma(double spot, double vol  , int strike, string forwardRic, double quantity)
//        {
//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return 0;
//            }

//            double div = 0;
//            double rate = 0;

//            return ((Math.Exp(-div * data.TimeToExpi) * Nxd1(spot, vol, div, rate, strike, data.TimeToExpi) /vol/spot/Math.Sqrt(data.TimeToExpi)) * quantity * (spot * 0.01) * (spot * 0.01))/0.001;
//        }

//        /// <summary>
//        ////Greeks Vega
//        /// approx div = 0 rate = 0
//        /// </summary>
//        public double Vega(double underlyingPrice, double modelVol, int strike, string forwardRic, double quantity)
//        {
//            MaturityData data;
//            if (!_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return 0;
//                // il faut peut etre appeler build forward
//            }

//            double dividend = 0;
//            double rate = 0;

//            return underlyingPrice * Math.Sqrt(data.TimeToExpi) * Math.Exp(-dividend * data.TimeToExpi) * Nxd1(underlyingPrice, modelVol, dividend, rate, strike, data.TimeToExpi) * quantity * _volBump;
//        }

//        private double Nxd1(double underlyingPrice, double modelvol, double dividend, double rate, int strike,
//                            double timeToExpi)
//        {
//            return 1 / Math.Sqrt(2 * 3.1415926) *
//                   Math.Exp(-0.5 * d1(underlyingPrice, modelvol, dividend, rate, strike, timeToExpi) *
//                            d1(underlyingPrice, modelvol, dividend, rate, strike, timeToExpi));

//        }

//        private double d1(double underlyingPrice, double modelvol, double dividend, double rate, int strike,
//                          double timeToExpi)
//        {
//            return (Math.Log(underlyingPrice / strike) + (rate - dividend + 0.5 * modelvol * modelvol) * timeToExpi) / modelvol /
//                   Math.Sqrt(timeToExpi);
//        }

//        /// <summary>
//        ////Greeks Theta
//        /// approx div = 0 rate = 0
//        /// </summary>
//        public double Theta(string optionType, double underlyingPrice, int strike, double modelVol,
//                            DateTime maturity, int multiplier, int quantity, string forwardRic)
//        {
//            double rate = 0;
//            double dividend = 0;

//            DateTime nextDay = DateTime.Today.AddDays(+1);
//            double timeToExpi = GetTimeToExpiration(DateTime.Today, maturity);
//            double nextTimeToExpi = GetTimeToExpiration(nextDay, maturity);

//            double fairPrice = BlackScholes(optionType, underlyingPrice, strike, rate, dividend, modelVol, timeToExpi);
//            double nextfairPrice = BlackScholes(optionType, underlyingPrice, strike, rate, dividend, modelVol,nextTimeToExpi);
//            return (nextfairPrice - fairPrice) * multiplier * quantity;
//        }

//        public MaturityData BuildForward(DateTime maturity, string forwardId, string futureTtCode)
//        {
//            if (maturity < DateTime.Today)
//            {
//                log.Error($"{forwardId} as expired");
//                return null;
//            }


//            if (_fwdPricingDataDico.ContainsKey(forwardId))
//            {
//                log.Error($"{forwardId} already in dico");
//                return null;
//            }

//            log.Info($"Build forward: {forwardId}");

//            int upStrike;
//            int downStrike;
//            DateTime previousDay = GetPreviousDay(DateTime.Today);
//            double spotClose = _dbManager.GetSpotClose(previousDay, DateTime.Today,futureTtCode);

//            if (spotClose == 0)
//            {
//                var e = new Exception($"{forwardId} closing price as of date:{previousDay} missing ");
//                log.Error(e);
//                throw e;
//            }

//            int roundedPrice = (int)Math.Round(spotClose / _strikeBase, 0) * _strikeBase;
//            if (roundedPrice >= spotClose)
//            {
//                upStrike = roundedPrice;
//                downStrike = roundedPrice - _strikeBase;
//            }
//            else
//            {
//                upStrike = roundedPrice + _strikeBase;
//                downStrike = roundedPrice;
//            }

//            log.Info($"Up strike: {upStrike} Down strike: {downStrike}");
            
//            HistoricalPrice[] closePrices = _dbManager.GetOptionsClosePrice(upStrike, downStrike, maturity,previousDay);


//            double upPutPrice = 0.0;
//            double downPutPrice = 0.0;
//            double upCallPrice = 0.0;
//            double downCallPrice = 0.0;

//            foreach (var item in closePrices)
//            {
//                string optionType = item.Instrument.OptionType;
//                var strike = item.Instrument.Strike;
//                var price = item.ClosePrice;

//                if (optionType == "PUT" && strike == upStrike)
//                {
//                    upPutPrice = price;
//                }
//                else if (optionType == "PUT" && strike == downStrike)
//                {

//                    downPutPrice = price;
//                }
//                else if (optionType == "CALL" && strike == upStrike)
//                {

//                    upCallPrice = price;
//                }
//                else if (optionType == "CALL" && strike == downStrike)
//                {
//                    downCallPrice = price;
//                }
//            }

//            ////check if data are correctly loaded
//            if (upPutPrice == 0 || downPutPrice == 0 || upCallPrice == 0 || downCallPrice == 0)
//            {
//                log.Error("Option close price Missing");
//            }

//            var maturityData = new MaturityData();
//            maturityData.TimeToExpi = GetTimeToExpiration(DateTime.Today, maturity);

//            maturityData.ImpliedDiv = GetImpliedDividend(downStrike, upStrike, spotClose, downCallPrice, downPutPrice, upCallPrice, upPutPrice, maturityData.TimeToExpi);

//            maturityData.ImpliedRate = GetImpliedRate(maturityData.ImpliedDiv, downStrike, spotClose, downCallPrice, downPutPrice, maturityData.TimeToExpi);

//            double fwdClosePrice = GetFwdClosePrice(spotClose, maturityData.ImpliedRate, maturityData.ImpliedDiv,maturityData.TimeToExpi);

//            maturityData.BaseOffset = GetFwdBaseOffset(fwdClosePrice, spotClose);

//            _fwdPricingDataDico.Add(forwardId, maturityData);

//            return maturityData;
//        }

//        public static DateTime GetPreviousDay(DateTime asOfDate)
//        {
//            DateTime previousDay = asOfDate.AddDays(-1);
//            while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
//            {
//                previousDay = previousDay.AddDays(-1);
//            }
//            return previousDay;
//        }

//        public double GetForwardBaseOffset(string forwardRic)
//        {
//            MaturityData data;
//            if (_fwdPricingDataDico.TryGetValue(forwardRic, out data))
//            {
//                return data.BaseOffset;
//            }
//            return -1;
//        }

//        //public string BuildEurexRic(int strike, string optionType, DateTime maturityDate)
//        //{
//        //    string maturityId = OptionLib.UniqueInstance.BuildOptionId(optionType, maturityDate);
//        //    return "STXE" + strike * 10 + maturityId + ".EX";
//        //}

//        public string BuildOptionMarketCode(string productName, string optionType, DateTime maturity, int strike)
//        {
//            string key = productName;

//            if (optionType == "CALL")
//                key += "C";
//            else
//                key += "P";

//            key += maturity.ToString("MMyyyy");
//            key += (strike * 10).ToString("D8");

//            return key;
//        }

//        public string BuildFutureMarketCode(string productName, DateTime maturity)
//        {
//            string ttInstruKey = productName + maturity.Month.ToString("D2") + maturity.Year;

//            return ttInstruKey;
//        }
//    }
//}
