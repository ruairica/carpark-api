# CarParkApi
C# .NET 6 Api running built as an Azure Function App.

The project use Sqlite as an in-memory database, this was used for prototyping purposes only to make it simpler for anyone to get the API running locally as quickly as possible. The database persists only as long as the application is running, however this could easily be swapped out for a SQL Server database. When running the project locally it will start on port/url: http://localhost:7192/api/

## Requirements to run
.NET 6 SDK

[Azure Function Core Tools 4.X](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash)

Azurite Storage Emulator (Included with VS 2022)


## API EndPoints
#### Prices [GET]
Query string Parameters:

startDate: start of the date range in format yyyy-MM-dd

endDate: end of the date range in format yyyy-MM-dd

eg.
http://localhost:7192/api/CarPark/Prices?startDate=2022-10-10&endDate=2022-10-12

Returns:
a list of Objects containing the date and the corresponding price

#### Availability [GET]
Query string Parameters:

startDate: start of the date range in format yyyy-MM-dd

endDate: end of the date range in format yyyy-MM-dd

eg.
http://localhost:7192/api/CarPark/Availability?startDate=2022-10-10&endDate=2022-10-12

Returns:
A list of objects containing the date and the number of available of spaces on that day.

#### Add booking [POST]
Request Body example:

StartDate: The start date of the booking

EndDate: The end date of the booking

```
{
    "StartDate": "2022-10-10T00:00:00",
    "EndDate": "2022-10-13T00:00:00"
}
```

http://localhost:7192/api/CarPark/Booking

Returns:
The id of the created booking, to be used for cancelling or amending the bookings.

#### Cancel Booking [PATCH]
Route Parameter: 

{id} is the Id of the booking to be cancelled.

eg.
http://localhost:7192/api/CarPark/Booking/Cancel/{id}

Returns:
The id of the booking

#### Amend Booking [PATCH]

Request Body example:

id: the original booking Id

StartDate: The updated start date

EndDate: The updated end date
```
{
    "Id": {id}
    "StartDate": "2022-10-10T00:00:00",
    "EndDate": "2022-10-13T00:00:00"
}
```
eg.
http://localhost:7192/api/CarPark/Booking/Edit

Returns:
The id of the booking


## Database Tables

### Booking
Id (Guid) [PK]

Start (DateTime)

End (DateTime)

Status (int)

### Inventory
Date (DateTime) [PK]

TotalSpaces (int)

ReservedSpaces (int)

### Rate
Date (DateTime) [PK]

Price (Decimal)

## Assumptions/Constraints
###### A booking is up to an including the end date.
###### Users can book a parking space up to 2 years away.





