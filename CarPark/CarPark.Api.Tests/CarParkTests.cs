using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CarPark.Api.Controller;
using CarPark.Api.Data;
using CarPark.Api.Data.Entities;
using CarPark.Api.Data.Enums;
using CarPark.Api.Data.Models;
using CarPark.Api.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CarPark.Api.Tests
{
    [TestFixture]
    public class CarParkTests
    {
        private readonly CarParkController _controller;
        private readonly CarParkContext _context;
        public CarParkTests()
        {
            var sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = this.GetType().Name,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<CarParkContext>(options => options.UseSqlite(sqliteConnection).EnableSensitiveDataLogging().EnableDetailedErrors());
            var sp = serviceCollection.BuildServiceProvider();

            this._context = sp.GetRequiredService<CarParkContext>();
            var repo = new CarParkRepository(this._context);
            this._controller = new CarParkController(repo);
        }

        [TearDown]
        public void TearDown()
        {
            this._context.Bookings.RemoveRange(this._context.Bookings);
            var inv = this._context.Inventory.ToList();
            inv.ForEach(x => x.ReservedSpaces = 0);
            this._context.SaveChanges();
            this._context.Bookings.ToList().Should().BeEmpty();
        }

        [Test]
        public async Task GetPrices()
        {
            var req = new DefaultHttpContext().Request;
            req.QueryString = new QueryString("?startDate=2022-10-10&endDate=2022-10-20");
            var result = (await this._controller.Prices(req)).Value;
            result.Count.Should().Be(11);
            result.All(x => x.Price == 15.00M).Should().BeTrue();
        }

        [Test]
        public async Task GetPricesInvalidDate()
        {
            var req = new DefaultHttpContext().Request;
            
            // date is in the past
            req.QueryString = new QueryString("?startDate=2021-10-10&endDate=2022-10-20");
            var actionResult = (await this._controller.Prices(req)).Result;
            actionResult.Should().BeOfType<BadRequestObjectResult>()
                .Which.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GetPricesInvalidQueryString()
        {
            var req = new DefaultHttpContext().Request;

            // date is in the past
            req.QueryString = new QueryString("?startDate=2022/10/10&endDate=2022-10-20");
            var actionResult = (await this._controller.Prices(req)).Result;
            actionResult.Should().BeOfType<BadRequestObjectResult>()
                .Which.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task AddBooking()
        {
            var req = new DefaultHttpContext().Request;

            var startDate = DateTime.UtcNow.AddDays(100).Date;
            var endDate = DateTime.UtcNow.AddDays(102).Date;
            var reqBody = new BookingRequestModel
            {
                StartDate = startDate,
                EndDate = endDate,
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reqBody)));
            req.Body = stream;
            req.ContentLength = stream.Length;

            var result =  (await this._controller.AddBooking(req)).Value;

            result.Should().NotBe(Guid.Empty);

            var record = await this._context.Bookings.FindAsync(result);
            record.Start.Should().Be(startDate);
            record.End.Should().Be(endDate);

            var inv = await this._context.Inventory.Where(x => x.Date >= startDate && x.Date <= endDate)
                .Select(x => x.ReservedSpaces).ToListAsync();

            inv.All(x => x == 1).Should().BeTrue();
        }

        [Test]
        public async Task AddBooking_FullCarPark()
        {
            var date = DateTime.UtcNow.AddDays(30).Date;
            var inventory = await this._context.Inventory.FindAsync(date);
            inventory.Should().NotBe(null);
            inventory.ReservedSpaces = inventory.TotalSpaces;
            await this._context.SaveChangesAsync();


            var req = new DefaultHttpContext().Request;

            var startDate = DateTime.UtcNow.AddDays(20).Date;
            var reqBody = new BookingRequestModel
            {
                StartDate = date,
                EndDate = date,
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reqBody)));
            req.Body = stream;
            req.ContentLength = stream.Length;

            var actionResult = (await this._controller.AddBooking(req)).Result;
            actionResult.Should().BeOfType<BadRequestObjectResult>()
                .Which.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task CancelBooking()
        {
            var bookingId = Guid.NewGuid();
            var bookingDate = DateTime.UtcNow.AddYears(1).Date;

            var booking = new Booking
            {
                Id = bookingId,
                Start = bookingDate,
                End = bookingDate,
                Status = BookingStatus.Confirmed,
            };

            await this._context.Bookings.AddAsync(booking);
            var inventory = await this._context.Inventory.FindAsync(bookingDate);
            inventory.ReservedSpaces += 1;
            await this._context.SaveChangesAsync();

            await this._controller.CancelBooking(new DefaultHttpContext().Request, bookingId);

            (await this._context.Bookings.FindAsync(bookingId)).Status.Should().Be(BookingStatus.Cancelled);
            (await this._context.Inventory.FindAsync(bookingDate)).ReservedSpaces.Should().Be(0);
        }

        [Test]
        public async Task Availability()
        {
            var date = new DateTime(2023, 10, 10);
            var inv = await this._context.Inventory.FindAsync(date);
            inv.ReservedSpaces = inv.TotalSpaces;
            await this._context.SaveChangesAsync();

            var req = new DefaultHttpContext().Request;
            req.QueryString = new QueryString("?startDate=2023-10-10&endDate=2023-10-11");

            var result = (await this._controller.Availability(req)).Value;
            result.Count.Should().Be(2);
            result.First().AvailableSpaces.Should().Be(0);
            result.Last().AvailableSpaces.Should().Be(10);
        }

        [Test]
        public async Task Amend_ChangeDates()
        {
            var bookingId = Guid.NewGuid();
            var bookingDate = DateTime.UtcNow.Date.AddYears(1).AddDays(1);

            var booking = new Booking
            {
                Id = bookingId,
                Start = bookingDate,
                End = bookingDate,
                Status = BookingStatus.Confirmed,
            };

            await this._context.Bookings.AddAsync(booking);
            var inventory = await this._context.Inventory.FindAsync(bookingDate);
            inventory.ReservedSpaces += 1;
            await this._context.SaveChangesAsync();

            var req = new DefaultHttpContext().Request;
            var updatedBookingDate = bookingDate.AddDays(1).Date;
            var reqBody = new BookingRequestEditModel()
            {
                Id = bookingId,
                StartDate = updatedBookingDate,
                EndDate = updatedBookingDate,
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reqBody)));
            req.Body = stream;
            req.ContentLength = stream.Length;
            var result = (await this._controller.AmendBooking(req)).Value;
            result.Should().Be(bookingId);


            (await this._context.Inventory.FindAsync(bookingDate)).ReservedSpaces.Should().Be(0);
            (await this._context.Inventory.FindAsync(updatedBookingDate)).ReservedSpaces.Should().Be(1);

            var updatedBooking = await this._context.Bookings.FindAsync(bookingId);
            updatedBooking.Start.Should().Be(updatedBookingDate);
            updatedBooking.End.Should().Be(updatedBookingDate);
        }

        [Test]
        public async Task Amend_ExpandBooking()
        {
            var bookingId = Guid.NewGuid();
            var bookingDate = DateTime.UtcNow.Date.AddYears(1).AddDays(1);

            var booking = new Booking
            {
                Id = bookingId,
                Start = bookingDate,
                End = bookingDate,
                Status = BookingStatus.Confirmed,
            };

            await this._context.Bookings.AddAsync(booking);
            var inventory = await this._context.Inventory.FindAsync(bookingDate);
            inventory.ReservedSpaces += 1;
            await this._context.SaveChangesAsync();

            var req = new DefaultHttpContext().Request;
            var updatedBookingDate = bookingDate.AddDays(1).Date;
            var reqBody = new BookingRequestEditModel()
            {
                Id = bookingId,
                StartDate = bookingDate,
                EndDate = updatedBookingDate,
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reqBody)));
            req.Body = stream;
            req.ContentLength = stream.Length;
            var result = (await this._controller.AmendBooking(req)).Value;
            result.Should().Be(bookingId);


            (await this._context.Inventory.FindAsync(bookingDate)).ReservedSpaces.Should().Be(1);
            (await this._context.Inventory.FindAsync(updatedBookingDate)).ReservedSpaces.Should().Be(1);

            var updatedBooking = await this._context.Bookings.FindAsync(bookingId);
            updatedBooking.Start.Should().Be(bookingDate);
            updatedBooking.End.Should().Be(updatedBookingDate);
        }

        [Test]
        public async Task Amend_ExpandBooking_InvalidDueToFullCarPark()
        {
            var bookingId = Guid.NewGuid();
            var bookingDate = DateTime.UtcNow.Date.AddYears(1).AddDays(1);

            var booking = new Booking
            {
                Id = bookingId,
                Start = bookingDate,
                End = bookingDate,
                Status = BookingStatus.Confirmed,
            };

            await this._context.Bookings.AddAsync(booking);
            var inventory = await this._context.Inventory.FindAsync(bookingDate);
            inventory.ReservedSpaces += 1;

            var updatedBookingDate = bookingDate.AddDays(1).Date;

            var maxInv = await this._context.Inventory.FindAsync(updatedBookingDate);
            maxInv.ReservedSpaces = maxInv.TotalSpaces;

            await this._context.SaveChangesAsync();

            var req = new DefaultHttpContext().Request;
            var reqBody = new BookingRequestEditModel()
            {
                Id = bookingId,
                StartDate = bookingDate,
                EndDate = updatedBookingDate,
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reqBody)));
            req.Body = stream;
            req.ContentLength = stream.Length;

            var actionResult = (await this._controller.AmendBooking(req)).Result;
            actionResult.Should().BeOfType<BadRequestObjectResult>()
                .Which.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        }
    }
}