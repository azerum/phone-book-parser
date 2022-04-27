using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook, IDisposable, IAsyncDisposable
    {
        private readonly IPhoneBook inner;
        private readonly SqliteConnection connection;

        private CachingSqlitePhoneBook(
            IPhoneBook inner,
            SqliteConnection connection
        )
        {
            this.inner = inner;
            this.connection = connection;
        }

        public static CachingSqlitePhoneBook Create(
            IPhoneBook inner,
            string connectionString
        )
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            CachingDbBootstrap dbBootstrap = new(connection);

            dbBootstrap.InitRegionTable();
            dbBootstrap.InitProvinceTable();
            dbBootstrap.InitCityTable();

            return new(inner, connection);
        }

        public async IAsyncEnumerable<Region> GetAllRegionsAsync()
        {
            var regions = connection.Query<Region>(
                "SELECT Url, DisplayName FROM Region"
            );

            if (regions.Any())
            {
                foreach (Region r in regions)
                {
                    yield return r;
                }

                yield break;
            }

            var transaction = await connection.BeginTransactionAsync();

            await foreach (Region r in inner.GetAllRegionsAsync())
            {
                await connection.ExecuteAsync(
                    @"
                    INSERT OR IGNORE INTO Region(Url, DisplayName)
                    VALUES (@Url, @DisplayName)
                    ",
                    r,
                    transaction
                );

                yield return r;
            }

            await transaction.CommitAsync();
        }

        public IAsyncEnumerable<Province> GetAllProvincesInRegionAsync(
            Region region
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<City> GetAllCitiesInProvinceAsync(
            Province province
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInAllAsync(
            SearchCriteria criteria
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInCityAsync(
            City city,
            SearchCriteria criteria
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInProvinceAsync(
            Province province,
            SearchCriteria criteria
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInRegionAsync(
            Region region,
            SearchCriteria criteria
        )
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return connection.DisposeAsync();
        }
    }
}
