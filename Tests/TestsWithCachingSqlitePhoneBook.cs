using System.Linq;
using System.Threading.Tasks;
using Library;
using Library.Caching;
using Library.Parsing;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public abstract class TestsWithCachingSqlitePhoneBook
    {
        protected const string dbPath = "cache.db";

        protected static readonly string connectionString =
            $"Data Source={dbPath}";

        protected readonly ParsingSitePhoneBook parsingBook = new();
        protected CachingSqlitePhoneBook cachingBook;

        [OneTimeSetUp]
        public void CreateCachingPhoneBook()
        {
            FileExtensions.Truncate(dbPath);

            cachingBook = CachingSqlitePhoneBook.Open(
                parsingBook,
                connectionString
            );
        }

        public ValueTask<Region> GetFirstRegionAsync()
        {
            return parsingBook.GetAllRegions().FirstAsync();
        }

        public async ValueTask<Province> GetFirstProvinceAsync()
        {
            Region region = await GetFirstRegionAsync();

            return await parsingBook
                .GetAllProvincesInRegion(region)
                .FirstAsync();
        }

        public async ValueTask<City> GetFirstCityAsync()
        {
            Province province = await GetFirstProvinceAsync();

            return await parsingBook
                .GetAllCitiesInProvince(province)
                .FirstAsync();
        }
    }
}
