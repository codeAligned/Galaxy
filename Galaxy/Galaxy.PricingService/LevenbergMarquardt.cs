using System;

namespace PricingLib
{
    public static class LevenbergMarquardt
    {
        public static Parameter[] Compute(Func<double>[] regressionFunctions, Parameter[] regressionParameters, Parameter[] observedParameters, double[,] data, int numberOfDerivativePoints)
        {
            double l0 = 100.0;
            double v = 10.0;

            Derivatives derivatives = new Derivatives(numberOfDerivativePoints);
            Matrix jacobian = new Matrix(regressionFunctions.Length * data.GetLength(0), regressionParameters.Length);
            Matrix residuals = new Matrix(regressionFunctions.Length * data.GetLength(0), 1);
            Matrix regressionParameters0 = new Matrix(regressionParameters.Length, 1);

            for (int f = 0; f < 10000; f++)
            {
                int numberOfPoints = data.GetLength(0);
                int numberOfParameters = regressionParameters.Length;
                int numberOfFunctions = regressionFunctions.Length;
                double error = 0.0;

                for (int i = 0; i < numberOfFunctions; i++)
                {
                    for (int j = 0; j < numberOfPoints; j++)
                    {
                        for (int k = 0; k < observedParameters.Length; k++)
                        {
                            observedParameters[k].Value = data[j, k];
                        }
                        double functionValue = regressionFunctions[i]();
                        double residual = data[j, observedParameters.Length + i] - functionValue;
                        residuals[j + i * numberOfPoints, 0] = residual;
                        error += residual * residual;
                        for (int k = 0; k < numberOfParameters; k++)
                        {
                            jacobian[j + i * numberOfPoints, k] = derivatives.ComputePartialDerivative(regressionFunctions[i], regressionParameters[k], 1, functionValue);
                        }
                    }
                }
                for (int i = 0; i < numberOfParameters; i++)
                {
                    regressionParameters0[i, 0] = regressionParameters[i];
                }

                Matrix jacobianTranspose = jacobian.Transpose();
                Matrix jacobianTransposeResiduals = jacobianTranspose * residuals;
                Matrix jacobianTransposeJacobian = jacobianTranspose * jacobian;
                Matrix jacobianTransposeJacobianDiagnol = new Matrix(jacobianTransposeJacobian.RowCount, jacobianTransposeJacobian.RowCount);

                for (int i = 0; i < jacobianTransposeJacobian.RowCount; i++)
                {
                    jacobianTransposeJacobianDiagnol[i, i] = jacobianTransposeJacobian[i, i];
                }

                double newResidual = error + 1.0;
                l0 /= v;

                while (newResidual > error)
                {
                    newResidual = 0.0; l0 *= v;
                    Matrix matLHS = jacobianTransposeJacobian + l0 * jacobianTransposeJacobianDiagnol;
                    var delta = matLHS.SolveFor(jacobianTransposeResiduals);
                    ;
                    var newRegressionParameters = regressionParameters0 + delta;
                    for (int i = 0; i < numberOfParameters; i++)
                    {
                        regressionParameters[i].Value = newRegressionParameters[i, 0];
                    }
                    for (int i = 0; i < numberOfFunctions; i++)
                    {
                        for (int j = 0; j < numberOfPoints; j++)
                        {
                            for (int k = 0; k < observedParameters.Length; k++)
                            {
                                observedParameters[k].Value = data[j, k];
                            }
                            double functionValue = regressionFunctions[i]();
                            double residual = data[j, observedParameters.Length + i] - functionValue; newResidual += residual * residual;
                        }
                    }
                }
                l0 /= v;
            }

            return regressionParameters;
        }
    }
}
