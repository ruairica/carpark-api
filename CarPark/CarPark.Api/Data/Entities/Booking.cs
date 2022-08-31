using System;
using CarPark.Api.Data.Enums;

namespace CarPark.Api.Data.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public BookingStatus Status { get; set; }
    }
}
