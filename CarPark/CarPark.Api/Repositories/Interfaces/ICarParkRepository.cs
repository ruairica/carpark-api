using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarPark.Api.Data.Models;

namespace CarPark.Api.Repositories.Interfaces
{
    public interface ICarParkRepository
    {
        Task<Guid> AddReservation(DateTime start, DateTime end);

        Task<List<AvailabilityModel>> GetAvailability(DateTime start, DateTime end);

        Task<List<PriceModel>> GetPrices(DateTime start, DateTime end);

        Task<Guid> CancelBooking(Guid id);

        Task<Guid> AmendBooking(Guid id, DateTime updatedStart, DateTime updatedEnd);
    }
}
