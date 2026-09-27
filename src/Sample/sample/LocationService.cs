using System;
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
        /// <param name="locationProvider">The location provider to use</param>
        public LocationService(ILocationProvider locationProvider)
        {
            _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
        }

        /// <summary>
        /// Gets the coordinates for a location
        /// </summary>
        /// <param name="locationName">The name of the location</param>
        /// <returns>The coordinates of the location</returns>
        public async Task<Coordinates> GetCoordinatesAsync(string locationName)
        {
            return await _locationProvider.GetCoordinatesAsync(locationName);
        }

        /// <summary>
        /// Gets the address for coordinates
        /// </summary>
        /// <param name="coordinates">The coordinates to get the address for</param>
        /// <returns>The address at the coordinates</returns>
        public async Task<string> GetAddressAsync(Coordinates coordinates)
        {
            return await _locationProvider.GetAddressAsync(coordinates);
        }

        /// <summary>
        /// Calculates the distance between two locations
        /// </summary>
        /// <param name="location1">The first location</param>
        /// <param name="location2">The second location</param>
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
        /// <param name="coords1">The first coordinates</param>
        /// <param name="coords2">The second coordinates</param>
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
        /// <param name="latitude">The latitude in degrees</param>
        /// <param name="longitude">The longitude in degrees</param>
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
            return $"{Latitude:F6}, {Longitude:F6}";
        }
    }
}