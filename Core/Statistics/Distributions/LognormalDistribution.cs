﻿using System;
using System.Collections.Generic;

using Meta.Numerics;
using Meta.Numerics.Functions;

namespace Meta.Numerics.Statistics.Distributions {

    /// <summary>
    /// Represents a log-normal distribution.
    /// </summary>
    /// <remarks>
    /// <para>The logrithm of a log-normal distributed variable is distributed normally.</para>
    /// <img src="../images/LogNormalFromNormal.png" />
    /// <para>The log-normal distribution is commonly used in financial engineering as a model of stock prices.
    /// If the rate of return on an asset is distributed normally, then its price will be distributed log-normally.</para>
    /// </remarks>
    /// <seealso cref="NormalDistribution"/>
    /// <seealso href="http://en.wikipedia.org/wiki/Log-normal_distribution" />
    public sealed class LognormalDistribution : Distribution, IParameterizedDistribution {

        /// <summary>
        /// Initializes a log normal distribution.
        /// </summary>
        /// <param name="mu">The mean of the underlying normal distribution.</param>
        /// <param name="sigma">The standard deviation of the underlying normal distribution.</param>
        /// <remarks>
        /// <para>Note that the <paramref name="mu"/> and <paramref name="sigma"/> parameters are
        /// <em>not</em> the mean and standard deviation of the log-normal deviates; they are the
        /// mean and standard deviation of their logrithms z = ln x.</para>
        /// </remarks>
        public LognormalDistribution (double mu, double sigma) {
            if (sigma <= 0.0) throw new ArgumentOutOfRangeException("sigma");
            this.mu = mu;
            this.sigma = sigma;
            this.normal = new NormalDistribution(mu, sigma);
        }

        /// <summary>
        /// Initializes a standard log normal distribution.
        /// </summary>
        /// <remarks>
        /// <para>A standard lognormal distribution has mu = 0 and sigma = 1. It is the log transform of the standard
        /// normal distribution.</para>
        /// </remarks>
        public LognormalDistribution () : this(0, 1) {
        }

        private double mu = 0.0;
        private double sigma = 1.0;
        private NormalDistribution normal;

        /// <inheritdoc />
        public override double ProbabilityDensity (double x) {
            if (x <= 0.0) return (0.0);
            double z = (Math.Log(x) - mu) / sigma;
            return (Math.Exp(-z * z / 2.0) / x / (Global.SqrtTwoPI * sigma));
        }

        /// <inheritdoc />
        public override double Mean {
            get {
                return (Math.Exp(mu + sigma * sigma / 2.0));
            }
        }

        /// <inheritdoc />
        public override double Median {
            get {
                return (Math.Exp(mu));
            }
        }

        /// <inheritdoc />
        public override double Variance {
            get {
                return (MoreMath.ExpMinusOne(sigma * sigma) * Math.Exp(2.0 * mu + sigma * sigma));
            }
        }

        /// <inheritdoc />
        public override double Skewness {
            get {
                return (Math.Sqrt(MoreMath.ExpMinusOne(sigma * sigma)) * (Math.Exp(sigma * sigma) + 2.0));
            }
        }

        /// <inheritdoc />
        public override Interval Support {
            get {
                return (Interval.FromEndpoints(0.0, Double.PositiveInfinity));
            }
        }

        /// <inheritdoc />
        public override double LeftProbability (double x) {
            if (x <= 0.0) {
                return (0.0);
            } else {
                return (normal.LeftProbability(Math.Log(x)));
            }
            /*
            double z = (Math.Log(x) - mu) / sigma;
            return (NormalDistribution.Phi(z));
            */
        }

        /// <inheritdoc />
        public override double RightProbability (double x) {
            if (x <= 0.0) {
                return (1.0);
            } else {
                return (normal.RightProbability(Math.Log(x)));
            }
            /*
            double z = (Math.Log(x) - mu) / sigma;
            return (NormalDistribution.Phi(-z));
            */
        }

        /// <inheritdoc />
        public override double InverseLeftProbability (double P) {
            if ((P < 0.0) || (P > 1.0)) throw new ArgumentOutOfRangeException("P");
            return (Math.Exp(normal.InverseLeftProbability(P)));
            /*
            double z = Global.SqrtTwo * AdvancedMath.InverseErf(2.0 * P - 1.0);
            return (Math.Exp(mu + sigma * z));
             */
        }

        /// <inheritdoc />
        public override double InverseRightProbability (double Q) {
            if ((Q < 0.0) || (Q > 1.0)) throw new ArgumentOutOfRangeException("Q");
            return (Math.Exp(normal.InverseRightProbability(Q)));
        }

        /// <inheritdoc />
        public override double GetRandomValue (Random rng) {
            if (rng == null) throw new ArgumentNullException("rng");
            return (Math.Exp(normal.GetRandomValue(rng)));
        }

        /// <inheritdoc />
        public override double Moment (int n) {
            if (n < 0) {
                throw new ArgumentOutOfRangeException("n");
            } else {
                return (Math.Exp(n * mu + MoreMath.Pow2(n * sigma) / 2.0));
            }
        }

        /// <inheritdoc />
        public override double MomentAboutMean (int n) {
            if (n < 0) {
                throw new ArgumentOutOfRangeException("n");
            } else if (n == 0) {
                return (1.0);
            } else if (n == 1) {
                return (0.0);
            } else if (n == 2) {
                return (Variance);
            } else {

                // This follows from a straightforward expansion of (x-m)^n and substitution of expressions for M_k.
                // It eliminates some arithmetic but is still subject to loss of significance due to cancelation.
                double s2 = sigma * sigma / 2.0;

                double C = 0.0;
                for (int i = 0; i <= n; i++) {
                    double dC = AdvancedIntegerMath.BinomialCoefficient(n, i) * Math.Exp((MoreMath.Pow2(n - i) + i) * s2);
                    if (i % 2 != 0) dC = -dC;
                    C += dC;
                }
                return (Math.Exp(n * mu) * C);

                // this isn't great, but it does the job
                // expand in terms of moments about the origin
                // there is likely to be some cancelation, but the distribution is wide enough that it may not matter
                /*
                double m = -Mean;
                double C = 0.0;
                for (int k = 0; k <= n; k++) {
                    C += AdvancedIntegerMath.BinomialCoefficient(n, k) * Moment(k) * Math.Pow(m, n - k);
                }
                return (C);
                */
            }

        }

        double[] IParameterizedDistribution.GetParameters () {
            return (new double[] { mu, sigma });
        }

        void IParameterizedDistribution.SetParameters (IList<double> parameters) {
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (parameters.Count != 2) throw new DimensionMismatchException();
            if (parameters[1] <= 0.0) throw new ArgumentOutOfRangeException("parameters");
            mu = parameters[0];
            sigma = parameters[1];
        }

        double IParameterizedDistribution.Likelihood (double x) {
            return (ProbabilityDensity(x));
        }


    }

}