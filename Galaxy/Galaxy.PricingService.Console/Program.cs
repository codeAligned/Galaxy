
using System;
using PricingLib;

namespace Galaxy.PricingService.Console
{
    class Program
    {
        static void Main()
        {
            double a = 1;
            double b = 1;
            double rho = 0.5;
            double sig = 1.5;
            double m = 2;

            double fwdPrice = 100;
            double strike = 150;

            double time = Option.GetTimeToExpiration(DateTime.Today, new DateTime(2015, 12, 18));

            // function to fit
            Func<double, double>[] regressionFunction =
            {
                (k) => a + b*(rho*(k-m)+Math.Sqrt(Math.Pow((k-m),2)+Math.Pow(sig,2)))
            };

            // generate perfect data
            double[,] data = new double[30, 2];
            for (int i = 0; i < 30; i++)
            {
                double t = 4 * (i) + 100;
                data[i, 0] = t;
                data[i, 1] = Math.Sqrt(regressionFunction[0](Math.Log(t / 100)));
            }

            Parameter[] outParams  = SVI.Fit(data, fwdPrice);

            System.Console.WriteLine($"param a: {outParams[0].Value}");
            System.Console.WriteLine($"param b: {outParams[1].Value}");
            System.Console.WriteLine($"param rho: {outParams[2].Value}");
            System.Console.WriteLine($"param sig: {outParams[3].Value}");
            System.Console.WriteLine($"param m: {outParams[4].Value}");

            System.Console.WriteLine(Option.SviVolatility(strike, fwdPrice, outParams[0].Value, outParams[1].Value, outParams[2].Value, outParams[3].Value, outParams[4].Value, time));
        }
    }
}
