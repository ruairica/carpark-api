using System;
using System.ComponentModel.DataAnnotations;

namespace CarPark.Api.Data.Entities
{
    public class Inventory
    {
        [Key]
        public DateTime Date { get; set; }

        public int TotalSpaces { get; set; }

        public int ReservedSpaces { get; set; }
    }
}
