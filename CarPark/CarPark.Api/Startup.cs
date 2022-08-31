using System;
using CarPark.Api.Data;
using CarPark.Api.Repositories;
using CarPark.Api.Repositories.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CarPark.Api.Startup))]


namespace CarPark.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = this.GetType().Name,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
            }.ToString());

            builder.Services.AddDbContext<CarParkContext>(options => options.UseSqlite(sqliteConnection).EnableSensitiveDataLogging().EnableDetailedErrors());
            builder.Services.AddLogging();
            builder.Services.AddTransient<ICarParkRepository, CarParkRepository>();
        }
    }
}
