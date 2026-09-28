using System;

namespace SampleProject
{
    /// <summary>
    /// Provides methods for converting between temperature units
    /// </summary>
    public static class TemperatureConverter
    {
        /// <summary>
        /// Converts Celsius to Fahrenheit
        /// </summary>
        /// <param name="celsius">Temperature in Celsius</param>
        /// <returns>Temperature in Fahrenheit</returns>
        public static double CelsiusToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        /// <summary>
        /// Converts Fahrenheit to Celsius
        /// </summary>
        /// <param name="fahrenheit">Temperature in Fahrenheit</param>
        /// <returns>Temperature in Celsius</returns>
        public static double FahrenheitToCelsius(double fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }

        /// <summary>
        /// Converts Celsius to Kelvin
        /// </summary>
        /// <param name="celsius">Temperature in Celsius</param>
        /// <returns>Temperature in Kelvin</returns>
        public static double CelsiusToKelvin(double celsius)
        {
            return celsius + 273.15;
        }

        /// <summary>
        /// Converts Kelvin to Celsius
        /// </summary>
        /// <param name="kelvin">Temperature in Kelvin</param>
        /// <returns>Temperature in Celsius</returns>
        public static double KelvinToCelsius(double kelvin)
        {
            return kelvin - 273.15;
        }

        /// <summary>
        /// Converts Fahrenheit to Kelvin
        /// </summary>
        /// <param name="fahrenheit">Temperature in Fahrenheit</param>
        /// <returns>Temperature in Kelvin</returns>
        public static double FahrenheitToKelvin(double fahrenheit)
        {
            return CelsiusToKelvin(FahrenheitToCelsius(fahrenheit));
        }

        /// <summary>
        /// Converts Kelvin to Fahrenheit
        /// </summary>
        /// <param name="kelvin">Temperature in Kelvin</param>
        /// <returns>Temperature in Fahrenheit</returns>
        public static double KelvinToFahrenheit(double kelvin)
        {
            return CelsiusToFahrenheit(KelvinToCelsius(kelvin));
        }
    }
}