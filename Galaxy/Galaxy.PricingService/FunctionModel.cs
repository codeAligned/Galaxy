using System;
using System.Collections.Generic;

namespace Galaxy.PricingService
{
    public static class FunctionModel
    {
        /* 
        * linear fit function
        *
        * m - number of data points
        * n - number of parameters (2)
        * p - array of fit parameters 
        * dy - array of residuals to be returned
        * CustomUserVariable - private data (struct vars_struct *)
        *
        * RETURNS: error code (0 = success)
        */

        public static void LinFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            var v = (CustomUserVariable)vars;

            double[] x = v.X;
            double[] y = v.Y;
            double[] ey = v.Ey;

            for (int i = 0; i < dy.Length; i++)
            {
                double f = p[0] * x[i] + p[1];
                dy[i] = (y[i] - f) / ey[i];
            }
        }

        /* 
        * quadratic fit function
        *
        * m - number of data points
        * n - number of parameters (2)
        * p - array of fit parameters 
        * dy - array of residuals to be returned
        * CustomUserVariable - private data (struct vars_struct *)
        *
        * RETURNS: error code (0 = success)
        */
        public static void QuadFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            var v = (CustomUserVariable)vars;
            double[] x = v.X;
            double[] y = v.Y;
            double[] ey = v.Ey;

            /* Console.Write ("QuadFunc %f %f %f\n", p[0], p[1], p[2]); */

            for (int i = 0; i < dy.Length; i++)
            {
                double f = p[0] - p[1] * x[i] - p[2] * x[i] * x[i];
                dy[i] = (y[i] - f) / ey[i];
            }
        }

        /* 
         * gaussian fit function
         *
         * m - number of data points
         * n - number of parameters (4)
         * p - array of fit parameters 
         * dy - array of residuals to be returned
         * CustomUserVariable - private data (struct vars_struct *)
         *
         * RETURNS: error code (0 = success)
         */
        public static void GaussFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            var v = (CustomUserVariable)vars;
            double[] x = v.X;
            double[] y = v.Y;
            double[] ey = v.Ey;

            double sig2 = p[3] * p[3];

            for (int i = 0; i < dy.Length; i++)
            {
                double xc = x[i] - p[2];
                double f = p[1] * Math.Exp(-0.5 * xc * xc / sig2) - p[0];

                dy[i] = (y[i] - f) / ey[i];
            }
        }

        // p equal parameters a, b
        // dy error vector
        // vars obs xy errory
        public static void CustomFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
        {
            var v = (CustomUserVariable)vars;

            double[] x = v.X;
            double[] y = v.Y;
            double[] ey = v.Ey;

            for (int i = 0; i < dy.Length; i++)
            {
                double f = (p[0] * x[i]) / (p[1] + x[i]);
                dy[i] = (y[i] - f) / ey[i];
            }
        }

        public static void GatheralFunc(double[] p, double[] fvec, IList<double>[] dvec, object vars)
        {
            double a = p[0];
            double b = p[1];
            double sigma = p[2];
            double rho = p[3];
            double m = p[4];
            double fwdPrice = p[5];
            double timeToExpi = p[6];

            var v = (CustomUserVariable)vars;

            double[] x = v.X;
            double[] y = v.Y;
            double[] ey = v.Ey;

            for (int i = 0; i < fvec.Length; i++)
            {
                double f = Option.SviVolatility(x[i], fwdPrice, a, b, sigma, rho, m, timeToExpi);
                fvec[i] = (y[i] - f) / ey[i];
            }
        }
    }
}
