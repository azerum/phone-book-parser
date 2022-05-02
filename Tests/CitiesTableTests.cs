using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
    public class CitiesTableTests : TestsWithCacheDb
    {
        [Test]
        public async Task SelectAll_Works()
        {
            var (_, _, expectedCities) =
                CreateRegionWithProvincesAndCities("Region", 10, 10);

            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAll();
            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }

        [Test]
        public async Task SelectAllInRegion_Works()
        {
            var (region, _, expectedCities) =
                CreateRegionWithProvincesAndCities("Region", 1, 10);

            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAllInRegion(region);

            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }

        [Test]
        public async Task SelectAllInProvince_Works()
        {
            var (_, provinces, expectedCities) =
                CreateRegionWithProvincesAndCities("Region", 1, 10);

            var province = provinces.First();

            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAllInProvince(province);

            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }
    }
}
