//using System;
//using NUnit.Framework;

//namespace Galaxy.PricingService.Test
//{
//    [TestFixture]
//    public class LmAlgoUnitTest
//    {
//        /* Test harness routine, which contains test data, invokes mpfit() */
//        [Test]
//        public void TestLinFit()
//        {
//            double[] x = {  -1.7237,1.8712,-0.9661,-0.2839,1.3416,1.3757,-1.3703,0.0426,-0.1497,0.8207};
//            double[] y = {  0.1900,6.5807,1.4582,2.7271,5.5969,5.6249,0.7876,3.2599,2.9772,4.5936};

//            var ey = new double[x.Length];
//            double[] p = { 1.0, 1.0 };           /* Initial conditions */
//            double[] pactual = {  1.77,3.21 };   /* Actual values used to make data */

//            var result = new LmResult(p.Length);

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.07; /* Data errors */
//            }

//            var v = new CustomUserVariable {X = x, Y = y, Ey = ey};

//            /* Call fitting function for 10 data points and 2 parameters */
//            LmAlgo.Solve(FunctionModel.LinFunc, x.Length, p.Length, p, null, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.001);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.001);
//        }

//        [Test]
//        public void TestLogFit()
//        {
//            double[] x = { 0.038, 0.194, 0.425, 0.626, 1.253, 2.5, 3.74 };
//            double[] y = { 0.0500, 0.127, 0.094, 0.2122, 0.2729, 0.2665, 0.3317 };

//            var ey = new double[x.Length];
//            double[] p = { 1, 1 };           /* Initial conditions */
//            double[] pactual = { 0.3618, 0.556266 };    /* Actual values used to make data */
//            int i;

//            var result = new LmResult(p.Length);

//            for (i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.07; /* Data errors */
//            }

//            var v = new CustomUserVariable {X = x, Y = y, Ey = ey};

//            /* Call fitting function for 10 data points and 2 parameters */
//            LmAlgo.Solve(FunctionModel.CustomFunc, x.Length, p.Length, p, null, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.001);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.001);
//        }

//        [Test]
//        public void TestGatheralFit()
//        {
//            const double forward = 3518.74;
//            const double timeToExpi = 0.05849;

//            const double error = 0.07;
//            double[] x = { 3000, 3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900 };
//            double[] y = { 0.296827, 0.269388, 0.2449, 0.223526, 0.204884, 0.188140, 0.1716, 0.1607, 0.1511, 0.1472 }; 

//            var ey = new double[x.Length];
//            double[] p = { 1, 1, 1, 1, 1, forward, timeToExpi };                /* Initial conditions */
//            double[] pactual = { 0.2165, -0.1962, -1.1551, 0.2694, 0.4049 };    /* Actual values used to make data */

//            // Parameter constraints
//            var param = new[] {     new LmParams(), 
//                                    new LmParams(),
//                                    new LmParams(),
//                                    new LmParams(),
//                                    new LmParams(),
//                                    new LmParams {isFixed = 1},  // Fix parameter 1
//                                    new LmParams {isFixed = 1}   // Fix parameter 1
//                                };

//            var result = new LmResult(p.Length);

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = error; 
//            }

//            var v = new CustomUserVariable{X =x,Y = y,Ey= ey};

//            LmAlgo.Solve(FunctionModel.GatheralFunc, x.Length, p.Length, p, param, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.001);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.001);
//            Assert.Less(Math.Abs(p[2] - pactual[2]), 0.001);
//            Assert.Less(Math.Abs(p[3] - pactual[3]), 0.001);
//            Assert.Less(Math.Abs(p[4] - pactual[4]), 0.001);
//            Assert.Less(Math.Abs(p[5] - pactual[3]), 0.001); // fixed param
//            Assert.Less(Math.Abs(p[6] - pactual[4]), 0.001); // fixed param
//        }

//        /* Test harness routine, which contains test gaussian-peak data 

//           Example of fixing two parameter

//           Commented example of how to put boundary constraints
//        */

//        [Test]
//        public void TestQuadFit()
//        {
//            double[] x = {-1.7237,1.8712,-0.9661,-0.2839,1.3417,1.3757,-1.3703,0.0426,-0.1497,0.8207};
//            double[] y = {23.0959,26.4494,10.2045,5.40507,15.7876,16.5209,15.9718,4.7669,4.9338,8.7348};
//            var ey = new double[x.Length];
//            double[] p = { 1.0, 1.0, 1.0 };        /* Initial conditions */
//            double[] pactual = { 4.7, 0.0, -6.2 };  /* Actual values used to make data */

//            var result = new LmResult(p.Length);

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.2;       /* Data errors */
//            }

//            var v = new CustomUserVariable { X = x, Y = y, Ey = ey };

//            /* Call fitting function for 10 data points and 3 parameters */
//            LmAlgo.Solve(FunctionModel.QuadFunc, x.Length, p.Length, p, null, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.2);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.2);
//            Assert.Less(Math.Abs(p[2] - pactual[2]), 0.2);
//        }

//        /* Test harness routine, which contains test quadratic data;
//        Example of how to fix a parameter
//     */
//        [Test]
//        public void TestQuadFix()
//        {
//            double[] x = { -1.7237, 1.8712, -0.9661, -0.2839, 1.3417, 1.3757, -1.3703, 0.0426, -0.1497, 0.8207 };
//            double[] y = { 23.0959, 26.4494, 10.2045, 5.40507, 15.7876, 16.5209, 15.9718, 4.7669, 4.9338, 8.7348 };

//            var ey = new double[x.Length];
//            double[] p = { 1.0, 0.0, 1.0 };        /* Initial conditions */
//            double[] pactual = { 4.7, 0.0, -6.2 };  /* Actual values used to make data */

//            var result = new LmResult(p.Length);

//            var param = new[] /* Parameter constraints */
//                                {
//                                    new LmParams() , 
//                                    new LmParams {isFixed = 1},  /* Fix parameter 1 */
//                                    new LmParams()
//                                };

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.2;
//            }

//            var v = new CustomUserVariable { X = x, Y = y, Ey = ey };

//            /* Call fitting function for 10 data points and 3 parameters (1
//               parameter fixed) */
//            LmAlgo.Solve(FunctionModel.QuadFunc, x.Length, p.Length, p, param, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.1);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.1);
//            Assert.Less(Math.Abs(p[2] - pactual[2]), 0.1);

//        }

//        /* Test harness routine, which contains test gaussian-peak data */
//        [Test]
//        public void TestGaussFit()
//        {
//            double[] x = { -1.7237, 1.8712, -0.9660, -0.2839, 1.3417, 1.3757, -1.3703, 0.0426, -0.1497, 0.8207 };
//            double[] y = { -0.0445, 0.8732, 0.7444, 4.7632, 0.1719, 0.1164, 1.5646, 5.2322, 4.2543, 0.6279 };
//            var ey = new double[x.Length];
//            double[] p = { 0.0, 1.0, 1.0, 1.0 };       /* Initial conditions */
//            double[] pactual = { 0.0, 4.70, 0.0, 0.5 };/* Actual values used to make data*/

//            var result = new LmResult(p.Length);

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.5;
//            }
            
//            var v = new CustomUserVariable { X = x, Y = y, Ey = ey };

//            /* Call fitting function for 10 data points and 4 parameters (no
//               parameters fixed) */
//            LmAlgo.Solve(FunctionModel.GaussFunc, x.Length, p.Length, p, null, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.5);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.5);
//            Assert.Less(Math.Abs(p[2] - pactual[2]), 0.5);
//            Assert.Less(Math.Abs(p[3] - pactual[3]), 0.5);
//        }

//        [Test]
//        public void TestGaussFitFixParams()
//        {
//            double[] x = {-1.7237,1.8712,-0.9660,-0.2839,1.3417,1.3757,-1.3703,0.0426,-0.1497,0.8207};
//            double[] y = {-0.0445,0.8732,0.7444,4.7632,0.1719,0.1164,1.5646,5.2322,4.2543,0.6279};
//            var ey = new double[x.Length];
//            double[] p = { 0.0, 1.0, 0.0, 0.1 };       /* Initial conditions */
//            double[] pactual = { 0.0, 4.70, 0.0, 0.5 };/* Actual values used to make data*/

//            var result = new LmResult(p.Length);

//            var pars = new[]/* Parameter constraints */
//                                {
//                                    new LmParams{isFixed = 1},/* Fix parameters 0 and 2 */
//                                    new LmParams(), 
//                                    new LmParams{isFixed = 1},
//                                    new LmParams()
//                                };

//            for (int i = 0; i < x.Length; i++)
//            {
//                ey[i] = 0.5;
//            }

//            var v = new CustomUserVariable { X = x, Y = y, Ey = ey };

//            /* Call fitting function for 10 data points and 4 parameters (2
//               parameters fixed) */
//            LmAlgo.Solve(FunctionModel.GaussFunc, x.Length, p.Length, p, pars, null, v, ref result);

//            Assert.Less(Math.Abs(p[0] - pactual[0]), 0.01);
//            Assert.Less(Math.Abs(p[1] - pactual[1]), 0.5);
//            Assert.Less(Math.Abs(p[2] - pactual[2]), 0.01);
//            Assert.Less(Math.Abs(p[3] - pactual[3]), 0.5);
//        }
//    }
//}
