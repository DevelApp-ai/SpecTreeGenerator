using System;
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
        /// <param name="weatherProvider">The weather provider to use</param>
        /// <param name="weatherFormatter">The weather formatter to use</param>
        public WeatherService(IWeatherProvider weatherProvider, IWeatherFormatter weatherFormatter)
        {
            _weatherProvider = weatherProvider ?? throw new ArgumentNullException(nameof(weatherProvider));
            _weatherFormatter = weatherFormatter ?? throw new ArgumentNullException(nameof(weatherFormatter));
        }

        /// <summary>
        /// Gets the current weather for a location
        /// </summary>
        /// <param name="location">The location to get weather for</param>
        /// <returns>The formatted weather data</returns>
        public async Task<string> GetWeatherAsync(string location)
        {
            var weatherData = await _weatherProvider.GetWeatherDataAsync(location);
            return _weatherFormatter.Format(weatherData);
        }

        /// <summary>
        /// Gets the weather forecast for a location
        /// </summary>
        /// <param name="location">The location to get forecast for</param>
        /// <param name="days">Number of days to forecast</param>
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
        public string Location { get; set; } = string.Empty;

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
        public string Condition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents forecast data for a location
    /// </summary>
    public class ForecastData
    {
        /// <summary>
        /// The location this forecast is for
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// The daily forecasts
        /// </summary>
        public DailyForecast[] DailyForecasts { get; set; } = Array.Empty<DailyForecast>();
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
        public string Condition { get; set; } = string.Empty;
    }
}