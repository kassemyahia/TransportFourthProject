namespace TransportFourthProject.Api.DTOs.Employee
{
    public class DriverTripsCountDto
    {
        public int Count { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class DriverYearlyTripsCountDto
    {
        public int Count { get; set; }
        public int Year { get; set; }
    }
}
