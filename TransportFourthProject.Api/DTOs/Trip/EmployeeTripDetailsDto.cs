namespace TransportFourthProject.Api.DTOs.Trip
{
    public class EmployeeTripDetailsDto
    {
        public int TripId { get; set; }

        public string DepartureTime { get; set; } = string.Empty;
        public string ArrivalTime { get; set; } = string.Empty;
        public string StartCity { get; set; } = string.Empty;
        public string EndCity { get; set; } = string.Empty;

        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;

        public int AvailableSeats { get; set; }
        public string TripStatus { get; set; } = string.Empty;

        public string BasePrice { get; set; } = string.Empty;

        public string DriverName { get; set; } = string.Empty;

        public string DiscountName { get; set; } = string.Empty;
        public string DiscountPercentage { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } 
        public int TotalBookings { get; set; }
    }
}
