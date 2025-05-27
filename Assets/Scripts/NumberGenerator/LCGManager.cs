using UnityEngine; // Necesario para Debug.Log, etc.
using System.Collections.Generic;
using System;
using System.Linq; // Para .Average() y .Sum()
using MathNet.Numerics.Distributions; // Para Normal.InvCDF y ChiSquared.InvCDF

/// <summary>
/// Generador Congruencia Lineal (LCG) para producir secuencias de números pseudoaleatorios
/// que pueden ser sometidos a pruebas estadísticas de calidad (promedio y varianza).
/// No es un modelo de simulación en sí, sino una herramienta para generar entradas aleatorias
/// para modelos de simulación u otras lógicas que requieran aleatoriedad controlada.
/// </summary>
public class LCGManager
{
    // Parámetros del LCG y de prueba, guardados en la instancia.
    private long initialSeed;       // Semilla inicial para el primer intento de generación.
    private long multiplier;        // Parámetro 'a' del LCG.
    private long increment;         // Parámetro 'c' del LCG.
    private long modulus;           // Parámetro 'm' del LCG.
    private double alphaTestLevel;  // Nivel de significancia alfa para las pruebas estadísticas.

    /// <summary>
    /// Constructor para inicializar el LCGManager con los parámetros necesarios.
    /// </summary>
    /// <param name="seed">Semilla inicial para el primer intento de generación.</param>
    /// <param name="multiplier">Parámetro 'a' del LCG.</param>
    /// <param name="increment">Parámetro 'c' del LCG.</param>
    /// <param name="modulus">Parámetro 'm' del LCG.</param>
    /// <param name="alphaLevel">Nivel de significancia alfa para las pruebas estadísticas.</param>
    public LCGManager(long seed, long multiplier, long increment, long modulus, double alphaLevel)
    {
        this.initialSeed = seed;
        this.multiplier = multiplier;
        this.increment = increment;
        this.modulus = modulus;
        this.alphaTestLevel = alphaLevel;
    }

    /// <summary>
    /// Intenta generar una secuencia de números pseudoaleatorios (Ri) en el rango [0,1)
    /// que pasen las pruebas de promedio y varianza.
    /// </summary>
    /// <param name="numSamples">La cantidad de números Ri a generar y probar.</param>
    /// <param name="generationSucceeded">Salida: true si se generó una secuencia válida, false en caso contrario.</param>
    /// <returns>Una lista de floats (Ri) si tiene éxito; una lista vacía si falla.</returns>
    public List<float> GetValidatedRiNumbers(
        int numSamples,
        out bool generationSucceeded)
    {
        List<float> validatedRiValues = new List<float>();
        generationSucceeded = false; // Asumir fallo hasta que se pruebe lo contrario.
        // Valida que se solicite un número positivo de muestras.
        if (numSamples <= 0)
        {
            Debug.LogError("LCGManager: numSamples debe ser mayor que 0.");
            return validatedRiValues; // Devuelve lista vacía.
        }
        int maxAttempts = 100; // Límite de intentos para encontrar una secuencia válida.
        int attempts = 0;
        long currentAttemptSeed = this.initialSeed; // Comienza con la semilla proporcionada a la instancia.
        // Bucle para intentar generar y validar secuencias de números.
        while (attempts < maxAttempts)
        {
            attempts++;
            // Genera una secuencia de números usando los parámetros LCG de la instancia.
            List<double> currentDoubleRiNumbers = GenerateLCGNumbers(currentAttemptSeed, numSamples, this.multiplier, this.increment, this.modulus);

            // Verifica si se generó la cantidad esperada de números.
            if (currentDoubleRiNumbers.Count != numSamples)
            {
                currentAttemptSeed++; // Prueba con la siguiente semilla si la generación no fue completa.
                continue;
            }
            // Ejecuta pruebas estadísticas sobre los números generados.
            bool averagePassed = RunAverageTest(currentDoubleRiNumbers, this.alphaTestLevel, out double calculatedAverage);
            bool variancePassed = RunVarianceTest(currentDoubleRiNumbers, this.alphaTestLevel, calculatedAverage);

            // Si ambas pruebas pasan, la secuencia es válida.
            if (averagePassed && variancePassed)
            {
                validatedRiValues = currentDoubleRiNumbers.ConvertAll(d => (float)d); // Convierte a float.
                generationSucceeded = true;
                return validatedRiValues; // Devuelve la secuencia válida.
            }
            else
            {
                currentAttemptSeed++; // Si las pruebas fallan, prueba con la siguiente semilla.
            }
        }

        // Si se superan los intentos máximos sin éxito.
        Debug.LogError($"LCGManager: Falló la generación de un conjunto de números Ri válido después de {maxAttempts} intentos. La semilla inicial fue: {this.initialSeed}. Revisa los parámetros del LCG.");
        return validatedRiValues; // Devuelve lista vacía.
    }

    /// <summary>
    /// Genera una lista de números pseudoaleatorios utilizando el algoritmo LCG.
    /// Los números generados están en el rango [0, 1).
    /// </summary>
    /// <param name="currentSeed">La semilla actual para iniciar la generación.</param>
    /// <param name="count">El número de valores a generar.</param>
    /// <param name="currentMultiplier">El multiplicador (a) del LCG.</param>
    /// <param name="currentIncrement">El incremento (c) del LCG.</param>
    /// <param name="currentModulus">El módulo (m) del LCG.</param>
    /// <returns>Una lista de números 'double' pseudoaleatorios.</returns>
    private List<double> GenerateLCGNumbers(long currentSeed, int count, long currentMultiplier, long currentIncrement, long currentModulus)
    {
        List<double> generatedNumbers = new List<double>(count);
        long xi = currentSeed; // Valor actual de la secuencia.
        // Valida que el módulo sea positivo.
        if (currentModulus <= 0)
        {
            Debug.LogError("LCGManager.GenerateLCGNumbers: El módulo (m) debe ser > 0.");
            return generatedNumbers; // Retorna lista vacía.
        }
        // Denominador para normalizar los números al rango [0, 1). Xi / m.
        double denominator = (double)currentModulus;
        for (int i = 0; i < count; i++)
        {
            // Fórmula LCG: Xi+1 = (a * Xi + c) mod m
            xi = (currentMultiplier * xi + currentIncrement);
            xi = xi % currentModulus;
            // Asegura que el resultado del módulo sea positivo si xi es negativo.
            if (xi < 0) xi += currentModulus;

            generatedNumbers.Add((double)xi / denominator); // Normaliza y añade a la lista.
        }
        return generatedNumbers;
    }

    /// <summary>
    /// Realiza la prueba de promedio (media) sobre un conjunto de números.
    /// Comprueba si la media muestral está dentro de los límites de confianza para una media teórica de 0.5 (distribución U(0,1)).
    /// </summary>
    private bool RunAverageTest(List<double> numbers, double alpha, out double calculatedAverage)
    {
        calculatedAverage = 0;
        int n = numbers.Count;
        if (n == 0) return false; // No se puede probar un conjunto vacío.
        calculatedAverage = numbers.Average(); // Calcula la media muestral.
        // Calcula el valor crítico Z_(alfa/2) de la distribución normal estándar.
        double z_alpha_half = InverseNormalCDF(1.0 - (alpha / 2.0));
        if (double.IsNaN(z_alpha_half)) return false; // Error en el cálculo del Z crítico.
        // Calcula los límites de aceptación para la media.
        double limitFactor = z_alpha_half / Math.Sqrt(12.0 * n);
        double lowerLimit = 0.5 - limitFactor;
        double upperLimit = 0.5 + limitFactor;
        // La prueba pasa si la media calculada está dentro de los límites.
        bool passed = (calculatedAverage >= lowerLimit && calculatedAverage <= upperLimit);
        return passed;
    }

    /// <summary>
    /// Realiza la prueba de varianza sobre un conjunto de números.
    /// Comprueba si la varianza muestral está dentro de los límites de confianza para una varianza teórica de 1/12 (distribución U(0,1)).
    /// </summary>
    private bool RunVarianceTest(List<double> numbers, double alpha, double precalculatedAverage)
    {
        int n = numbers.Count;
        // Se necesita n > 1 para calcular la varianza muestral (n-1 grados de libertad).
        if (n <= 1) return false;
        // Calcula la sumatoria de los cuadrados de las diferencias respecto a la media.
        double sumOfSquares = 0;
        foreach (double num in numbers)
        {
            sumOfSquares += Math.Pow(num - precalculatedAverage, 2);
        }
        double sampleVariance = sumOfSquares / (n - 1); // Varianza muestral (S^2).
        int degreesOfFreedom = n - 1;
        // No se puede realizar la prueba Chi-Cuadrado con 0 grados de libertad.
        if (degreesOfFreedom == 0) return false;
        double theoreticalVariance = 1.0 / 12.0; // Varianza teórica de una U(0,1).
        // Calcula los valores críticos inferior y superior de la distribución Chi-Cuadrado.
        double chi_square_lower_crit_val = InverseChiSquareCDF(alpha / 2.0, degreesOfFreedom);
        double chi_square_upper_crit_val = InverseChiSquareCDF(1.0 - (alpha / 2.0), degreesOfFreedom);
        if (double.IsNaN(chi_square_lower_crit_val) || double.IsNaN(chi_square_upper_crit_val)) return false; // Error en el cálculo.
        // Calcula los límites de aceptación para la varianza muestral.
        double lowerLimitForVariance = (chi_square_lower_crit_val * theoreticalVariance) / degreesOfFreedom;
        double upperLimitForVariance = (chi_square_upper_crit_val * theoreticalVariance) / degreesOfFreedom;
        // La prueba pasa si la varianza muestral está dentro de los límites.
        bool passed = (sampleVariance >= lowerLimitForVariance && sampleVariance <= upperLimitForVariance);
        return passed;
    }

    /// <summary>
    /// Calcula el inverso de la Función de Distribución Acumulada (CDF) para una distribución Normal Estándar.
    /// </summary>
    private double InverseNormalCDF(double p)
    {
        // Valida que la probabilidad p esté en el rango (0, 1).
        if (p <= 0 || p >= 1) return double.NaN;
        return Normal.InvCDF(0, 1, p); // Usa la librería MathNet para media 0, desv. estándar 1.
    }

    /// <summary>
    /// Calcula el inverso de la Función de Distribución Acumulada (CDF) para una distribución Chi-Cuadrado.
    /// </summary>
    private double InverseChiSquareCDF(double probability, int degreesOfFreedom)
    {
        // Valida entradas.
        if (probability < 0 || probability >= 1 || degreesOfFreedom <= 0) return double.NaN;
        // Ajusta la probabilidad si está demasiado cerca de 1.0 para evitar problemas con algunas implementaciones de InvCDF.
        if (probability > 0.9999999999) probability = 0.9999999999;
        return ChiSquared.InvCDF(degreesOfFreedom, probability); // Usa la librería MathNet.
    }
}