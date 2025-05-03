using UnityEngine; // Required for Debug.Log and Application.dataPath/persistentDataPath
using System;
using System.Collections.Generic;
using System.Linq; // Required for Linq functions like Average, Sum
using System.IO; // Required for file writing (StreamWriter)

// --- Placeholder for MathNet.Numerics or similar library ---
// IMPORTANT: To use this script, you MUST add a library like MathNet.Numerics
// to your Unity project (e.g., via NuGet package manager for Unity).
// Then, you need to uncomment the 'using MathNet.Numerics.Distributions;' line
// below and replace the placeholder calls in InverseNormalCDF and InverseChiSquareCDF
// with the actual calls from the library.
// Example using MathNet.Numerics:
// using MathNet.Numerics.Distributions;
// ------------------------------------------------------------

public class LCG_Tester : MonoBehaviour
{
    [Header("LCG Parameters")]
    public long seed = 12345; // x0
    public long multiplier = 1103515245; // a
    public long increment = 12345; // c
    public long modulus = 2147483648; // m (2^31) - ensure m > 0

    [Header("Test Parameters")]
    public int numSamples = 1000;
    public double alpha = 0.05; // Significance level for tests

    [Header("Output")]
    public string outputFileName = "ri_numbers.txt";

    // --- Placeholder for Inverse Standard Normal Cumulative Distribution Function ---
    // Equivalent to scipy.stats.norm.ppf(1 - alpha / 2)
    private double InverseNormalCDF(double p)
    {
        // !! IMPORTANT !!
        // Replace this placeholder with the actual call from your chosen library.
        // Example using MathNet.Numerics:
        // if (p <= 0 || p >= 1) return double.NaN; // Or handle appropriately
        // return Normal.InvCDF(0, 1, p);
        // --------------------------------------------------------------
        Debug.LogError("InverseNormalCDF function needs implementation (e.g., using MathNet.Numerics)!");
        if (p > 0.97 && p < 0.98) return 1.96; // Common value for alpha=0.05 as a crude placeholder
        return double.NaN; // Indicate failure
    }

    // --- Placeholder for Inverse Chi-Squared Cumulative Distribution Function ---
    // Equivalent to scipy.stats.chi2.ppf(probability, degrees_freedom)
    private double InverseChiSquareCDF(double probability, int degreesOfFreedom)
    {
        // !! IMPORTANT !!
        // Replace this placeholder with the actual call from your chosen library.
        // Example using MathNet.Numerics:
        // if (probability < 0 || probability > 1 || degreesOfFreedom <= 0) return double.NaN; // Or handle appropriately
        // return ChiSquared.InvCDF(degreesOfFreedom, probability);
        // --------------------------------------------------------------
        Debug.LogError("InverseChiSquareCDF function needs implementation (e.g., using MathNet.Numerics)!");
        return double.NaN; // Indicate failure
    }

    // --- Linear Congruential Generator (LCG) ---
    // Based on linear_congruence.py
    private List<double> GenerateLCGNumbers(long currentSeed, int count)
    {
        List<double> riNumbers = new List<double>(count);
        long xi = currentSeed;

        if (modulus <= 0)
        {
            Debug.LogError("Modulus (m) must be greater than 0.");
            return riNumbers; // Return empty list on error
        }
        if (modulus == 1)
        {
             Debug.LogWarning("Modulus (m) = 1 will result in only 0s.");
        }

        // Using m-1 in the denominator as per the provided python script
        double denominator = modulus - 1.0;
        if (denominator <= 0) // Avoid division by zero or negative if m=1
        {
             Debug.LogWarning("Denominator (m-1) is zero or negative. RI numbers will be 0 or NaN.");
             denominator = 1.0; // Prevent division by zero, result will be xi
        }


        for (int i = 0; i < count; i++)
        {
            // Calculate next xi: (a * xi + c) % m
            // Use long to prevent overflow during multiplication
            xi = (multiplier * xi + increment) % modulus;
            // Ensure xi is non-negative (modulo can sometimes be negative in C# if dividend is negative, though unlikely here)
             if (xi < 0) {
                xi += modulus;
            }

            // Calculate ri: xi / (m - 1)
            double ri = (double)xi / denominator;
            riNumbers.Add(ri);
        }
        return riNumbers;
    }

    // --- Average Test ---
    // Based on average_test.py
    private bool RunAverageTest(List<double> numbers, out double calculatedAverage)
    {
        calculatedAverage = 0;
        int n = numbers.Count;
        if (n == 0) return false;

        // Calculate average
        calculatedAverage = numbers.Average();

        // Calculate Z value for confidence interval
        double z_alpha_half = InverseNormalCDF(1.0 - (alpha / 2.0));
        if (double.IsNaN(z_alpha_half))
        {
             Debug.LogError("Failed to get Z value for Average Test. Check InverseNormalCDF implementation.");
             return false;
        }


        // Calculate limits
        double limitFactor = z_alpha_half * (1.0 / Math.Sqrt(12.0 * n));
        double lowerLimit = 0.5 - limitFactor;
        double upperLimit = 0.5 + limitFactor;

        // Check if average is within limits
        bool passed = (calculatedAverage >= lowerLimit && calculatedAverage <= upperLimit);

        Debug.Log($"Average Test: Avg={calculatedAverage:F5}, Lower={lowerLimit:F5}, Upper={upperLimit:F5} -> {(passed ? "PASSED" : "FAILED")}");
        return passed;
    }

    // --- Variance Test ---
    // Based on variance_test.py
    private bool RunVarianceTest(List<double> numbers, double precalculatedAverage)
    {
        int n = numbers.Count;
        if (n <= 1) // Variance is undefined for n <= 1
        {
            Debug.LogWarning("Variance Test requires n > 1.");
            return false;
        }

        // Calculate Variance (Population Variance, similar to numpy.var default)
        double sumOfSquares = numbers.Sum(num => Math.Pow(num - precalculatedAverage, 2));
        double variance = sumOfSquares / n;

        // Calculate Chi-Square critical values
        int degreesOfFreedom = n - 1;
        double chi_square1 = InverseChiSquareCDF(alpha / 2.0, degreesOfFreedom);
        double chi_square2 = InverseChiSquareCDF(1.0 - (alpha / 2.0), degreesOfFreedom);

         if (double.IsNaN(chi_square1) || double.IsNaN(chi_square2))
        {
             Debug.LogError("Failed to get Chi-Square values for Variance Test. Check InverseChiSquareCDF implementation.");
             return false;
        }

        // Calculate limits for variance
        double denominator = 12.0 * degreesOfFreedom;
        if (Math.Abs(denominator) < 1e-9) {
             Debug.LogError("Denominator is zero in Variance Test limits calculation (n=1?).");
             return false;
        }
        double lowerLimit = chi_square1 / denominator;
        double superiorLimit = chi_square2 / denominator;

        // Check if variance is within limits
        bool passed = (variance >= lowerLimit && variance <= superiorLimit);

        Debug.Log($"Variance Test: Var={variance:F5}, Lower={lowerLimit:F5}, Upper={superiorLimit:F5} -> {(passed ? "PASSED" : "FAILED")}");
        return passed;
    }

    // --- Main Function to Generate and Test ---
    public List<double> GenerateAndTestNumbers()
    {
        int attempts = 0;
        long currentAttemptSeed = seed; // Start with the initial seed

        while (true)
        {
            attempts++;
            Debug.Log($"--- Attempt #{attempts} (Seed: {currentAttemptSeed}) ---");

            // 1. Generate numbers using LCG
            List<double> currentRiNumbers = GenerateLCGNumbers(currentAttemptSeed, numSamples);
             if (currentRiNumbers.Count != numSamples) {
                 Debug.LogError($"Failed to generate the required number of samples ({numSamples}). Check LCG parameters.");
                 return null; // Indicate failure
             }


            // 2. Run Average Test
            bool averagePassed = RunAverageTest(currentRiNumbers, out double calculatedAverage);

            // 3. Run Variance Test (needs the average)
            bool variancePassed = false;
            if (averagePassed) // No need to calculate variance if average is already suspect, but run it if average test passed.
            {
                 // Pass the pre-calculated average to avoid recalculating
                variancePassed = RunVarianceTest(currentRiNumbers, calculatedAverage);
            }
            else {
                 Debug.Log("Skipping Variance Test because Average Test failed.");
            }


            // 4. Check if both passed
            if (averagePassed && variancePassed)
            {
                Debug.Log($"Success! Found valid set of {numSamples} numbers after {attempts} attempts.");
                // Write to file
                WriteRiToFile(currentRiNumbers, outputFileName);
                return currentRiNumbers; // Return the valid list
            }
            else
            {
                Debug.Log("Set failed tests. Generating new set...");
                // Prepare for next attempt: Use the last generated number's integer part (xi) as the next seed,
                // or simply increment/change the seed in another way. Using the last xi provides some sequence.
                // Get the last xi state from the generator logic implicitly.
                // Re-running GenerateLCGNumbers with the same seed produces the same sequence.
                // To get a NEW sequence, we must change the seed. Let's increment it for simplicity.
                currentAttemptSeed++;
                // Optional: Add a mechanism to prevent infinite loops, e.g., max attempts.
                // if (attempts > 10000) {
                //    Debug.LogError("Exceeded maximum attempts to find a valid number set.");
                //    return null;
                // }
            }
        }
    }

    // --- Function to write the successful list to file ---
    private void WriteRiToFile(List<double> riList, string fileName)
    {
        // Use Application.persistentDataPath for write access on most platforms
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log($"Attempting to write {riList.Count} numbers to: {filePath}");

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false)) // Overwrite existing file
            {
                foreach (double ri in riList)
                {
                    // Use CultureInfo.InvariantCulture to ensure '.' as decimal separator
                    writer.WriteLine(ri.ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            Debug.Log($"Successfully saved file '{fileName}' to persistentDataPath.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error writing file '{filePath}': {e.Message}");
        }
    }

    // --- Example Usage (e.g., call from another script or a UI button) ---
    void Start()
    {
        // Automatically start generation when the script loads
        // You might want to trigger this from a button click instead
        Debug.Log("Starting LCG generation and testing...");
        GenerateAndTestNumbers();
    }
}