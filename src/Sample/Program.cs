using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GeneticSpecOptimizer.Pipeline;

namespace GeneticSpecOptimizer.Sample
{
    /// <summary>
    /// Sample program to demonstrate the genetic algorithm-based specification optimization
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Genetic Algorithm Specification Optimizer - Sample Run");
            Console.WriteLine("======================================================");
            Console.WriteLine();

            // Create sample code directory
            var sampleDir = CreateSampleProject();
            Console.WriteLine($"Created sample project in: {sampleDir}");

            // Create output directory
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
            Directory.CreateDirectory(outputDir);
            Console.WriteLine($"Output will be saved to: {outputDir}");
            Console.WriteLine();

            // Configure pipeline
            var config = new OptimizationPipelineConfig
            {
                SourceCodeDirectory = sampleDir,
                DocumentationFiles = new List<string> { Path.Combine(sampleDir, "README.md") },
                OutputDirectory = outputDir,
                OptimizerConfig = new Common.Models.HierarchyOptimizerConfig
                {
                    EvolutionConfig = new Common.Models.EvolutionConfig
                    {
                        PopulationSize = 20,
                        MaxGenerations = 10,
                        ElitismCount = 2
                    },
                    MaxOptimizationTimeMinutes = 1
                }
            };

            // Create pipeline
            var pipeline = new OptimizationPipeline();

            // Subscribe to progress updates
            pipeline.ProgressUpdated += (sender, e) =>
            {
                Console.WriteLine($"[{e.Stage}] {e.Message} - {e.ProgressPercentage:F1}%");
            };

            try
            {
                // Run pipeline
                Console.WriteLine("Starting optimization pipeline...");
                var result = await pipeline.RunAsync(config);

                // Display results
                Console.WriteLine();
                Console.WriteLine("Optimization completed successfully!");
                Console.WriteLine();
                Console.WriteLine("Metrics:");
                Console.WriteLine($"- Original size: {result.Metrics.OriginalSize} characters");
                Console.WriteLine($"- Optimized size: {result.Metrics.OptimizedSize} characters");
                Console.WriteLine($"- Size reduction: {result.Metrics.SizeReductionPercentage:F1}%");
                Console.WriteLine($"- Inherited sections: {result.Metrics.InheritedSections}/{result.Metrics.TotalSections} ({result.Metrics.InheritancePercentage:F1}%)");
                Console.WriteLine($"- Hierarchy depth: {result.Metrics.HierarchyDepth}");
                Console.WriteLine($"- Optimization time: {result.Metrics.OptimizationTime}");
                Console.WriteLine();
                Console.WriteLine("Output files:");
                foreach (var file in result.OutputFilePaths)
                {
                    Console.WriteLine($"- {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string CreateSampleProject()
        {
            // Create sample directory
            var sampleDir = Path.Combine(Directory.GetCurrentDirectory(), "sample");
            if (Directory.Exists(sampleDir))
            {
                Directory.Delete(sampleDir, true);
            }
            Directory.CreateDirectory(sampleDir);

            // Create README.md
            File.WriteAllText(Path.Combine(sampleDir, "README.md"), @"# Sample Project

This is a sample project to demonstrate the genetic algorithm-based specification optimization.

## Weather Service

The WeatherService provides weather data for different locations. It uses the WeatherProvider to fetch data and the WeatherFormatter to format it.

## Temperature Converter

The TemperatureConverter allows converting between different temperature units like Celsius, Fahrenheit, and Kelvin.

## Location Service

The LocationService provides geolocation data for different locations.
");

            // Create WeatherService.cs
            File.WriteAllText(Path.Combine(sampleDir, "WeatherService.cs"), @"using System;
using System.Threading.Tasks;

namespace SampleProject
{
    /// <summary>
    /// Provides weather data for different locations
    /// </summary>
    public class WeatherService
    {
        private readonly IWeatherProvider _weatherProvider;
        private readonly IWeatherFormatter _weatherFormatter;

        /// <summary>
        /// Creates a new instance of the WeatherService
        /// </summary>
        /// <param name=""weatherProvider"">The weather provider to use</param>
        /// <param name=""weatherFormatter"">The weather formatter to use</param>
        public WeatherService(IWeatherProvider weatherProvider, IWeatherFormatter weatherFormatter)
        {
            _weatherProvider = weatherProvider ?? throw new ArgumentNullException(nameof(weatherProvider));
            _weatherFormatter = weatherFormatter ?? throw new ArgumentNullException(nameof(weatherFormatter));
        }

        /// <summary>
        /// Gets the current weather for a location
        /// </summary>
        /// <param name=""location"">The location to get weather for</param>
        /// <returns>The formatted weather data</returns>
        public async Task<string> GetWeatherAsync(string location)
        {
            var weatherData = await _weatherProvider.GetWeatherDataAsync(location);
            return _weatherFormatter.Format(weatherData);
        }

        /// <summary>
        /// Gets the weather forecast for a location
        /// </summary>
        /// <param name=""location"">The location to get forecast for</param>
        /// <param name=""days"">Number of days to forecast</param>
        /// <returns>The formatted forecast data</returns>
        public async Task<string> GetForecastAsync(string location, int days)
        {
            var forecastData = await _weatherProvider.GetForecastDataAsync(location, days);
            return _weatherFormatter.FormatForecast(forecastData);
        }
    }

    /// <summary>
    /// Interface for weather data providers
    /// </summary>
    public interface IWeatherProvider
    {
        /// <summary>
        /// Gets current weather data for a location
        /// </summary>
        Task<WeatherData> GetWeatherDataAsync(string location);

        /// <summary>
        /// Gets forecast data for a location
        /// </summary>
        Task<ForecastData> GetForecastDataAsync(string location, int days);
    }

    /// <summary>
    /// Interface for weather data formatters
    /// </summary>
    public interface IWeatherFormatter
    {
        /// <summary>
        /// Formats weather data as a string
        /// </summary>
        string Format(WeatherData weatherData);

        /// <summary>
        /// Formats forecast data as a string
        /// </summary>
        string FormatForecast(ForecastData forecastData);
    }

    /// <summary>
    /// Represents weather data for a location
    /// </summary>
    public class WeatherData
    {
        /// <summary>
        /// The location this data is for
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The temperature in Celsius
        /// </summary>
        public double Temperature { get; set; }

        /// <summary>
        /// The humidity percentage
        /// </summary>
        public double Humidity { get; set; }

        /// <summary>
        /// The weather condition
        /// </summary>
        public string Condition { get; set; }
    }

    /// <summary>
    /// Represents forecast data for a location
    /// </summary>
    public class ForecastData
    {
        /// <summary>
        /// The location this forecast is for
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// The daily forecasts
        /// </summary>
        public DailyForecast[] DailyForecasts { get; set; }
    }

    /// <summary>
    /// Represents a daily weather forecast
    /// </summary>
    public class DailyForecast
    {
        /// <summary>
        /// The date of this forecast
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The minimum temperature in Celsius
        /// </summary>
        public double MinTemperature { get; set; }

        /// <summary>
        /// The maximum temperature in Celsius
        /// </summary>
        public double MaxTemperature { get; set; }

        /// <summary>
        /// The weather condition
        /// </summary>
        public string Condition { get; set; }
    }
}");

            // Create TemperatureConverter.cs
            File.WriteAllText(Path.Combine(sampleDir, "TemperatureConverter.cs"), @"using System;

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
        /// <param name=""celsius"">Temperature in Celsius</param>
        /// <returns>Temperature in Fahrenheit</returns>
        public static double CelsiusToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        /// <summary>
        /// Converts Fahrenheit to Celsius
        /// </summary>
        /// <param name=""fahrenheit"">Temperature in Fahrenheit</param>
        /// <returns>Temperature in Celsius</returns>
        public static double FahrenheitToCelsius(double fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }

        /// <summary>
        /// Converts Celsius to Kelvin
        /// </summary>
        /// <param name=""celsius"">Temperature in Celsius</param>
        /// <returns>Temperature in Kelvin</returns>
        public static double CelsiusToKelvin(double celsius)
        {
            return celsius + 273.15;
        }

        /// <summary>
        /// Converts Kelvin to Celsius
        /// </summary>
        /// <param name=""kelvin"">Temperature in Kelvin</param>
        /// <returns>Temperature in Celsius</returns>
        public static double KelvinToCelsius(double kelvin)
        {
            return kelvin - 273.15;
        }

        /// <summary>
        /// Converts Fahrenheit to Kelvin
        /// </summary>
        /// <param name=""fahrenheit"">Temperature in Fahrenheit</param>
        /// <returns>Temperature in Kelvin</returns>
        public static double FahrenheitToKelvin(double fahrenheit)
        {
            return CelsiusToKelvin(FahrenheitToCelsius(fahrenheit));
        }

        /// <summary>
        /// Converts Kelvin to Fahrenheit
        /// </summary>
        /// <param name=""kelvin"">Temperature in Kelvin</param>
        /// <returns>Temperature in Fahrenheit</returns>
        public static double KelvinToFahrenheit(double kelvin)
        {
            return CelsiusToFahrenheit(KelvinToCelsius(kelvin));
        }
    }
}");

            // Create LocationService.cs
            File.WriteAllText(Path.Combine(sampleDir, "LocationService.cs"), @"using System;
using System.Threading.Tasks;

namespace SampleProject
{
    /// <summary>
    /// Provides location data services
    /// </summary>
    public class LocationService
    {
        private readonly ILocationProvider _locationProvider;

        /// <summary>
        /// Creates a new instance of the LocationService
        /// </summary>
        /// <param name=""locationProvider"">The location provider to use</param>
        public LocationService(ILocationProvider locationProvider)
        {
            _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
        }

        /// <summary>
        /// Gets the coordinates for a location
        /// </summary>
        /// <param name=""locationName"">The name of the location</param>
        /// <returns>The coordinates of the location</returns>
        public async Task<Coordinates> GetCoordinatesAsync(string locationName)
        {
            return await _locationProvider.GetCoordinatesAsync(locationName);
        }

        /// <summary>
        /// Gets the address for coordinates
        /// </summary>
        /// <param name=""coordinates"">The coordinates to get the address for</param>
        /// <returns>The address at the coordinates</returns>
        public async Task<string> GetAddressAsync(Coordinates coordinates)
        {
            return await _locationProvider.GetAddressAsync(coordinates);
        }

        /// <summary>
        /// Calculates the distance between two locations
        /// </summary>
        /// <param name=""location1"">The first location</param>
        /// <param name=""location2"">The second location</param>
        /// <returns>The distance in kilometers</returns>
        public async Task<double> CalculateDistanceAsync(string location1, string location2)
        {
            var coords1 = await GetCoordinatesAsync(location1);
            var coords2 = await GetCoordinatesAsync(location2);
            return CalculateDistance(coords1, coords2);
        }

        /// <summary>
        /// Calculates the distance between two coordinates using the Haversine formula
        /// </summary>
        /// <param name=""coords1"">The first coordinates</param>
        /// <param name=""coords2"">The second coordinates</param>
        /// <returns>The distance in kilometers</returns>
        public static double CalculateDistance(Coordinates coords1, Coordinates coords2)
        {
            const double EarthRadiusKm = 6371.0;
            
            var dLat = DegreesToRadians(coords2.Latitude - coords1.Latitude);
            var dLon = DegreesToRadians(coords2.Longitude - coords1.Longitude);
            
            var lat1 = DegreesToRadians(coords1.Latitude);
            var lat2 = DegreesToRadians(coords2.Latitude);
            
            var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                    Math.Sin(dLon/2) * Math.Sin(dLon/2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            
            return EarthRadiusKm * c;
        }
        
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }

    /// <summary>
    /// Interface for location data providers
    /// </summary>
    public interface ILocationProvider
    {
        /// <summary>
        /// Gets coordinates for a location name
        /// </summary>
        Task<Coordinates> GetCoordinatesAsync(string locationName);

        /// <summary>
        /// Gets address for coordinates
        /// </summary>
        Task<string> GetAddressAsync(Coordinates coordinates);
    }

    /// <summary>
    /// Represents geographic coordinates
    /// </summary>
    public class Coordinates
    {
        /// <summary>
        /// The latitude in degrees
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude in degrees
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Creates a new instance of Coordinates
        /// </summary>
        public Coordinates() { }

        /// <summary>
        /// Creates a new instance of Coordinates with the specified values
        /// </summary>
        /// <param name=""latitude"">The latitude in degrees</param>
        /// <param name=""longitude"">The longitude in degrees</param>
        public Coordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Returns a string representation of the coordinates
        /// </summary>
        public override string ToString()
        {
            return $""{Latitude:F6}, {Longitude:F6}"";
        }
    }
}");

            return sampleDir;
        }
    }
}