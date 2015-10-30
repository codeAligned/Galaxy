
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
            double m = 2;
            double sig = 1.5;
            Func<double, double>[] regressionFunction =
            {
                (k) => a + b*(rho*(k-m)+Math.Sqrt(Math.Pow((k-m),2)+Math.Pow(sig,2)))
            };

            double[,] data = new double[30, 2];

            for (int i = 0; i < 30; i++)
            {
                double t = 4 * (i) + 100;
                data[i, 0] = t;
                data[i, 1] = Math.Sqrt(regressionFunction[0](Math.Log(t / 100)));
            }

            SVI test = new SVI(data, 100);

            Parameter[] paraFin = test.SVIRawPara;
            System.Console.WriteLine(paraFin[0].Value);
            System.Console.WriteLine(paraFin[1].Value);
            System.Console.WriteLine(paraFin[2].Value);
            System.Console.WriteLine(paraFin[3].Value);
            System.Console.WriteLine(paraFin[4].Value);
            System.Console.WriteLine(test.getVol(150));

        }
    }
}
