using System;
using System.ComponentModel.DataAnnotations;

namespace CarPark.Api.Data.Entities
{
    public class Rate
    {
        [Key]
        public DateTime Date { get; set; }

        public decimal Price { get; set; }
    }
}
