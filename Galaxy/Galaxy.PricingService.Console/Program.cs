
namespace Galaxy.PricingService.Console
{
    class Program
    {
        static void Main()
        {
            double accuracy = 0.0001;
            double guess = 0.5;
            double lowerBound = 0.00;
            double upperBound = 1;
            double strike = 40;
            string exercice = "EU";
            string optionType = "CALL";
            double optionPrice = 4.759;
            double spotPrice = 40;
            double rate = 0.1;
            double div = 0;
            double vol = 0.2;
            double time = 0.5;
            double a = -0.2712;
            double b = 0.2603;
            double sigma = 1.0666;
            double rho = 0.2057;
            double m = 0.2449;
            double quantity = 100;

            double atmModelVol = Option.SviVolatility(1, 1, a, b, sigma, rho, m, time);
            double modelVol = Option.SviVolatility(strike,spotPrice, a, b, sigma, rho, m, time);

        //    double oldStickyDelta = OptionLib.UniqueInstance.OldNumericalDelta(optionType, exercice, spotPrice, modelVol, strike, quantity, time, a, b, sigma, rho, m);
            double stikyDelta = Option.Delta(optionType, spotPrice, strike, atmModelVol, time);
            double delta = Option.Delta(optionType, spotPrice, strike, modelVol, time);

            System.Console.ReadLine();
        }
    }
}
