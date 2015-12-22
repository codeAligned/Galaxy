using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using ExcelDna.Integration;
using Galaxy.DatabaseService;
using log4net;
using static System.Math;

namespace Galaxy.PricingService
{
    public static class Option
    {
        private const double _timeBasis = 365;
        private const int _strikeBase = 50;

        private static readonly ILog _logger;
        private static readonly IDbManager _dbManager;

        private static readonly Dictionary<string, double> _forwardCloseDico;

        static Option()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _forwardCloseDico = new Dictionary<string, double>();
            _dbManager = new DbManager();
        }

        [ExcelFunction(Name = "TEST", Description = "Test for excel dna fonction")]
        public static string TestExcelDna(string testString)
        {
            return "Value: " + testString;
        }

        [ExcelFunction(Name = "GREEK.DELTA.CALL", Description = "Delta for call option")]
        public static double CallDelta(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return Exp(-dividend * time) * CumulativeNormalDistribution(D1(spot, strike, time, volatility, riskFreeRate, dividend));
        }

        [ExcelFunction(Name = "GREEK.DELTA.PUT", Description = "Delta for put option")]
        public static double PutDelta(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return -Exp(-dividend * time) * CumulativeNormalDistribution(-D1(spot, strike, volatility, time, riskFreeRate, dividend));
        }

        [ExcelFunction(Name = "GREEK.DELTA", Description = "Delta for option")]
        public static double Delta(string optionType, double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            if (optionType == "CALL")
            {
                return CallDelta(spot, strike, volatility, time, riskFreeRate, dividend);
            }
            if (optionType == "PUT")
            {
                return PutDelta(spot, strike, volatility, time, riskFreeRate, dividend);
            }

            Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
            _logger.Error(e);
            throw e;
        }

        //[ExcelFunction(Name = "GREEK.DELTA.SMILE", Description = "Delta developped by Clement")]
        //public static double DeltaSmile(string optionType, double spot, double strike, double volatility, double time, double[] pointsVol,
        //                                    [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
        //                                    [ExcelArgument(Description = "Optional")] double dividend = 0)
        //{
        //    Derivatives derivatives = new Derivatives(pointsVol.GetLength(0));
        //    if (optionType == "CALL")
        //    {
        //        return CallDelta(spot, strike, volatility, time, riskFreeRate, dividend) + Vega(spot, strike, volatility, time, riskFreeRate, dividend) * derivatives.ComputeDerivative(pointsVol, 1, 0, 25);
        //    }
        //    if (optionType == "PUT")
        //    {
        //        return PutDelta(spot, strike, volatility, time, riskFreeRate, dividend) + Vega(spot, strike, volatility, time, riskFreeRate, dividend) * derivatives.ComputeDerivative(pointsVol, 1, 0, 25);
        //    }

        //    Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
        //    _logger.Error(e);
        //    throw e;
        //}

        /// <summary>
        /// Differentiation
        /// </summary>
        [ExcelFunction(Name = "GREEK.DELTA.STICKY", Description = "Delta par differentiation")]
        public static double DegueulasseDelta(string optionType, double spot,double modelVol, double strike, double time , int qty, double a,double b,double sigma,double rho, double m, double rate = 0, double dividend = 0)
        {
            double spotBump = 0.001;

            double fwdBumpUp = spot * (1 + spotBump);
            double fwdBumpDown = spot * (1 - spotBump);
            double volBumpUp = SviVolatility(strike, fwdBumpUp, a, b, sigma, rho, m);
            double volBumpDown = SviVolatility(strike, fwdBumpDown, a, b, sigma, rho, m);

            double fairBumpUp = BlackScholes(optionType, fwdBumpUp, strike, time, volBumpUp, rate, dividend);
            double fairBumpDown = BlackScholes(optionType, fwdBumpDown, strike, time, volBumpDown, rate, dividend);
            return ((fairBumpUp - fairBumpDown)* qty) / (2 * spotBump * spot);
        }

        [ExcelFunction(Name = "GREEK.THETA.CALL", Description = "Theta for call option")]
        public static double CallTheta(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            double res = -(spot * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) * volatility * Exp(-dividend * time)) / (2 * Sqrt(time)) + dividend * spot * CumulativeNormalDistribution(D1(spot, strike, time, volatility, rate, dividend)) * Exp(-dividend * time) - rate * strike * Exp(-rate * time) * CumulativeNormalDistribution(D2(spot, strike, time, volatility, rate, dividend));
            return res / _timeBasis;
        }

        [ExcelFunction(Name = "GREEK.THETA.PUT", Description = "Theta for put option")]
        public static double PutTheta(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            double res = -(spot * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) * volatility * Exp(-dividend * time)) / (2 * Sqrt(time)) - dividend * spot * CumulativeNormalDistribution(-D1(spot, strike, time, volatility, rate, dividend)) * Exp(-dividend * time) + rate * strike * Exp(-rate * time) * CumulativeNormalDistribution(-D2(spot, strike, time, volatility, rate, dividend));
            return res / _timeBasis;
        }

        [ExcelFunction(Name = "GREEK.THETA", Description = "Theta for option")]
        public static double Theta(string optionType, double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            if (optionType == "CALL")
            {
                return CallTheta(spot, strike, volatility, time, riskFreeRate, dividend);
            }
            if (optionType == "PUT")
            {
                return PutTheta(spot, strike, volatility, time, riskFreeRate, dividend);
            }

            Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
            _logger.Error(e);
            throw e;
        }

        public static double NumTheta(string optionType, double spot, double strike, double volatility, double time, double riskFreeRate = 0, double dividend = 0)
        {
            double nextTime = time - 1/_timeBasis; // get time to expi for day j+1

            if (optionType == "CALL")
            {
                double todayFairPrice = BlackScholesCall(spot, strike, time, volatility, riskFreeRate, dividend);
                double nextDayFairPrice = BlackScholesCall(spot, strike, nextTime, volatility, riskFreeRate, dividend);

                return nextDayFairPrice - todayFairPrice;
            }
            if (optionType == "PUT")
            {
                double todayFairPrice = BlackScholesPut(spot, strike, time, volatility, riskFreeRate, dividend);
                double nextDayFairPrice = BlackScholesPut(spot, strike, nextTime, volatility, riskFreeRate, dividend);

                return nextDayFairPrice - todayFairPrice;
            }

            Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
            _logger.Error(e);
            throw e;
        }

        [ExcelFunction(Name = "GREEK.GAMMA", Description = "Gamma for option")]
        public static double Gamma(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return (ProbabilityDensity(D1(spot, strike, time, volatility, riskFreeRate, dividend)) * Exp(-dividend * time)) / (spot * volatility * Sqrt(time));
        }

        [ExcelFunction(Name = "GREEK.VEGA", Description = "Vega for option")]
        public static double Vega(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            double res = spot * Sqrt(time) * ProbabilityDensity(D1(spot, strike, time, volatility, riskFreeRate, dividend)) * Exp(-dividend * time);
            return res;
        }

        [ExcelFunction(Name = "GREEK.VETA", Description = "Veta for option")]
        public static double Veta(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return Vega(spot, strike, volatility, time, riskFreeRate, dividend) * (dividend + (riskFreeRate - dividend) * D1(spot, strike, time, volatility, riskFreeRate, dividend) / volatility / Sqrt(time) - (1 + D1(spot, strike, time, volatility, riskFreeRate, dividend) * D2(spot, strike, time, volatility, riskFreeRate, dividend)) / 2 / time);
        }

        [ExcelFunction(Name = "GREEK.SPEED", Description = "Speed for option")]
        public static double Speed(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return -Gamma(spot, strike, volatility, time, riskFreeRate, dividend) / spot * (D1(spot, strike, time, volatility, riskFreeRate, dividend) / volatility / Sqrt(time) + 1);
        }

        [ExcelFunction(Name = "GREEK.RHO.CALL", Description = "Rho for Call option")]
        public static double CallRho(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            double res = strike * time * Exp(-riskFreeRate * time) * CumulativeNormalDistribution(D2(spot, strike, time, volatility, riskFreeRate, dividend));
            return res / 100; // % conversion back
        }

        [ExcelFunction(Name = "GREEK.RHO.PUT", Description = "Rho for put option")]
        public static double PutRho(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            double res = -strike * time * Exp(-riskFreeRate * time) * CumulativeNormalDistribution(-D2(spot, strike, time, volatility, riskFreeRate, dividend));
            return res / 100; // % conversion back
        }

        [ExcelFunction(Name = "GREEK.RHO", Description = "Rho for option")]
        public static double Rho(string optionType, double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            if (optionType == "CALL")
            {
                return CallRho(spot, strike, volatility, time, riskFreeRate, dividend);
            }
            if (optionType == "PUT")
            {
                return PutRho(spot, strike, volatility, time, riskFreeRate, dividend);
            }

            Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
            _logger.Error(e);
            throw e;
        }

        [ExcelFunction(Name = "GREEK.VANNA", Description = "Vanna for option")]
        public static double Vanna(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return -Exp(-dividend * time) * ProbabilityDensity(D1(spot, strike, time, volatility, riskFreeRate, dividend)) * D2(spot, strike, time, volatility, riskFreeRate, dividend) / volatility;
        }

        [ExcelFunction(Name = "GREEK.VOLGA", Description = "Volga for option")]
        public static double Volga(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return -spot * Sqrt(time) * D1(spot, strike, time, volatility, riskFreeRate, dividend) * Vanna(spot, strike, volatility, time, riskFreeRate, dividend);
        }

        [ExcelFunction(Name = "GREEK.VOMMA", Description = "Vomma for option")]
        public static double Vomma(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return Volga(spot, strike, volatility, time, riskFreeRate, dividend);
        }

        [ExcelFunction(Name = "GREEK.CHARM", Description = "Charm for option")]
        public static double Charm(string optionType, double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double riskFreeRate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            if (optionType == "CALL")
            {
                return CharmCall(spot, strike, volatility, time, riskFreeRate, dividend);
            }
            if (optionType == "PUT")
            {
                return CharmPut(spot, strike, volatility, time, riskFreeRate, dividend);
            }
            else
            {
                Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
                _logger.Error(e);
                throw e;
            }
        }

        [ExcelFunction(Name = "GREEK.CHARM.CALL", Description = "charm for Call")]
        public static double CharmCall(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")]double rate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            return dividend * Exp(-dividend * time) * CumulativeNormalDistribution(D1(spot, strike, time, volatility, rate, dividend)) - Exp(-dividend * time) * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) * (2 * (rate - dividend) * time - D2(spot, strike, time, volatility, rate, dividend) * volatility * Sqrt(time)) / (2 * time * volatility * Sqrt(time));
        }

        [ExcelFunction(Name = "GREEK.CHARM.PUT", Description = "charm for Put")]
        public static double CharmPut(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            return -dividend * Exp(-dividend * time) * CumulativeNormalDistribution(-D1(spot, strike, time, volatility, rate, dividend)) - Exp(-dividend * time) * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) * (2 * (rate - dividend) * time - D2(spot, strike, time, volatility, rate, dividend) * volatility * Sqrt(time)) / (2 * time * volatility * Sqrt(time));
        }

        [ExcelFunction(Name = "GREEK.COLOR", Description = "color for option")]
        public static double Color(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            return -Exp(-dividend * time) * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) / (volatility * spot * Sqrt(time)) * (dividend + (rate - dividend) * D1(spot, strike, time, volatility, rate, dividend) / (volatility * Sqrt(time)) + (1 - D1(spot, strike, time, volatility, rate, dividend) * D2(spot, strike, time, volatility, rate, dividend)) / 2 / time);
        }

        [ExcelFunction(Name = "GREEK.ULTIMA", Description = "ultima for option")]
        public static double Ultima(double spot, double strike, double volatility, double time,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            return -Exp(-dividend * time) * ProbabilityDensity(D1(spot, strike, time, volatility, rate, dividend)) * spot * Sqrt(time) * D2(spot, strike, time, volatility, rate, dividend) * D1(spot, strike, time, volatility, rate, dividend) / volatility / volatility * (D2(spot, strike, time, volatility, rate, dividend) * D1(spot, strike, time, volatility, rate, dividend) - D1(spot, strike, time, volatility, rate, dividend) / D2(spot, strike, time, volatility, rate, dividend) - D2(spot, strike, time, volatility, rate, dividend) / D1(spot, strike, time, volatility, rate, dividend) - 1);
        }

        [ExcelFunction(Name = "TIME_TO_EXPI", Description = "Return nb of days between maturity and today")]
        public static double GetTimeToExpiration(DateTime asOfDate, DateTime maturity)
        {
            return (maturity - asOfDate).TotalDays / _timeBasis;
        }

        [ExcelFunction(Name = "BLACKSCHOLES.CALL", Description = "BlackSholes Formula for Call")]
        public static double BlackScholesCall(double spot, double strike, double time, double voltatility,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            return spot * CumulativeNormalDistribution(D1(spot, strike, time, voltatility, rate, dividend)) - strike * Exp(-rate * time) * CumulativeNormalDistribution(D2(spot, strike, time, voltatility, rate, dividend));
        }

        [ExcelFunction(Name = "BLACKSCHOLES.PUT", Description = "BlackSholes Formula for Put")]
        public static double BlackScholesPut(double spot, double strike, double time, double voltatility,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")]double dividend = 0)
        {
            return -spot * CumulativeNormalDistribution(-D1(spot, strike, time, voltatility, rate, dividend)) + strike * Exp(-rate * time) * CumulativeNormalDistribution(-D2(spot, strike, time, voltatility, rate, dividend));
        }

        [ExcelFunction(Name = "BLACKSCHOLES.OPTION", Description = "BlackSholes Formula")]
        public static double BlackScholes(string optionType, double spot, double strike, double time, double voltatility,
                                            [ExcelArgument(Description = "Optional")] double rate = 0,
                                            [ExcelArgument(Description = "Optional")] double dividend = 0)
        {
            if (optionType == "CALL")
            {
                return BlackScholesCall(spot, strike, time, voltatility, rate, dividend);
            }
            if (optionType == "PUT")
            {
                return BlackScholesPut(spot, strike, time, voltatility, rate, dividend);
            }

            Exception e = new Exception("Wrong optionType valide type: PUT,CALL");
            _logger.Error(e);
            throw e;
        }

        private static double D1(double spot, double strike, double time, double volatility, double riskFreeRate, double dividend)
        {
            return (Log(spot / strike) + (riskFreeRate - dividend + Pow(volatility, 2) / 2) * time) / (volatility * Sqrt(time));
        }

        private static double D2(double spot, double strike, double time, double volatility, double riskFreeRate, double dividend)
        {
            return (Log(spot / strike) + (riskFreeRate - dividend + Pow(volatility, 2) / 2) * time) / (volatility * Sqrt(time)) - volatility * Sqrt(time);
        }

        private static double CumulativeNormalDistribution(double z)
        {
            // constants
            const double a1 = 0.254829592;
            const double a2 = -0.284496736;
            const double a3 = 1.421413741;
            const double a4 = -1.453152027;
            const double a5 = 1.061405429;
            const double p = 0.3275911;

            // Save the sign of z
            int sign = 1;
            if (z < 0)
            {
                sign = -1;
            }

            z = Abs(z) / Sqrt(2.0);

            // A&S formula 
            double t = 1.0 / (1.0 + p * z);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Exp(-z * z);

            return 0.5 * (1.0 + sign * y);
        }

        private static double ProbabilityDensity(double nb, double standardDeviation = 1, double mean = 0)
        {
            return (1 / (standardDeviation * Sqrt(2 * PI))) * Exp(-Pow((nb - mean) / standardDeviation, 2) / 2);
        }


        [ExcelFunction(Name = "BLACKSCHOLES.VOL", Description = " Return blackScholes implied volatility by bisection ")]
        public static double BlackScholesVol( double optionPrice, double spotPrice,double strike, string optionType, double time,
                                                    [ExcelArgument(Description = "Optional")] double rate = 0,
                                                    [ExcelArgument(Description = "Optional")] double dividend = 0,
                                                    [ExcelArgument(Description = "Optional")] double accuracy = 0.0001,
                                                    [ExcelArgument(Description = "Optional")] double maxVol = 1,
                                                    [ExcelArgument(Description = "Optional")] double minVol = 0)
        {

            if (optionPrice == 0 || spotPrice == 0)
            {
                return 0;
            }

            double res;
            double mid;
            int maxLoop = 100;
            int i = 0;

            do
            {
                mid = (maxVol + minVol) / 2;
                res = BlackScholes(optionType, spotPrice, strike, time, mid, rate, dividend);

                if (res > optionPrice)
                {
                    maxVol = mid;
                }
                else if (res < optionPrice)
                {
                    minVol = mid;
                }
                i++;

                if (i > maxLoop)
                    return 0;

            } while (Abs(res - optionPrice) > accuracy);

            return mid;
        }

        [ExcelFunction(Name = "PREVIOUS_WEEKDAY", Description = "Return previous week day")]
        public static DateTime PreviousWeekDay(DateTime asOfDate)
        {
            DateTime previousDay = asOfDate.AddDays(-1);
            while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
            {
                previousDay = previousDay.AddDays(-1);
            }
            return previousDay;
        }

        /// <summary>
        /// Return future maturity code
        /// </summary>
        public static string BuildForwardId(string prefix, DateTime maturityDate)
        {
            int value = maturityDate.Month;
            string letter = "";
            switch (value)
            {
                case 1: { letter = "F"; break; }
                case 2: { letter = "G"; break; }
                case 3: { letter = "H"; break; }
                case 4: { letter = "J"; break; }
                case 5: { letter = "K"; break; }
                case 6: { letter = "M"; break; }
                case 7: { letter = "N"; break; }
                case 8: { letter = "Q"; break; }
                case 9: { letter = "U"; break; }
                case 10: { letter = "V"; break; }
                case 11: { letter = "X"; break; }
                case 12: { letter = "Z"; break; }
                default: { letter = "?"; break; }
            }

            string year = maturityDate.Year.ToString();
            char yearNb = year[3];

            return prefix + letter + yearNb;
        }

        [ExcelFunction(Name = "GetNextFutureTtCode", IsHidden = true)]
        public static string GetNextFutureTtCode(string prefix, DateTime OptionMaturity)
        {
            int month = OptionMaturity.Month;
            string res = "";
            switch (month)
            {
                case 1:
                case 2:
                case 3: { res = "03"; break; }
                case 4:
                case 5:
                case 6: { res = "06"; break; }
                case 7:
                case 8:
                case 9: { res = "09"; break; }
                case 10:
                case 11:
                case 12: { res = "12"; break; }
                default: { res = "?"; break; }
            }
            int year = OptionMaturity.Year;
            return prefix + res + year;
        }

        [ExcelFunction(Name = "GetOptionTtCode", IsHidden = true)]
        public static string GetOptionTtCode(string productName, string optionType, DateTime maturity, int strike)
        {
            string key = productName;

            if (optionType == "CALL")
                key += "C";
            else
                key += "P";

            key += maturity.ToString("MMyyyy");
            key += (strike * 10).ToString("D8");

            return key;
        }

        /// <summary>
        /// Return option maturity code
        /// Option type: "CALL" or "PUT"
        /// </summary>
        [ExcelFunction(Name = "BuildOptionId", IsHidden = true)]
        public static string BuildOptionId(string optionType, DateTime maturityDate, string prefix = "", string suffix = "")
        {
            int value = maturityDate.Month;
            string letter = "";

            if (optionType == "CALL")
            {
                switch (value)
                {
                    case 1: { letter = "A"; break; }
                    case 2: { letter = "B"; break; }
                    case 3: { letter = "C"; break; }
                    case 4: { letter = "D"; break; }
                    case 5: { letter = "E"; break; }
                    case 6: { letter = "F"; break; }
                    case 7: { letter = "G"; break; }
                    case 8: { letter = "H"; break; }
                    case 9: { letter = "I"; break; }
                    case 10: { letter = "J"; break; }
                    case 11: { letter = "K"; break; }
                    case 12: { letter = "L"; break; }
                    default: { letter = "?"; break; }
                }
            }
            else if (optionType == "PUT")
            {
                switch (value)
                {
                    case 1: { letter = "M"; break; }
                    case 2: { letter = "N"; break; }
                    case 3: { letter = "O"; break; }
                    case 4: { letter = "P"; break; }
                    case 5: { letter = "Q"; break; }
                    case 6: { letter = "R"; break; }
                    case 7: { letter = "S"; break; }
                    case 8: { letter = "T"; break; }
                    case 9: { letter = "U"; break; }
                    case 10: { letter = "V"; break; }
                    case 11: { letter = "W"; break; }
                    case 12: { letter = "X"; break; }
                    default: { letter = "?"; break; }
                }
            }

            string year = maturityDate.Year.ToString();
            char yearNb = year[3];

            return prefix + letter + yearNb + suffix;
        }

        /// <summary>
        /// convert future ttcode to future ID (Reuters)
        /// </summary>
        [ExcelFunction(Name = "ConvertTtCodeToId", IsHidden = true)]
        public static string ConvertTtCodeToId(string ttCode, string prefix)
        {
            int month = int.Parse(ttCode.Substring(4, 2));
            int year = int.Parse(ttCode.Substring(6, 4));
            DateTime maturity = new DateTime(year, month, 1);

            return BuildForwardId(prefix, maturity);
        }

        /// <summary>
        /// FittingModel volatility model,   a=hauteur, b=backbone, rho=rotation, sigma=convexe
        /// </summary>
        [ExcelFunction(Name = "ComputeSviVol", IsHidden = true)]
        public static double ComputeSviVol(double moneyness, double a, double b, double sigma, double rho, double m)
        {
            return Sqrt(Abs((a + b * (rho * (moneyness - m) + Sqrt((moneyness - m) * (moneyness - m) + sigma * sigma))) ));
        }

        /// <summary>
        /// FittingModel volatility model
        /// </summary>
        [ExcelFunction(Name = "VOLATILITY.SVI", Description = "Compute svi volatility")]
        public static double SviVolatility(double strike, double spot, double a, double b, double sigma, double rho, double m)
        {
            double moneyness = Log(strike / spot);
            return ComputeSviVol(moneyness, a, b, sigma, rho, m);
        }
        [ExcelFunction(Name = "VOLATILITY.PARAM.A")]
        public static void GetParamA()
        {
            string conString = "Data Source = VPS210729; Initial Catalog = UatDb; User ID = sa; Password = Phy14!";
            using (SqlConnection connection = new SqlConnection(conString))
            {
                connection.Open();
                //
                // The SqlCommand should be created inside a using statement.
                // ... It receives the SQL statement as the first argument.
                // ... It receives the connection object as the second argument.
                // ... The SQL text only works with a specific database.
                //
                using (SqlCommand command = new SqlCommand("select * from VolParam Where MaturityDate = '2016-06-17'", connection))
                {
                    //
                    // Instance methods can be used on the SqlCommand instance.
                    // ... These read data from executing the command.
                    //
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string res = reader.GetValue(i).ToString();

                                Console.WriteLine(res);
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }
        }
    
    //DbManager db = new DbManager();
    //VolParam Param = db.GetVolParams("OESX", new DateTime(2016,01,15));
    //return Param.A;
    //  return 999;


        /// <summary>
        /// get corresponding forward price from future price
        /// </summary>
        [ExcelFunction(Name = "GetForwardClose", IsHidden = true)]
        public static double GetForwardClose(string forwardId, DateTime maturity, double futureClose)
        {
            if (maturity < DateTime.Today)
            {
                _logger.Error("Maturity as expired");
                return 0;
            }

            if (futureClose == 0)
            {
                _logger.Error("Future close can't be 0");
                return 0;
            }

            if (_forwardCloseDico.ContainsKey(forwardId))
            {
                return _forwardCloseDico[forwardId];
            }

            // high an low strike are strike which frame futureClose 
            int highStrike;
            int lowStrike;

            int roundedPrice = (int)Round(futureClose / _strikeBase, 0) * _strikeBase;
            if (roundedPrice >= futureClose)
            {
                highStrike = roundedPrice;
                lowStrike = roundedPrice - _strikeBase;
            }
            else
            {
                highStrike = roundedPrice + _strikeBase;
                lowStrike = roundedPrice;
            }

            _logger.Info($"Up strike: {highStrike} Down strike: {lowStrike}");
            DateTime previousDay = PreviousWeekDay(DateTime.Today);

            double highPutPrice = _dbManager.GetOptionsClosePrice(highStrike, "PUT", maturity, previousDay);
            double lowPutPrice = _dbManager.GetOptionsClosePrice(lowStrike, "PUT", maturity, previousDay);
            double highCallPrice = _dbManager.GetOptionsClosePrice(highStrike, "CALL", maturity, previousDay);
            double lowCallPrice = _dbManager.GetOptionsClosePrice(lowStrike, "CALL", maturity, previousDay);

            ////check if data are correctly loaded
            if (highPutPrice == 0 || lowPutPrice == 0 || highCallPrice == 0 || lowCallPrice == 0)
            {
                return 0;
            }

            double time = GetTimeToExpiration(DateTime.Today, maturity);
            double forwardClose = GetForwardClosePrice(lowStrike, highStrike, lowCallPrice, lowPutPrice, highCallPrice, highPutPrice, futureClose, time);
            _forwardCloseDico.Add(forwardId, forwardClose);

            return forwardClose;
        }

        /// <summary>
        /// Compute Forward Close price using interest rate and dividend
        /// </summary>
        [ExcelFunction(Name = "GetForwardClosePrice", IsHidden = true)]
        private static double GetForwardClosePrice(double lowStrike, double highStrike, double lowCallPrice, double lowPutPrice, double highCallPrice, double highPutPrice, double spotClose, double time)
        {
            double impliedDiv = GetImpliedDividend(lowStrike, highStrike, spotClose, lowCallPrice, lowPutPrice, highCallPrice, highPutPrice, time);
            double impliedRate = GetImpliedRate(impliedDiv, lowStrike, spotClose, lowCallPrice, lowPutPrice, time);

            return spotClose * Exp((impliedRate - impliedDiv) * time);
        }

        /// <summary>
        /// Compute dividend based on strike and Close price of options ATM
        /// </summary>
        [ExcelFunction(Name = "GetImpliedDividend", IsHidden = true)]
        private static double GetImpliedDividend(double lowStrike, double highStrike, double closeSpot, double lowCallPrice, double lowPutPrice, double highCallPrice, double highPutPrice, double time)
        {

            if (highCallPrice > lowCallPrice || highPutPrice < lowPutPrice)
            {
                Exception e = new Exception("Wrong Close prices");
                _logger.Error(e);
                throw e;
            }

            return -Log((highStrike * (lowCallPrice - lowPutPrice) - lowStrike * (highCallPrice - highPutPrice)) / (closeSpot * (highStrike - lowStrike))) / time;
        }

        /// <summary>
        /// Compute Interest Rate based on strike and Close price of options ATM
        /// </summary>
        [ExcelFunction(Name = "GetImpliedRate", IsHidden = true)]
        private static double GetImpliedRate(double dividend, double strike, double closeSpot, double callPrice, double putPrice, double time)
        {
            return -Log((closeSpot * Exp(-dividend * time) - callPrice + putPrice) / strike) / time;
        }
    }
}
