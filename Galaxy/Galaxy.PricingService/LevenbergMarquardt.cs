using System;

namespace PricingLib
{
    public class LevenbergMarquardt
    {
        private Matrix _jacobian;
        private Matrix _residuals;
        private Matrix _regressionParameters0;
        private Derivatives _derivatives;
        private Parameter[] _regressionParameters;
        private Parameter[] _observedParameters;
        private Func<double>[] _regressionFunctions;
        private double _error;
        private double[,] _data;
        private double _l0 = 100.0;
        private double _v = 10.0;

        public LevenbergMarquardt(Func<double>[] regressionFunctions, Parameter[] regressionParameters, Parameter[] observedParameters, double[,] data, int numberOfDerivativePoints)
        {
            System.Diagnostics.Debug.Assert(data.GetLength(1) == observedParameters.Length + regressionFunctions.Length);
            _data = data;
            _observedParameters = observedParameters;
            _regressionParameters = regressionParameters;
            _regressionFunctions = regressionFunctions;
            _error = 1;
            int numberOfParameters = _regressionParameters.Length;
            int numberOfPoints = data.GetLength(0);
            _derivatives = new Derivatives(numberOfDerivativePoints);
            int numberOfFunctions = _regressionFunctions.Length;
            _jacobian = new Matrix(numberOfFunctions * numberOfPoints, numberOfParameters);
            _residuals = new Matrix(numberOfFunctions * numberOfPoints, 1); 
            _regressionParameters0 = new Matrix(numberOfParameters, 1);
        }

        public LevenbergMarquardt(Func<double>[] regressionFunctions, Parameter[] regressionParameters, Parameter[] observedParameters, double[,] data) : this(regressionFunctions, regressionParameters, observedParameters, data, 3)
        {

        }

        public void Iterate()
        {
            int numberOfPoints = _data.GetLength(0);
            int numberOfParameters = _regressionParameters.Length;
            int numberOfFunctions = _regressionFunctions.Length;
            _error = 0.0;
            for (int i = 0; i < numberOfFunctions; i++)
            {
                for (int j = 0; j < numberOfPoints; j++)
                {
                    for (int k = 0; k < _observedParameters.Length; k++)
                    {
                        _observedParameters[k].Value = _data[j, k];
                    }
                    double functionValue = _regressionFunctions[i]();
                    double residual = _data[j, _observedParameters.Length + i] - functionValue;
                    _residuals[j + i * numberOfPoints, 0] = residual;
                    _error += residual * residual;
                    for (int k = 0; k < numberOfParameters; k++)
                    {
                        _jacobian[j + i * numberOfPoints, k] = _derivatives.ComputePartialDerivative(_regressionFunctions[i], _regressionParameters[k], 1, functionValue);
                    }
                }
            }
            for (int i = 0; i < numberOfParameters; i++)
            {
                _regressionParameters0[i, 0] = _regressionParameters[i];
            }

            Matrix jacobianTranspose = _jacobian.Transpose();
            Matrix jacobianTransposeResiduals = jacobianTranspose * _residuals;
            Matrix jacobianTransposeJacobian = jacobianTranspose * _jacobian;
            Matrix jacobianTransposeJacobianDiagnol = new Matrix(jacobianTransposeJacobian.RowCount, jacobianTransposeJacobian.RowCount);

            for (int i = 0; i < jacobianTransposeJacobian.RowCount; i++)
            {
                jacobianTransposeJacobianDiagnol[i, i] = jacobianTransposeJacobian[i, i];
            }
            double newResidual = _error + 1.0;
            _l0 /= _v;
            while (newResidual > _error)
            {
                newResidual = 0.0; _l0 *= _v;
                Matrix matLHS = jacobianTransposeJacobian + _l0 * jacobianTransposeJacobianDiagnol;
                var delta = matLHS.SolveFor(jacobianTransposeResiduals);
                ;
                var newRegressionParameters = _regressionParameters0 + delta;
                for (int i = 0; i < numberOfParameters; i++)
                {
                    _regressionParameters[i].Value = newRegressionParameters[i, 0];
                }
                for (int i = 0; i < numberOfFunctions; i++)
                {
                    for (int j = 0; j < numberOfPoints; j++)
                    {
                        for (int k = 0; k < _observedParameters.Length; k++)
                        {
                            _observedParameters[k].Value = _data[j, k];
                        }
                        double functionValue = _regressionFunctions[i]();
                        double residual = _data[j, _observedParameters.Length + i] - functionValue; newResidual += residual * residual;
                    }
                }
            }
            _l0 /= _v;
        }

        public void Res()
        {
            for (int i = 0; i < _regressionParameters.Length; i++)
            {
                Console.WriteLine($"param {i} : {_regressionParameters[i]}");
            }
            Console.WriteLine(_error);
        }

        public void compute()
        {
            for (int i = 0; i < 10000; i++)
            {
                Iterate();
            }
        }

        public Parameter[] regressionParameters
        {
            get
            {
                return _regressionParameters;
            }
        }
    }
}
