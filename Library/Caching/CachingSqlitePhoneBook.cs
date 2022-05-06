using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Library.Caching
{
    public class CachingSqlitePhoneBook : IPhoneBook
    {
        private readonly IPhoneBook inner;
        private readonly string connectionString;

        private CachingSqlitePhoneBook(
            IPhoneBook inner,
            string connectionString
        )
        {
            this.inner = inner;
            this.connectionString = connectionString;
        }

        public static CachingSqlitePhoneBook Open(
            IPhoneBook inner,
            string connectionString
        )
        {
            using SqliteConnection connection = new(connectionString);

            connection.Open();
            connection.EnsureAllTablesAreCreated();

            return new(inner, connectionString);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await using var connection = OpenConnection();
            var regions = await connection.SelectAllRegions();

            if (regions.Any())
            {
                foreach (Region r in regions)
                {
                    yield return r;
                }

                yield break;
            }

            var newRegions = inner.GetAllRegions(cancellationToken);
            List<Region> toInsert = new();

            await foreach (Region r in newRegions)
            {
                toInsert.Add(r);
                yield return r;
            }

            await connection.InsertMany(toInsert, cancellationToken);
        }

        public IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<FoundRecord> SearchInCity(
            City city,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            return inner.SearchInCity(city, criteria, cancellationToken);
        }

        private SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new(connectionString);
            connection.Open();

            return connection;
        }
    }
}
