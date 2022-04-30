using System.Collections.Generic;
using System.Threading.Tasks;
using Library;
using NUnit.Framework;

namespace Tests
{
    public class CitiesTableTests : TestsWithCacheDb
    {
        [Test]
        public async Task SelectAll_Works()
        {
            var expectedCities = Create10CitiesIn10ProvincesIn2Regions();
            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAll();

            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }

        private static List<City> Create10CitiesIn10ProvincesIn2Regions()
        {
            List<City> cities = new();

            Region region1 = new("https://region/1", "Region1");
            Region region2 = new("https://region/2", "Region2");

            for (int i = 0; i < 10; ++i)
            {
                Region r = (i < 5) ? region1 : region2;

                Province p = new(r, $"https://province/{i}", $"Province{i}");
                City c = new(p, $"https://city/{i}", $"City{i}");

                cities.Add(c);
            }

            return cities;
        }

        [Test]
        public async Task SelectAllInRegion_Works()
        {
            var (region, _, expectedCities) = Create10Cities();
            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAllInRegion(region);

            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }

        [Test]
        public async Task SelectAllInProvince_Works()
        {
            var (_, province, expectedCities) = Create10Cities();
            await db.Cities.InsertMany(expectedCities);

            var cities = await db.Cities.SelectAllInProvince(province);

            Assert.That(cities, Is.EquivalentTo(expectedCities));
        }

        private static (Region, Province, IEnumerable<City>) Create10Cities()
        {
            Region region = new("https://region", "Region");
            Province province = new(region, "https://province", "Province");

            List<City> cities = new();

            for (int i = 0; i < 10; ++i)
            {
                City c = new(province, $"https://city/{i}", $"City{i}");
                cities.Add(c);
            }

            return (region, province, cities);
        }
    }
}
