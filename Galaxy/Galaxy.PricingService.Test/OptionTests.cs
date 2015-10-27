using System;
using NUnit.Framework;

namespace Galaxy.PricingService.Test
{
    [TestFixture]
    public class OptionTests
    {
        //[Test]
        //public void GetPreviousDay_over_weekend()
        //{
        //    DateTime result = OptionLib.PreviousWeekDay(new DateTime(2015, 08, 03));
        //    DateTime expected = new DateTime(2015, 07, 31);

        //    Assert.AreEqual(result, expected);
        //}

        //[Test]
        //public void GetPreviousDay_over_weekday()
        //{
        //    DateTime result = OptionLib.PreviousWeekDay(new DateTime(2015, 07, 31));
        //    DateTime expected = new DateTime(2015, 07, 30);

        //    Assert.AreEqual(result, expected);
        //}

        [Test]
        public void TestGreekDeltaCall()
        {
            double result = Option.Delta("CALL", 49, 50, 0.2, 0.3846, 0.05);
            double expected = 0.5216;

            Assert.AreEqual(Math.Round(result, 4), expected);
        }


        [Test]
        public void TestGreekVomma()
        {
            double result = Option.Vomma(49, 50, 0.2, 0.3846, 0.05);
            double expected = -0.229;

            Assert.AreEqual(Math.Round(result, 3), expected);
        }


        [Test]
        public void TestGreekThetaCall()
        {
            double result = Option.Theta("CALL", 49, 50, 0.2, 0.3846, 0.05);
            double expected = -0.0118;

            Assert.AreEqual(Math.Round(result, 4), expected);
        }

        [Test]
        public void TestGreekGamma()
        {
            double result = Option.Gamma(49, 50, 0.2, 0.3846, 0.05);
            double expected = 0.065545;

            Assert.AreEqual(Math.Round(result, 6), expected);
        }

        [Test]
        public void TestGreekVega()
        {
            double result = Option.Vega(49, 50, 0.2, 0.3846, 0.05);
            double expected = 12.1;

            Assert.AreEqual(Math.Round(result, 1), expected);
        }

        [Test]
        public void TestGreekRho()
        {
            double result = Option.Rho("CALL", 49, 50, 0.2, 0.3846, 0.05);
            double expected = 0.0891;

            Assert.AreEqual(Math.Round(result, 4), expected);
        }

        [Test]
        public void TestGreekVanna()
        {
            double result = Option.Vanna(150, 100, 0.25, 30, 0.03, 0.02);
            double expected = 0.0722;

            Assert.AreEqual(Math.Round(result, 4), expected);
        }

        [Test]
        public void TestGreekCharmCall()
        {
            double result = Option.Charm("CALL", 150, 100, 0.25, 30, 0.03, 0.02);
            double expected = 0.008633;

            Assert.AreEqual(Math.Round(result, 6), expected);
        }

        [Test]
        public void TestGreekCharmPut()
        {
            double result = Option.Charm("PUT", 150, 100, 0.25, 30, 0.03, 0.02);
            double expected = -0.002343;

            Assert.AreEqual(Math.Round(result, 6), expected);
        }

        [Test]
        public void TestGreekUltima()
        {
            double result = Option.Ultima(150, 100, 0.25, 30, 0.03, 0.02);
            double expected = 1714.610454;

            Assert.AreEqual(Math.Round(result, 6), expected);
        }

        [Test]
        public void TestGreekColor()
        {
            double result = Option.Color(150, 100, 0.25, 30, 0.03, 0.02);
            double expected = -0.00002533;
            Assert.AreEqual(Math.Round(result, 8), expected);
        }

        [Test]
        public void TestBlackScholesCall()
        {
            double result = Option.BlackScholesCall(42, 40, 0.5, 0.2, 0.1);
            double expected = 4.76;
            Assert.AreEqual(Math.Round(result, 2), expected);
        }

        [Test]
        public void TestBlackScholesPut()
        {
            double result = Option.BlackScholesPut(42, 40, 0.5, 0.2, 0.1);
            double expected = 0.81;
            Assert.AreEqual(Math.Round(result, 2), expected);
        }

        [Test]
        public void TestGreekVeta()
        {
            double result = Option.Veta(150, 100, 0.25, 30, 0.03, 0.02);
            double expected = 1.356;
            Assert.AreEqual(Math.Round(result, 3), expected);
        }

        [Test]
        public void TestGreekSpeed()
        {
            double result = Option.Speed(150, 100, 0.25, 30, 0.03, 0.02);
            double expected = -0.000006;
            Assert.AreEqual(Math.Round(result, 6), expected);
        }

        [Test]
        public void TestVolatilitySvi()
        {
            double result = Option.SviVolatility(3300, 3224.048096, 0.0179, -0.6701, -0.0823, 0.9356, 0.2935, 0.02739);
            double expected = 0.2684;
            Assert.AreEqual(Math.Round(result, 4), expected);
        }
    }
}
