namespace TransportFourthProject.Api.DTOs.Trip
{
    public class AvailableBusesForAddTripForEmployeeDto
    {
        public int BusId { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; }
        public int Capacity { get; set; }
    }
}
