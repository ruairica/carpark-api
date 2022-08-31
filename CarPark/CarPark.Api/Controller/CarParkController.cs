using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CarPark.Api.Data.Models;
using CarPark.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CarPark.Api.Controller
{
    public class CarParkController
    {
        private readonly ICarParkRepository _repository;
        private const string DateFormat = "yyyy-MM-dd";
        public CarParkController(ICarParkRepository repository)
        {
            this._repository = repository;
        }

        [FunctionName("AmendBooking")]
        public async Task<ActionResult<Guid>> AmendBooking([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CarPark/Booking/Edit")] HttpRequest req)
        {
            var reqModel = JsonConvert.DeserializeObject<BookingRequestEditModel>(await new StreamReader(req.Body).ReadToEndAsync());
            if (InvalidDateRange(reqModel.StartDate, reqModel.EndDate))
            {
                return new BadRequestObjectResult("Invalid date range");
            }

            var id = await this._repository.AmendBooking(reqModel.Id, reqModel.StartDate, reqModel.EndDate);

            if (id == Guid.Empty)
            {
                return new BadRequestObjectResult("Unable to change booking");
            }

            return id;
        }

        [FunctionName("CancelBooking")]
        public async Task<ActionResult<Guid>> CancelBooking(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CarPark/Booking/Cancel/{id}")] HttpRequest req, Guid id)
        {
            var result = await this._repository.CancelBooking(id);

            if (result == Guid.Empty)
            {
                return new BadRequestObjectResult($"Could not find an open booking with the id: {id}");
            }

            return result;
        }

        [FunctionName("AddBooking")]
        public async Task<ActionResult<Guid>> AddBooking([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CarPark/Booking")] HttpRequest req)
        {
            var reqModel = JsonConvert.DeserializeObject<BookingRequestModel>(await new StreamReader(req.Body).ReadToEndAsync());
            if (InvalidDateRange(reqModel.StartDate, reqModel.EndDate))
            {
                return new BadRequestObjectResult("Invalid Request");
            }

            var id = await this._repository.AddReservation(reqModel.StartDate, reqModel.EndDate);

            if (id == Guid.Empty)
            {
                return new BadRequestObjectResult("One or more days in the selected range has no spaces available to book.");
            }

            return id;
        }

        [FunctionName("Availability")]
        public async Task<ActionResult<List<AvailabilityModel>>> Availability([HttpTrigger(AuthorizationLevel.Function, "get", Route = "CarPark/Availability")] HttpRequest req)
        {
            if (!DateTime.TryParseExact(req.Query["startDate"], DateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var startDate))
            {
                return new BadRequestObjectResult($"startDate was not supplied in {DateFormat} format");
            }

            if (!DateTime.TryParseExact(req.Query["endDate"], DateFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var endDate))
            {
                return new BadRequestObjectResult($"endDate was not supplied in {DateFormat} format");
            }

            var availability = await this._repository.GetAvailability(startDate, endDate);

            return availability;
        }

        [FunctionName("Prices")]
        public async Task<ActionResult<List<PriceModel>>> Prices([HttpTrigger(AuthorizationLevel.Function, "get", Route = "CarPark/Prices")] HttpRequest req)
        {
            if (!DateTime.TryParseExact(req.Query["startDate"], DateFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var startDate))
            {
                return new BadRequestObjectResult($"startDate was not supplied in {DateFormat} format");
            }

            if (!DateTime.TryParseExact(req.Query["endDate"], DateFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var endDate))
            {
                return new BadRequestObjectResult($"endDate was not supplied in {DateFormat} format");
            }

            if (InvalidDateRange(startDate, endDate))
            {
                return new BadRequestObjectResult("Invalid date range");
            }

            return await this._repository.GetPrices(startDate, endDate);
        }

        private static bool InvalidDateRange(DateTime start, DateTime end)
        {
            var maxDate = DateTime.UtcNow.Date.AddYears(2);
            return start == default || end == default ||
                   start.Date < DateTime.UtcNow.Date || end.Date > maxDate ||
                   end.Date < start.Date;
        }
    }
}
