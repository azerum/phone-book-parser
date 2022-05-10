using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Library.Caching
{
    internal sealed class CacheDb : IDisposable
    {
        private readonly QueryFactory db;

        private CacheDb(SqliteConnection connection)
        {
            db = new(connection, new SqliteCompiler());
        }

        public static async Task<CacheDb> OpenAsync(string connectionString)
        {
            SqliteConnection connection = new(connectionString);
            await connection.OpenAsync();

            return new(connection);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public void EnsureAllTablesAreCreated()
        {
            var transaction = db.Connection.BeginTransaction();

            try
            {
                EnsureRootTableIsCreated(transaction);
                EnsureRegionsTableIsCreated(transaction);
                EnsureProvincesTableIsCreated(transaction);
                EnsureCitiesTableIfCreated(transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private void EnsureRootTableIsCreated(IDbTransaction transaction)
        {
            string createTable =
            @"
            create table if not exists Root(
                Id integer primary key check(Id = 0),
                AllRegionsAreCached int not null
            )
            ";

            db.Statement(createTable, transaction);

            //If row is already inserted, then insert statement will
            //be ignored and AllRegionsAreCached won't change in the row
            string insertOrIgnore =
            @"
            insert or ignore into Root (Id, AllRegionsAreCached)
            values (0, 0)
            ";

            db.Statement(insertOrIgnore, transaction);
        }

        private void EnsureRegionsTableIsCreated(IDbTransaction transaction)
        {
            string createTable =
            @"
            create table if not exists Regions(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                AllProvincesAreCached int default 0 not null,
                unique(Url)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Regions_DisplayName
            on Regions (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }

        private void EnsureProvincesTableIsCreated(IDbTransaction transaction)
        {
            string createTable =
            @"
            create table if not exists Provinces(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                RegionId int not null,
                AllCitiesAreCached int default 0 not null,
                unique(Url),
                foreign key (RegionId) references Regions (Id)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Provinces_DisplayName
            on Provinces (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }

        private void EnsureCitiesTableIfCreated(IDbTransaction transaction)
        {
            string createTable =
            @"
            create table if not exists Cities(
                Id integer primary key,
                Url text not null,
                DisplayName text not null,
                ProvinceId int not null,
                unique(Url),
                foreign key (ProvinceId) references Provinces (Id)
            )
            ";

            string createIndex =
            @"
            create unique index if not exists UQIX_Cities_DisplayName
            on Cities (DisplayName)
            ";

            db.Statement(createTable, transaction);
            db.Statement(createIndex, transaction);
        }

        private readonly Query selectRegions = new Query()
            .Select("r.Url", "r.DisplayName")
            .From("Regions as r");

        private readonly Query selectProvinces = new Query()
            .Select(
                "p.Url as PUrl",
                "p.DisplayName as PDisplayName",
                "r.Url as RUrl",
                "r.DisplayName as RDisplayName"
            )
            .From("Provinces as p")
            .Join("Regions as r", "p.RegionId", "r.Id");

        private readonly Query selectCities = new Query()
            .Select(
                "c.Url as CUrl",
                "c.DisplayName as CDisplayName",
                "p.Url as PUrl",
                "p.DisplayName as PDisplayName",
                "r.Url as RUrl",
                "r.DisplayName as RDisplayName"
            )
            .From("Cities as c")
            .Join("Provinces as p", "c.ProvinceId", "p.Id")
            .Join("Regions as r", "p.RegionId", "r.Id");

        private readonly Query selectRegionsIds = new Query()
            .Select("Id")
            .From("Regions");

        public async Task<IEnumerable<Region>> SelectAllRegions(
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .FromQuery(selectRegions)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(Mappers.ToRegion);
        }

        public async Task<IEnumerable<Province>> SelectProvincesByRegionId(
            long regionId,
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .FromQuery(selectProvinces)
                .Where("p.RegionId", "=", regionId)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(Mappers.ToProvince);
        }

        public async Task<IEnumerable<City>> SelectCitiesByProvinceId(
            long provinceId,
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .FromQuery(selectCities)
                .Where("c.ProvinceId", "=", provinceId)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(Mappers.ToCity);
        }

        public async Task<IEnumerable<City>> SelectAllCities(
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .FromQuery(selectCities)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(Mappers.ToCity);
        }

        public async Task<IEnumerable<(long id, Region region)>>
        SelectRegionsWithNotAllProvincesCached(
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .Query("Regions")
                .Select("Id", "Url", "DisplayName")
                .Where("AllProvincesAreCached", "=", 0)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(d =>
            {
                long id = d.Id;
                Region region = Mappers.ToRegion(d);

                return (id, region);
            });
        }

        public async Task<IEnumerable<(long id, Province province)>>
        SelectProvincesWithNotAllCitiesCached(
            CancellationToken cancellationToken
        )
        {
            var dynamics = await db
                .Query("Provinces as p")
                .Select(
                    "p.Id",
                    "p.Url as PUrl",
                    "p.DisplayName as PDisplayName",
                    "r.Url as RUrl",
                    "r.DisplayName as RDisplayName"
                )
                .Join("Regions as r", "p.RegionId", "r.Id")
                .Where("AllCitiesAreCached", "=", 0)
                .GetAsync(cancellationToken: cancellationToken);

            return dynamics.Select(d =>
            {
                long id = d.Id;
                Province province = Mappers.ToProvince(d);

                return (id, province);
            });
        }

        public Task<bool> AreAllRegionsCached(CancellationToken cancellationToken)
        {
            return db.Query("Root")
                .Select("AllRegionsAreCached")
                .FirstAsync<bool>(cancellationToken: cancellationToken);
        }

        public record RegionInfo(long Id, bool AllProvincesAreCached);

        public async Task<RegionInfo?> SelectRegionInfo(
            Region region,
            CancellationToken cancellationToken
        )
        {
            var d = await db
                .FromQuery(selectRegionsIds)
                .Select("AllProvincesAreCached")
                .Where("DisplayName", "=", region.DisplayName)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (d == null)
            {
                return null;
            }

            return new(d.Id, d.AllProvincesAreCached == 1);
        }

        public record ProvinceInfo(long Id, bool AllCitiesAreCached);

        public async Task<ProvinceInfo?> SelectProvinceInfo(
            Province province,
            CancellationToken cancellationToken
        )
        {
            var d = await db
                .Query("Provinces")
                .Select("Id", "AllCitiesAreCached")
                .Where("DisplayName", "=", province.DisplayName)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (d == null)
            {
                return null;
            }

            return new(d.Id, d.AllCitiesAreCached == 1);
        }

        public Task<long> InsertRegion(
            Region region,
            CancellationToken cancellationToken
        )
        {
            return db.Query("Regions").InsertGetIdAsync<long>(
                new { region.Url, region.DisplayName },
                cancellationToken: cancellationToken
            );
        }

        public async Task<long> InsertProvince(
            Province province,
            CancellationToken cancellationToken
        )
        {
            long? maybeRegionId = await db
                .FromQuery(selectRegionsIds)
                .Where("DisplayName", "=", province.Region.DisplayName)
                .FirstOrDefaultAsync<long?>(cancellationToken: cancellationToken);

            long regionId;

            if (maybeRegionId.HasValue)
            {
                regionId = maybeRegionId.Value;
            }
            else
            {
                regionId = await InsertRegion(province.Region, cancellationToken);
            }

            return await db.Query("Provinces").InsertGetIdAsync<long>(
                new { province.Url, province.DisplayName, RegionId = regionId },
                cancellationToken: cancellationToken
            );
        }

        public async Task CacheAllRegions(
            IEnumerable<Region> regions,
            CancellationToken cancellationToken
        )
        {
            //We need to use 'insert or ignore' as even though *all*
            //regions are not cached, there might be *some* regions
            //in the cache and hence some inserts might fails as
            //they could violate unique constraints
            string insertOrIgnore =
            @"
            insert or ignore into Regions (Url, DisplayName)
            values (@Url, @DisplayName)
            ";

            var transaction = db.Connection.BeginTransaction();

            try
            {
                await db.Query("Root").UpdateAsync(
                    new { AllRegionsAreCached = 1 },
                    transaction,
                    cancellationToken: cancellationToken
                );

                foreach (Region r in regions)
                {
                    await db.StatementAsync(
                        insertOrIgnore,
                        r,
                        transaction,
                        cancellationToken: cancellationToken
                    );
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task CacheAllProvincesInRegionWithId(
            IEnumerable<Province> provinces,
            long regionId,
            CancellationToken cancellationToken
        )
        {
            string insertOrIgnore =
            @"
            insert or ignore into Provinces (Url, DisplayName, RegionId)
            values (@Url, @DisplayName, @RegionId)
            ";

            var transaction = db.Connection.BeginTransaction();

            try
            {
                await db
                    .Query("Regions")
                    .Where("Id", "=", regionId)
                    .UpdateAsync(
                        new { AllProvincesAreCached = 1 },
                        transaction,
                        cancellationToken: cancellationToken
                    );

                foreach (Province p in provinces)
                {
                    await db.StatementAsync(
                        insertOrIgnore,
                        new { p.Url, p.DisplayName, RegionId = regionId },
                        transaction,
                        cancellationToken: cancellationToken
                    );
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task CacheAllCitiesInProvinceWithId(
            IEnumerable<City> cities,
            long provinceId,
            CancellationToken cancellationToken
        )
        {
            string insertOrIgnore =
            @"
            insert or ignore into Cities (Url, DisplayName, ProvinceId)
            values (@Url, @DisplayName, @ProvinceId)
            ";

            var transaction = db.Connection.BeginTransaction();

            try
            {
                await db
                    .Query("Provinces")
                    .Where("Id", "=", provinceId).
                    UpdateAsync(
                        new { AllCitiesAreCached = 1 },
                        transaction,
                        cancellationToken: cancellationToken
                    );

                foreach (City c in cities)
                {
                    await db.StatementAsync(
                        insertOrIgnore,
                        new { c.Url, c.DisplayName, ProvinceId = provinceId },
                        transaction,
                        cancellationToken: cancellationToken
                    );
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
