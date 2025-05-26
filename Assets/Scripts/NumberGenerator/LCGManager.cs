using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using MathNet.Numerics.Distributions;

public class LCGManager
{
    private long initialSeed;
    private long multiplier;
    private long increment;
    private long modulus;
    private double alphaTestLevel;

    public LCGManager(long seed, long multiplier, long increment, long modulus, double alphaLevel)
    {
        this.initialSeed = seed;
        this.multiplier = multiplier;
        this.increment = increment;
        this.modulus = modulus;
        this.alphaTestLevel = alphaLevel;
    }

    public List<float> GetValidatedRiNumbers(
        int numSamples,
        out bool generationSucceeded)
    {
        List<float> validatedRiValues = new List<float>();
        generationSucceeded = false;
        if (numSamples <= 0)
        {
            Debug.LogError("LCGManager: numSamples debe ser mayor que 0.");
            return validatedRiValues;
        }
        int maxAttempts = 100;
        int attempts = 0;
        long currentAttemptSeed = this.initialSeed;
        while (attempts < maxAttempts)
        {
            attempts++;
            List<double> currentDoubleRiNumbers = GenerateLCGNumbers(currentAttemptSeed, numSamples, this.multiplier, this.increment, this.modulus);
            if (currentDoubleRiNumbers.Count != numSamples)
            {
                currentAttemptSeed++;
                continue;
            }
            bool averagePassed = RunAverageTest(currentDoubleRiNumbers, this.alphaTestLevel, out double calculatedAverage);
            bool variancePassed = RunVarianceTest(currentDoubleRiNumbers, this.alphaTestLevel, calculatedAverage);
            if (averagePassed && variancePassed)
            {
                validatedRiValues = currentDoubleRiNumbers.ConvertAll(d => (float)d);
                generationSucceeded = true;
                return validatedRiValues;
            }
            else
            {
                currentAttemptSeed++;
            }
        }

        Debug.LogError($"LCGManager: Falló la generación de un conjunto de números Ri válido después de {maxAttempts} intentos. La semilla inicial fue: {this.initialSeed}. Revisa los parámetros del LCG.");
        return validatedRiValues;
    }

    private List<double> GenerateLCGNumbers(long currentSeed, int count, long currentMultiplier, long currentIncrement, long currentModulus)
    {
        List<double> generatedNumbers = new List<double>(count);
        long xi = currentSeed;

        if (currentModulus <= 0)
        {
            Debug.LogError("LCGManager.GenerateLCGNumbers: El módulo (m) debe ser > 0.");
            return generatedNumbers;
        }
        double denominator = (double)currentModulus;
        for (int i = 0; i < count; i++)
        {
            xi = (currentMultiplier * xi + currentIncrement);
            xi = xi % currentModulus;
            if (xi < 0) xi += currentModulus;

            generatedNumbers.Add((double)xi / denominator);
        }
        return generatedNumbers;
    }

    private bool RunAverageTest(List<double> numbers, double alpha, out double calculatedAverage)
    {
        calculatedAverage = 0;
        int n = numbers.Count;
        if (n == 0) return false;
        calculatedAverage = numbers.Average();
        double z_alpha_half = InverseNormalCDF(1.0 - (alpha / 2.0));
        if (double.IsNaN(z_alpha_half)) return false;
        double limitFactor = z_alpha_half / Math.Sqrt(12.0 * n);
        double lowerLimit = 0.5 - limitFactor;
        double upperLimit = 0.5 + limitFactor;

        bool passed = (calculatedAverage >= lowerLimit && calculatedAverage <= upperLimit);
        return passed;
    }

    private bool RunVarianceTest(List<double> numbers, double alpha, double precalculatedAverage)
    {
        int n = numbers.Count;
        if (n <= 1) return false;
        double sumOfSquares = 0;
        foreach (double num in numbers)
        {
            sumOfSquares += Math.Pow(num - precalculatedAverage, 2);
        }
        double sampleVariance = sumOfSquares / (n - 1);
        int degreesOfFreedom = n - 1;
        if (degreesOfFreedom == 0) return false;
        double theoreticalVariance = 1.0 / 12.0;
        double chi_square_lower_crit_val = InverseChiSquareCDF(alpha / 2.0, degreesOfFreedom);
        double chi_square_upper_crit_val = InverseChiSquareCDF(1.0 - (alpha / 2.0), degreesOfFreedom);
        if (double.IsNaN(chi_square_lower_crit_val) || double.IsNaN(chi_square_upper_crit_val)) return false;
        double lowerLimitForVariance = (chi_square_lower_crit_val * theoreticalVariance) / degreesOfFreedom;
        double upperLimitForVariance = (chi_square_upper_crit_val * theoreticalVariance) / degreesOfFreedom;
        bool passed = (sampleVariance >= lowerLimitForVariance && sampleVariance <= upperLimitForVariance);
        return passed;
    }

    private double InverseNormalCDF(double p)
    {
        if (p <= 0 || p >= 1)
        {
            return double.NaN;
        }
        return Normal.InvCDF(0, 1, p);
    }

    private double InverseChiSquareCDF(double probability, int degreesOfFreedom)
    {
        if (probability < 0 || probability >= 1 || degreesOfFreedom <= 0)
        {
            return double.NaN;
        }
        if (probability > 0.9999999999) probability = 0.9999999999;
        return ChiSquared.InvCDF(degreesOfFreedom, probability);
    }
}