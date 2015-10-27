//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Galaxy.PricingService
//{
//    class OptionLib_backup_
//    {
//        private static double h;
//        private static double alpha; static double beta; static double lambda; static double lambdaprime; static double alphadivh;
//        public static int basis = 365; // basis would be define using global parameter 360, 365 or 352. default value is 365
//        /*********************************************************************************************************************************************************************/
//        // MAIN FUNCTIONS

//        // Black and scholes pricing model for european vanilla options
//        public static double BlackScholes(string callPut, double spot, double strike, double t, double rate, double div, double vol)
//        {
//            if (callPut == "C")
//                return (European_Call(spot, strike, t, rate, div, vol));
//            if (callPut == "P")
//                return (European_Put(spot, strike, t, rate, div, vol));

//            return (0);
//        }

//        //Boron-adesi & whaley Pricing model for american vanilla options
//        public static double BaronAdesiWhaley(string callPut, double spot, double strike, double t, double rate, double div, double vol)
//        {
//            if (callPut == "C")
//                return (BWAmerican_Call(spot, strike, t, rate, div, vol));
//            if (callPut == "P")
//                return (BWAmerican_Put(spot, strike, t, rate, div, vol));

//            return (0);
//        }

//        //Bjerksund & Stensland Pricing model for american vanilla options
//        public static double BjerksundStensland(string CallPut, double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double functionReturnValue = 0;

//            double Binfinity, BB, dt, ht, I, alpha, beta, b1, rr = 0, dd = 0, dd2 = 0, assetnew, drift, v2, z = 0;

//            if (CallPut == "C")
//            {
//                z = 1;
//                rr = rate;
//                dd2 = div;
//            }
//            else if (CallPut == "P")
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
//                if (CallPut == "C")
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
//                    functionReturnValue = alpha * Math.Pow(spot, beta) - alpha * Phi(spot, t, beta, I, I, rr, z * drift, vol) + Phi(spot, t, 1, I, I, rr, z * drift, vol) - Phi(spot, t, 1, strike, I, rr, z * drift, vol) - strike * Phi(spot, t, 0, I, I, rr, z * drift, vol) + strike * Phi(spot, t, 0, strike, I, rr, z * drift, vol);
//                }
//            }
//            return functionReturnValue;

//        }

//        // Ju Zhong pricing model for american vanilla options
//        public static double JuZhong(string callPut, double spot, double strike, double rate, double div, double t, double vol)
//        {
//            double functionReturnValue = 0;


//            double Sstar = 0;
//            double VeS = 0;
//            double VeSstar = 0;
//            double d1Sstar = 0;
//            double d2Sstar = 0;
//            double dVeSstar = 0;
//            double ha = 0;
//            double c = 0;
//            double b = 0;
//            double chi = 0;
//            double Ssolve = 0;
//            double c1 = 0;
//            double c2 = 0;
//            double c3 = 0;
//            double c4 = 0;
//            double callPutAdjust = 0;


//            if (callPut == "C")
//            {
//                callPutAdjust = 1;
//            }
//            if (callPut == "P")
//            {
//                callPutAdjust = -1;
//            }


//            h = 1 - Math.Exp(-rate * t);
//            alpha = 2 * rate / (Math.Pow(vol, 2));
//            beta = 2 * (rate - div) / (Math.Pow(vol, 2));

//            if (rate == 0)
//            {
//                alphadivh = 2 / (t * Math.Pow(vol, 2));
//                lambda = (-(beta - 1) + callPutAdjust * Math.Sqrt(Math.Pow((beta - 1), 2) + 8 / (t * Math.Pow(vol, 2)))) / 2;
//            }
//            else
//            {
//                alphadivh = alpha / h;
//                lambda = (1 - beta + callPutAdjust * Math.Sqrt(Math.Pow((beta - 1), 2) + 4 * alphadivh)) / 2;
//                lambdaprime = -callPutAdjust * alpha / ((Math.Pow(h, 2)) * Math.Sqrt(Math.Pow((beta - 1), 2) + 4 * alphadivh));
//            }

//            Ssolve = spot;

//            Sstar = NewtonRaphsonOpt(callPut, Ssolve, strike, rate, div, t, vol);

//            VeS = BlackScholes(callPut, spot, strike, rate, div, t, vol);
//            VeSstar = BlackScholes(callPut, Sstar, strike, rate, div, t, vol);
//            d1Sstar = d1(Sstar, vol, div, rate, strike, t);
//            d2Sstar = d1Sstar - vol * Math.Sqrt(t);
//            ha = callPutAdjust * (Sstar - strike) - VeSstar;

//            if (rate == 0)
//            {
//                b = -2 / ((Math.Pow(vol, 4)) * (Math.Pow(t, 2)) * (Math.Pow((beta - 1), 2) + 8 / ((Math.Pow(vol, 2)) * t)));
//                c1 = -callPutAdjust / Math.Sqrt(Math.Pow((beta - 1), 2) + 8 / ((Math.Pow(vol, 2)) * t));
//                c2 = Sstar * (Math.Exp(-(Math.Pow(d1Sstar, 2)) / 2) / Math.Sqrt(2 * Math.PI)) * Math.Exp(-div * t) / (ha * vol * Math.Sqrt(t));
//                c3 = callPutAdjust * 2 * div * Sstar * NormSDist(callPutAdjust * d1Sstar) * Math.Exp(-div * t) / (ha * (Math.Pow(vol, 2)));
//                c4 = 2 / (t * Math.Pow(vol, 2)) + 2 * b;
//                c = c1 * (c2 - c3 + c4);

//            }
//            else
//            {
//                dVeSstar = Sstar * (Math.Exp(-(Math.Pow(d1Sstar, 2)) / 2) / Math.Sqrt(2 * Math.PI)) * vol * Math.Exp((rate - div) * t) / (2 * rate * Math.Sqrt(t)) - callPutAdjust * rate * Sstar * NormSDist(callPutAdjust * d1Sstar) * Math.Exp((rate - div) * t) / rate + callPutAdjust * strike * NormSDist(callPutAdjust * d2Sstar);
//                c = -(dVeSstar / ha + 1 / h + lambdaprime / (2 * lambda + beta - 1)) * (1 - h) * alpha / (2 * lambda + beta - 1);
//                b = (1 - h) * alpha * lambdaprime / (2 * (2 * lambda + beta - 1));
//            }

//            chi = b * (Math.Pow((Math.Log(spot / Sstar)), 2)) + c * Math.Log(spot / Sstar);

//            if (callPutAdjust * (Sstar - spot) > 0)
//            {
//                functionReturnValue = VeS + ha * (Math.Pow((spot / Sstar), lambda)) / (1 - chi);
//            }
//            else
//            {
//                functionReturnValue = callPutAdjust * (spot - strike);
//            }
//            return functionReturnValue;

//        }

//        //Delta per option
//        public static double AnalyticalDelta(string callPut, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            if (callPut == "C")
//                return (CallDelta(spot, vol, div, rate, strike, t));
//            if (callPut == "P")
//                return (PutDelta(spot, vol, div, rate, strike, t));

//            return (0);
//        }
//        //Gamma per option
//        public static double AnalyticalGamma(string callPut, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            double bump = 0.01;

//            if (callPut == "C")
//                return (CallGamma(spot, vol, div, rate, strike, t) / (bump * bump));
//            if (callPut == "P")
//                return (PutGamma(spot, vol, div, rate, strike, t) / (bump * bump));

//            return (0);
//        }

//        //Vega per option
//        public static double AnalyticalVega(string callPut, double bump, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            if (callPut == "C")
//                return (CallVega(spot, vol, div, rate, strike, t) / (1 / bump));
//            if (callPut == "P")
//                return (PutVega(spot, vol, div, rate, strike, t) / (1 / bump));

//            return (0);
//        }

//        //Theta per day per option
//        public static double AnalyticalTheta(string callPut, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            if (callPut == "C")
//                return (CallTheta(spot, vol, div, rate, strike, t) / basis);
//            if (callPut == "P")
//                return (PutTheta(spot, vol, div, rate, strike, t) / basis);

//            return (0);
//        }

//        //Rho
//        public static double AnalyticalRho(string callPut, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            if (callPut == "C")
//                return (CallRho(spot, vol, div, rate, strike, t));
//            if (callPut == "P")
//                return (PutRho(spot, vol, div, rate, strike, t));

//            return (0);
//        }

//        //Speed
//        public static double AnalyticalSpeed(string callPut, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            if (callPut == "C")
//                return (CallSpeed(spot, vol, div, rate, strike, t));
//            if (callPut == "P")
//                return (PutSpeed(spot, vol, div, rate, strike, t));

//            return (0);
//        }

//        //Implied Volatilitied for market prices Put and Call OTM & ATM exclusively
//        public static double ImpliedVolBisection(double accuracy, double optTargetPrice, double firstGuess, double lowerBound, double upperbound, double spotPrice, double rate, double div, double strike, double t,
//string callPut, string amEur)
//        {
//            double functionReturnValue = 0;


//            double fFirstGuess = 0;
//            double fUb = 0;
//            double fLb = 0;
//            double mid = 0;
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

//            if (amEur == "E")
//            {
//                fFirstGuess = BlackScholes(callPut, spotPrice, strike, t, rate, div, firstGuess);
//                fLb = BlackScholes(callPut, spotPrice, strike, t, rate, div, lowerBound);
//                fUb = BlackScholes(callPut, spotPrice, strike, t, rate, div, upperbound);
//            }

//            if (amEur == "A")
//            {
//                fFirstGuess = BjerksundStensland(callPut, spotPrice, strike, t, rate, div, firstGuess);
//                fLb = BjerksundStensland(callPut, spotPrice, strike, t, rate, div, lowerBound);
//                fUb = BjerksundStensland(callPut, spotPrice, strike, t, rate, div, upperbound);
//            }
//            diff = optTargetPrice - fFirstGuess;


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
//                mid = (upperbound + lowerBound) / 2;
//                if (amEur == "E")
//                    fMid = BlackScholes(callPut, spotPrice, strike, t, rate, div, mid);
//                if (amEur == "A")
//                    fMid = BjerksundStensland(callPut, spotPrice, strike, t, rate, div, mid);

//                if (Math.Abs(fMid - optTargetPrice) <= accuracy)
//                {
//                    functionReturnValue = mid;
//                    return functionReturnValue;
//                }
//                else
//                {
//                    diff = optTargetPrice - fMid;
//                    if (diff < 0)
//                    {
//                        upperbound = mid;
//                    }
//                    else if (diff > 0)
//                    {
//                        lowerBound = mid;
//                    }
//                }
//            }
//            return functionReturnValue;

//        }



//        /*********************************************************************************************************************************************************************/

//        //BS Model European Call 
//        public static double European_Call(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double dt = 0;
//            double d1 = 0;
//            double d2 = 0;
//            double Nd1 = 0;
//            double Nd2 = 0;

//            dt = vol * Math.Sqrt(t);
//            d1 = (Math.Log(spot / strike) + (rate - div + 0.5 * Math.Pow(vol, 2)) * t) / dt;
//            d2 = d1 - dt;

//            Nd1 = NormSDist(d1);
//            Nd2 = NormSDist(d2);

//            return (Math.Exp(-div * t) * spot * Nd1) - (strike * Math.Exp(-rate * t) * Nd2);
//        }

//        //BS Model European Put
//        public static double European_Put(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double dt = 0;
//            double d1 = 0;
//            double d2 = 0;
//            double NNd1 = 0;
//            double NNd2 = 0;

//            dt = vol * Math.Sqrt(t);
//            d1 = (Math.Log(spot / strike) + (rate - div + 0.5 * Math.Pow(vol, 2)) * t) / dt;
//            d2 = d1 - dt;


//            NNd1 = NormSDist(-d1);
//            NNd2 = NormSDist(-d2);

//            return (-spot * Math.Exp(-div * t) * NNd1) + (strike * Math.Exp(-rate * t) * NNd2);
//        }

//        // Black & Scholes Model for european options

//        /*
//    // Black & Scholes Model for european options
//   public static double BlackSholes(string callPut, double spot, double strike ,double time,  double rate, double div, double vol)
//   {
//       double dt,d1, d2, Nd1, Nd2, BSPrice;
        
//       if(vol == 0)
//            return(0);

//       if (callPut=="P")
//           vol=(-1)*vol;

//       dt = vol * Math.Sqrt(time);
//       d1 = (Math.Log(spot / strike) + (rate - div + 0.5 * vol*vol) * time) / dt;
//       d2 = d1 - dt;
       
//       Nd1 = NormSDist (d1);
//       Nd2 = NormSDist(d2);

//       BSPrice=(Math.Exp(-div * time) * spot * Nd1) - (strike * Math.Exp(-rate * time) * Nd2);

//       if (callPut=="P") 
//           BSPrice=(-1)*BSPrice;

//       return (BSPrice);
//   }
//         * */


//        // BA Whaley model for american Call
//        public static double BWAmerican_Call(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double functionReturnValue = 0;

//            double dt = 0;
//            double a2 = 0;
//            double b = 0;
//            double A = 0;
//            double h = 0;
//            double L1 = 0;
//            double L2 = 0;
//            double d1 = 0;
//            double Sc = 0;

//            b = 2 * (rate - div) / Math.Pow(vol, 2) - 1;
//            A = 2 * rate / Math.Pow(vol, 2);
//            h = 1 - Math.Exp(-rate * t);
//            L1 = (-b - Math.Sqrt(Math.Pow(b, 2) + 4 * A / h)) / 2;
//            L2 = (-b + Math.Sqrt(Math.Pow(b, 2) + 4 * A / h)) / 2;
//            dt = vol * Math.Sqrt(t);

//            if (div == 0)
//            {
//                functionReturnValue = European_Call(spot, strike, t, rate, div, vol);
//            }
//            else
//            {
//                Sc = Bisectional_Call(spot, strike, t, rate, div, vol);
//                d1 = (Math.Log(Sc / strike) + (rate - div + 0.5 * Math.Pow(vol, 2)) * t) / dt;
//                a2 = (1 - Math.Exp(-div * t) * NormSDist(d1)) * (Sc / L2);
//                if (spot < Sc)
//                {
//                    functionReturnValue = European_Call(spot, strike, t, rate, div, vol) + a2 * Math.Pow((spot / Sc), L2);
//                }
//                else
//                {
//                    functionReturnValue = spot - strike;
//                }
//            }
//            return functionReturnValue;

//        }

//        // BA Whaley model for american Put
//        public static double BWAmerican_Put(double spot, double strike, double t, double rate, double div, double vol)
//        {
//            double functionReturnValue = 0;

//            double dt = 0;
//            double A1 = 0;
//            double b = 0;
//            double A = 0;
//            double h = 0;
//            double L1 = 0;
//            double L2 = 0;
//            double d1 = 0;
//            double Sp = 0;

//            b = 2 * (rate - div) / Math.Pow(vol, 2) - 1;
//            A = 2 * rate / Math.Pow(vol, 2);
//            h = 1 - Math.Exp(-rate * t);
//            L1 = (-b - Math.Sqrt(Math.Pow(b, 2) + 4 * A / h)) / 2;
//            L2 = (-b + Math.Sqrt(Math.Pow(b, 2) + 4 * A / h)) / 2;
//            dt = vol * Math.Sqrt(t);

//            Sp = Bisectional_Put(spot, strike, t, rate, div, vol);
//            d1 = (Math.Log(Sp / strike) + (rate - div + Math.Pow(vol, 2) / 2) * t) / dt;
//            A1 = -(1 - Math.Exp(-div * t) * NormSDist(-d1)) * (Sp / L1);

//            if (spot > Sp)
//            {
//                functionReturnValue = European_Put(spot, strike, t, rate, div, vol) + A1 * Math.Pow((spot / Sp), L1);
//            }
//            else
//            {
//                functionReturnValue = strike - spot;
//            }
//            return functionReturnValue;
//        }

//        // Quadratic approach for BA Whaley pricing model for calls
//        public static double Bisectional_Call(double S, double X, double t, double r, double D, double V)
//        {
//            double dt, Sx = 0, Su, Sl, Suu, d1, d2, L2, c1, c2, b, A, h, N1, N2, E_st, IterationCountE;

//            dt = V * Math.Sqrt(t);
//            d1 = (Math.Log(S / X) + (r - D + Math.Pow(V, 2) / 2) * t) / dt;
//            d2 = d1 - dt;

//            N1 = NormSDist(d1);
//            N2 = NormSDist(d2);

//            E_st = S * Math.Exp((r - D) * t) * N1 / N2;

//            Su = E_st;
//            // Guess the high bound
//            Sl = S;
//            // Guess the low bound
//            Suu = Su;

//            b = 2 * (r - D) / Math.Pow(V, 2) - 1;
//            A = 2 * r / Math.Pow(V, 2);
//            h = 1 - Math.Exp(-r * t);
//            L2 = (-b + Math.Sqrt(Math.Pow(b, 2) + 4 * A / h)) / 2;
//        Start_Iteration:


//            IterationCountE = 1E-09;
//            while ((Su - Sl) > IterationCountE)
//            {
//                Sx = (Su + Sl) / 2;

//                d1 = (Math.Log(Sx / X) + (r - D + Math.Pow(V, 2) / 2) * t) / dt;
//                c1 = Sx - X;
//                c2 = European_Call(Sx, X, t, r, D, V) + (1 - Math.Exp(-D * t) * NormSDist(d1)) * Sx / L2;

//                if ((c2 - c1) > 0)
//                {
//                    Sl = Sx;
//                }
//                else
//                {
//                    Su = Sx;
//                }

//            }

//            if ((Math.Round(Sx, 4) == Math.Round(Suu, 4)))
//            {
//                Su = 2 * Suu;
//                Suu = Su;
//                goto Start_Iteration;
//            }

//            return Sx;
//        }

//        // Quadratic approach for BA Whaley pricing model for puts
//        public static double Bisectional_Put(double S, double X, double t, double r, double D, double V)
//        {

//            double dt, Sx = 0, Su, Sl, Sll, d1, d2, P1, P2, b, A, h, NN1, NN2, E_st, L1, IterationCountE;

//            dt = V * Math.Sqrt(t);
//            d1 = (Math.Log(S / X) + (r - D + Math.Pow(V, 2) / 2) * t) / dt;
//            d2 = d1 - dt;

//            NN1 = NormSDist(-d1);
//            NN2 = NormSDist(-d2);

//            E_st = S * Math.Exp((r - D) * t) * NN1 / NN2;

//            Sl = 0;
//            // Guess the low bound
//            Su = S;
//            // Guess the high bound
//            Sll = Sl;

//            b = 2 * (r - D) / Math.Pow(V, 2);
//            A = 2 * r / Math.Pow(V, 2);
//            h = 1 - Math.Exp(-r * t);
//            L1 = (-(b - 1) - Math.Sqrt(Math.Pow((b - 1), 2) + 4 * A / h)) / 2;


//            IterationCountE = 1E-09;
//            while ((Su - Sl) > IterationCountE)
//            {
//                Sx = (Su + Sl) / 2;

//                d1 = (Math.Log(Sx / X) + ((r - D) + Math.Pow(V, 2) / 2) * t) / dt;
//                P1 = X - Sx;
//                P2 = European_Put(Sx, X, t, r, D, V) - (1 - Math.Exp(-D * t) * NormSDist(-d1)) * Sx / L1;

//                if ((P2 - P1) > 0)
//                    Su = Sx;
//                else
//                    Sl = Sx;
//            }

//            return Sx;
//        }


//        //Phi function used into BjerksundStensland pricing model
//        public static double Phi(double spot, double t, double gamma, double h, double i, double r, double a, double V)
//        {
//            double Lambda, k, dd, dt;

//            dt = V * Math.Sqrt(t);
//            Lambda = (-r + gamma * a + 0.5 * gamma * (gamma - 1) * Math.Pow(V, 2)) * t;
//            dd = -(Math.Log(spot / h) + (a + (gamma - 0.5) * Math.Pow(V, 2)) * t) / dt;
//            k = 2 * a / Math.Pow(V, 2) + (2 * gamma - 1);

//            return (Math.Exp(Lambda) * Math.Pow(spot, gamma) * (NormSDist(dd) - Math.Pow((i / spot), k) * NormSDist(dd - 2 * Math.Log(i / spot) / dt)));
//        }


//        private static double erf(double x)
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
//            //return 1 - (a1 * t + a2 * t * t + a3 * t * t * t + a4 * t * t * t * t + a5 * t * t * t * t * t) * Math.Exp(-1 * x * x);

//            //Horner's method, takes O(n) operations for nth order polynomial
//            return 1 - ((((((a5 * t + a4) * t) + a3) * t + a2) * t) + a1) * t * Math.Exp(-1 * x * x);
//        }


//        //normal distribution
//        public static double NormSDist(double z)
//        {
//            double sign = 1;
//            if (z < 0) sign = -1;
//            return 0.5 * (1.0 + sign * erf(Math.Abs(z) / Math.Sqrt(2)));
//        }

//        //log normal distribution
//        public static double CDFNormal(double X)
//        {
//            double functionReturnValue, d, a1, a2, a3, a4, a5;
//            d = 1 / (1 + 0.2316419 * Math.Abs(X));
//            a1 = 0.31938153;
//            a2 = -0.356563782;
//            a3 = 1.781477937;
//            a4 = -1.821255978;
//            a5 = 1.330274429;
//            functionReturnValue = 1 - 1 / Math.Sqrt(2 * 3.1415926) * Math.Exp(-0.5 * X * X) * (a1 * d + a2 * d * d + a3 * d * d * d + a4 * d * d * d * d + a5 * d * d * d * d * d);
//            if (X < 0)
//                functionReturnValue = 1 - functionReturnValue;
//            return functionReturnValue;
//        }

//        //Approximation Newton Raphson for opt price used for Ju Zhong pricing model
//        public static double NewtonRaphsonOpt(string callPut, double Sstar, double X, double r, double q, double t, double sigma)
//        {

//            double d1Sstar, VeSstar, ha, fs, fprimes, dvesstarS, callPutAdjust = 0;


//            if (callPut == "C")
//                callPutAdjust = 1;
//            if (callPut == "P")
//                callPutAdjust = -1;

//            d1Sstar = d1(Sstar, X, r, q, t, sigma);
//            VeSstar = BlackScholes(callPut, Sstar, X, t, r, q, sigma);
//            ha = callPutAdjust * (Sstar - X) - VeSstar;
//            fs = callPutAdjust * Math.Exp(-q * t) * NormSDist(callPutAdjust * d1Sstar) + (lambda * ha / Sstar) - callPutAdjust;

//            while (Math.Abs(fs) > 0.0001)
//            {
//                d1Sstar = d1(Sstar, X, r, q, t, sigma);
//                VeSstar = BlackScholes(callPut, Sstar, X, t, r, q, sigma);
//                ha = callPutAdjust * (Sstar - X) - VeSstar;

//                fs = callPutAdjust * Math.Exp(-q * t) * NormSDist(callPutAdjust * d1Sstar) + (lambda * ha / Sstar) - callPutAdjust;
//                dvesstarS = AnalyticalDelta(callPut, Sstar, sigma, q, r, X, t);

//                fprimes = callPutAdjust * Math.Exp(-q * t) * (Math.Exp(-(Math.Pow(d1Sstar, 2)) / 2) / Math.Sqrt(2 * Math.PI)) + (lambda * callPutAdjust * Sstar - lambda * dvesstarS * Sstar - lambda * ha) / Sstar;

//                Sstar = Sstar - fs / fprimes;
//            }

//            return Sstar;

//        }


//        /***********************************************************************************************************************************************************************************/
//        //GREEKS functions  
//        public static double CallDelta(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return Math.Exp(-div * t) * N1p(spot, vol, div, rate, strike, t);
//        }
//        public static double CallGamma(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / vol / spot / Math.Sqrt(t);
//        }
//        public static double CallTheta(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -vol * spot * Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / 2 / Math.Sqrt(t) + div * spot * N1p(spot, vol, div, rate, strike, t) * Math.Exp(-div * t) - rate * strike * Math.Exp(-rate * t) * N2p(spot, vol, div, rate, strike, t);
//        }
//        public static double CallSpeed(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / vol / vol / spot / spot / t * (d1(spot, vol, div, rate, strike, t) + vol * Math.Sqrt(t));
//        }
//        public static double CallVega(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return spot * Math.Sqrt(t) * Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t);
//        }
//        public static double CallRho(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return strike * t * Math.Exp(-rate * t) * N2p(spot, vol, div, rate, strike, t);
//        }
//        public static double CallRhoD(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -t * spot * Math.Exp(-div * t) * N1p(spot, vol, div, rate, strike, t);
//        }

//        public static double PutDelta(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return Math.Exp(-div * t) * (-1 + N1p(spot, vol, div, rate, strike, t));
//        }
//        public static double PutGamma(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / vol / spot / Math.Sqrt(t);
//        }
//        public static double PutTheta(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -vol * spot * Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / 2 / Math.Sqrt(t) - div * spot * N1m(spot, vol, div, rate, strike, t) * Math.Exp(-div * t) + rate * strike * Math.Exp(-rate * t) * N2m(spot, vol, div, rate, strike, t);
//        }
//        public static double PutSpeed(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t) / vol / vol / spot / spot / t * (d1(spot, vol, div, rate, strike, t) + vol * Math.Sqrt(t));
//        }
//        public static double PutVega(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return spot * Math.Sqrt(t) * Math.Exp(-div * t) * Nxd1(spot, vol, div, rate, strike, t);
//        }
//        public static double PutRho(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return -strike * t * Math.Exp(-rate * t) * N2m(spot, vol, div, rate, strike, t);
//        }
//        public static double PutRhoD(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return t * spot * Math.Exp(-div * t) * N1m(spot, vol, div, rate, strike, t);
//        }


//        public static double d1(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return (Math.Log(spot / strike) + (rate - div + 0.5 * vol * vol) * t) / vol / Math.Sqrt(t);
//        }
//        public static double d2(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return d1(spot, vol, div, rate, strike, t) - vol * Math.Sqrt(t);
//        }
//        public static double N1p(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return CDFNormal(d1(spot, vol, div, rate, strike, t));
//        }
//        public static double N2p(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return CDFNormal(d2(spot, vol, div, rate, strike, t));
//        }
//        public static double N1m(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return CDFNormal(-d1(spot, vol, div, rate, strike, t));
//        }
//        public static double N2m(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return CDFNormal(-d2(spot, vol, div, rate, strike, t));
//        }
//        public static double Nxd1(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return 1 / Math.Sqrt(2 * 3.1415926) * Math.Exp(-0.5 * d1(spot, vol, div, rate, strike, t) * d1(spot, vol, div, rate, strike, t));
//        }
//        public static double Nxd2(double spot, double vol, double div, double rate, double strike, double t)
//        {
//            return 1 / Math.Sqrt(2 * 3.1415926) * Math.Exp(-0.5 * d2(spot, vol, div, rate, strike, t) * d2(spot, vol, div, rate, strike, t));
//        }


//        //Numerical Delta per option (in %)
//        public static double NumericalDelta(string callPut, string amEur, double bump, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            double optPrice, OptPriceBump, spotBump;
//            spotBump = (1 + bump) * spot;

//            if (amEur == "E")
//            {
//                optPrice = BlackScholes(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BlackScholes(callPut, spotBump, strike, t, rate, div, vol);
//                return ((OptPriceBump - optPrice) / (bump * spot));

//            }

//            if (amEur == "A")
//            {
//                optPrice = BjerksundStensland(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BjerksundStensland(callPut, spotBump, strike, t, rate, div, vol);
//                return ((OptPriceBump - optPrice) / (bump * spot));

//            }
//            return (0);
//        }

//        // Numerical Vega per option (in CCY)
//        public static double NumericalVega(string callPut, string amEur, double bump, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            double optPrice, OptPriceBump, volBump;
//            volBump = bump + vol;

//            if (amEur == "E")
//            {
//                optPrice = BlackScholes(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BlackScholes(callPut, spot, strike, t, rate, div, volBump);
//                return ((OptPriceBump - optPrice));

//            }

//            if (amEur == "A")
//            {
//                optPrice = BjerksundStensland(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BjerksundStensland(callPut, spot, strike, t, rate, div, volBump);
//                return ((OptPriceBump - optPrice));

//            }
//            return (0);
//        }

//        // Numerical Theta per option (in CCY)
//        public static double NumericalTheta(string callPut, string amEur, double bump, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            double optPrice, OptPriceBump, tBump;
//            tBump = t - bump / basis;

//            if (amEur == "E")
//            {
//                optPrice = BlackScholes(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BlackScholes(callPut, spot, strike, tBump, rate, div, vol);
//                return ((OptPriceBump - optPrice));
//            }

//            if (amEur == "A")
//            {
//                optPrice = BjerksundStensland(callPut, spot, strike, t, rate, div, vol);
//                OptPriceBump = BjerksundStensland(callPut, spot, strike, tBump, rate, div, vol);
//                return ((OptPriceBump - optPrice));
//            }
//            return (0);
//        }


//        //Numerical Delta per option (in %)
//        public static double NumericalGamma(string callPut, string amEur, double bump, double spot, double vol, double div, double rate, double strike, double t)
//        {
//            double delta, deltaBump, spotBump;
//            spotBump = (1 + bump) * spot;

//            delta = NumericalDelta(callPut, amEur, bump, spot, vol, div, rate, strike, t);
//            deltaBump = NumericalDelta(callPut, amEur, bump, spotBump, vol, div, rate, strike, t);
//            return ((deltaBump - delta));

//        }

//        /*******************************************************************************************************************************************************************************/
//        //Return the div to use for Pricing a given expi option chain
//        // dividend )à partir uniquement du spot de la date d'expi, du shift et de la serie d'option concernée
//        public static double ImpliedDiv(double closeSpot, string expirationDate, int shift, int basis, string optionSerie)
//        {
//            double strike1, strike2, callPrice1, putPrice1, callPrice2, putPrice2, t;
//            double div;
//            DateTime expi = Convert.ToDateTime(expirationDate);

//            strike2 = Math.Round(closeSpot / shift, 0) * shift; //return le strike ATM
//            strike1 = strike2 - shift;
//            t = Convert.ToDouble(expi - DateTime.Today) / basis;

//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike1, expi, type) par exemple (6E, 12550, "16/10/2014", call)
//            callPrice1 = 0; //callPrice2 de strike1
//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike2, expi, type) par exemple (6E, 12600, "16/10/2014", call)
//            callPrice2 = 0; // call de strike 2
//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike1, expi, type) par exemple (6E, 12600, "16/10/2014", put)
//            putPrice1 = 0;
//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike2, expi, type) par exemple (6E, 12600, "16/10/2014", put)
//            putPrice2 = 0;

//            div = -Math.Log((strike2 * (callPrice1 - putPrice1) - strike1 * (callPrice2 - putPrice2)) / (closeSpot * (strike2 - strike1))) / t;

//            return (div);
//        }

//        //calcul du div à partir des strike et des prix close des options
//        public static double ImpliedDivForOptions(double strike1, double strike2, double closeSpot, double callPrice1, double putPrice1, double callPrice2, double putPrice2, double t)
//        {
//            double div;

//            div = -Math.Log((strike2 * (callPrice1 - putPrice1) - strike1 * (callPrice2 - putPrice2)) / (closeSpot * (strike2 - strike1))) / t;

//            return (div);
//        }

//        //retyrn le strike ATM le shif est fourni en fichier de param
//        public static double GetATMStrike(double fwdPrice, int shift)
//        {
//            return (Math.Round(fwdPrice / shift, 0) * shift - shift);
//        }


//        public static double ImpliedRate(double div, double closeSpot, string expirationDate, int shift, int basis, string optionSerie)
//        {
//            double strike, callPrice, putPrice, t;
//            double rate;
//            DateTime expi = Convert.ToDateTime(expirationDate);

//            strike = Math.Round(closeSpot / shift, 0) * shift - shift;

//            t = Convert.ToDouble(expi - DateTime.Today) / basis;
//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike1, expi, type) par exemple (6E, 12550, "16/10/2014", call)
//            callPrice = 0;

//            //ici ajouter la fonction renvoyant le close de la veille de l'option (option serie, strike1, expi, type) par exemple (6E, 12600, "16/10/2014", put)
//            putPrice = 0;


//            rate = -Math.Log((closeSpot * Math.Exp(-div * t) - callPrice + putPrice) / strike) / t;

//            return (rate);
//        }
//        //INTEREST RATE 
//        public static double ImpliedRateForOptions(double div, double strike, double closeSpot, double callPrice, double putPrice, double t)
//        {
//            double rate;

//            rate = -Math.Log((closeSpot * Math.Exp(-div * t) - callPrice + putPrice) / strike) / t;

//            return (rate);
//        }

//        //Forward calculation
//        public static double ForwardCalculation(double strike1, double strike2, double callPrice1, double putPrice1, double callPrice2, double putPrice2)
//        {
//            double fwd;

//            fwd = (strike1 * (callPrice2 - putPrice2) - strike2 * (callPrice1 - putPrice1)) / (callPrice2 - putPrice2 - callPrice1 + putPrice1);

//            return (fwd);
//        }

//        //calcul du fwd avec formule analytique
//        public static double ForwardCalculationAnalytical(double spotPrice, double div, double rate, string expirationDate)
//        {
//            double fwd, t;

//            DateTime expi = Convert.ToDateTime(expirationDate);

//            t = Convert.ToDouble(expi - DateTime.Today) / basis;

//            fwd = spotPrice * Math.Exp((rate - div) * t);

//            return (fwd);
//        }

//        /*******************************************************************************************************************************************************************************/

//        static void Main(string[] args)
//        {
//            double spot, strike1, strike2, rate, div, t, vol, BSPrice, analitycalGamma, numericalGamma, callPrice1, putPrice1, callPrice2, putPrice2;
//            double optTargetPrice, guess;
//            double fwd;

//            spot = 3225.93;
//            strike1 = 3200;
//            strike2 = 3250;
//            callPrice1 = 156.4;
//            callPrice2 = 132.4;
//            putPrice1 = 210.6;
//            putPrice2 = 236.8;
//            //rate = 0.078 ;
//            //div = (1.701*88/365)/100;
//            t = (double)261 / (double)365;

//            optTargetPrice = callPrice1;
//            guess = 0.12;

//            fwd = ForwardCalculation(strike1, strike2, callPrice1, putPrice1, callPrice2, putPrice2);

//            div = ImpliedDivForOptions(strike1, strike2, spot, callPrice1, putPrice1, callPrice2, putPrice2, t);

//            rate = ImpliedRateForOptions(div, strike1, spot, callPrice1, putPrice1, t);

//            //accuary=precison -0.0001 par defaut fichier param
//            //optiontargetPrice= mid de l'option en question recup API en live (bid+ask)/2
//            //guess= point de depart (evaluation grossiere) fichier param
//            //lower bound = borne basse fichier param
//            // uppr bound = borne haute fichier param
//            //spot= prix de fwd en last correspondant à l'expi (opt oct==> fwd oct...)
//            //rate & div correspondant à l'expit
//            //strike de l'option
//            //t= temps en année =(expi-today) /basis
//            //call ou put 
//            //exercise type Am ou Eu
//            vol = ImpliedVolBisection(0.0001, optTargetPrice, guess, 0.08, 0.8, spot, rate, div, strike1, t, "C", "E");


//            fwd = spot * Math.Exp((rate - div) * t);

//            BSPrice = BlackScholes("C", spot, strike1, t, rate, div, vol);

//            double k;

//            k = Math.Round(spot / 50, 0) * 50;
//            Console.WriteLine("BjerksundStensland : ", BSPrice);


//            Console.ReadKey();
//        }
//    }
//}
