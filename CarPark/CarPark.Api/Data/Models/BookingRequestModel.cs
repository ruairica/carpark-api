using System;
using System.ComponentModel.DataAnnotations;

namespace CarPark.Api.Data.Models
{
    public class BookingRequestModel
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}
