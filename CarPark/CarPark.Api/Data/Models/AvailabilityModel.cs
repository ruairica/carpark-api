using System;

namespace CarPark.Api.Data.Models
{
    public class AvailabilityModel
    {
        public DateTime Date { get; set; }

        public int AvailableSpaces { get; set; }
    }
}
