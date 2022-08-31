using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarPark.Api.Data;
using CarPark.Api.Data.Entities;
using CarPark.Api.Data.Enums;
using CarPark.Api.Data.Models;
using CarPark.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Repositories
{
    public class CarParkRepository : ICarParkRepository
    {
        private readonly CarParkContext _context;

        public CarParkRepository(CarParkContext context)
        {
            this._context = context;
            this._context.Database.OpenConnection();
            this._context.Database.EnsureCreated();
        }

        public async Task<Guid> AddReservation(DateTime start, DateTime end)
        {
            if (await this.AvailableToBook(start, end))
            {
                var id = Guid.NewGuid();
                var model = new Booking
                {
                    Id = id,
                    Start = start.Date,
                    End = end.Date,
                    Status = BookingStatus.Confirmed,
                };

                await this._context.Bookings.AddAsync(model);


                var inventory = await this._context.Inventory
                    .Where(x => x.Date.Date >= start.Date && x.Date.Date <= end.Date).ToListAsync();
                inventory.ForEach(i => i.ReservedSpaces += 1);

                await this._context.SaveChangesAsync();
                return id;
            }

            
            return Guid.Empty;
        }

        public async Task<List<AvailabilityModel>> GetAvailability(DateTime start, DateTime end) =>
            await this._context.Inventory.AsNoTracking().Where(x => x.Date.Date >= start.Date && x.Date.Date <= end.Date)
                .Select(x => new AvailabilityModel
                {
                    Date = x.Date,
                    AvailableSpaces = x.TotalSpaces - x.ReservedSpaces,
                }).ToListAsync();

        public async Task<List<PriceModel>> GetPrices(DateTime start, DateTime end) =>
            await this._context.Rates.AsNoTracking().Where(x => x.Date >= start && x.Date <= end).Select(x => new PriceModel
            {
                Date = x.Date,
                Price = x.Price,
            }).ToListAsync();

        public async Task<Guid> CancelBooking(Guid id)
        {
            var booking =
                await this._context.Bookings.FirstOrDefaultAsync(x =>
                    x.Id == id && x.Status == BookingStatus.Confirmed);

            if (booking == null)
            {
                return Guid.Empty;
            }

            booking.Status = BookingStatus.Cancelled;

            var inventory = await this._context.Inventory
                .Where(x => x.Date.Date >= booking.Start.Date && x.Date.Date <= booking.End.Date).ToListAsync();

            inventory.ForEach(i => i.ReservedSpaces -= 1);


            await this._context.SaveChangesAsync();
            return id;
        }

        public async Task<Guid> AmendBooking(Guid id, DateTime updatedStart, DateTime updatedEnd)
        {
            var booking =
                await this._context.Bookings.FirstOrDefaultAsync(x =>
                    x.Id == id && x.Status == BookingStatus.Confirmed);

            if (booking == null)
            {
                return Guid.Empty;
            }

            var isAvailable = await this.IsAvailableForAmend(updatedStart, updatedEnd, booking);

            if (!isAvailable)
            {
                return Guid.Empty;
            }

            var increaseInventory = await this._context.Inventory
                .Where(x => x.Date.Date >= updatedStart.Date && x.Date.Date <= updatedEnd.Date).ToListAsync();
            increaseInventory.ForEach(i => i.ReservedSpaces += 1);

            var decreaseInventory = await this._context.Inventory
                .Where(x => x.Date.Date >= booking.Start.Date && x.Date.Date <= booking.End.Date).ToListAsync();
            decreaseInventory.ForEach(i => i.ReservedSpaces -= 1);

            booking.Start = updatedStart;
            booking.End = updatedEnd;

            await this._context.SaveChangesAsync();
            return id;
        }

        // only check availability outside the booking's current date range
        private async Task<bool> IsAvailableForAmend(DateTime updatedStart, DateTime updatedEnd, Booking booking) =>
            !(await this._context.Inventory.AsNoTracking().AnyAsync(x =>
                x.Date.Date >= updatedStart.Date &&
                x.Date.Date <= updatedEnd.Date &&
                !(x.Date.Date >= booking.Start.Date && x.Date.Date <= booking.End.Date) &&
                x.ReservedSpaces == x.TotalSpaces));
        

        private async Task<bool> AvailableToBook(DateTime start, DateTime end) =>
            !(await this._context.Inventory.AsNoTracking().AnyAsync(x =>
                x.Date.Date >= start.Date && x.Date.Date <= end.Date && x.ReservedSpaces == x.TotalSpaces));
    }
}
