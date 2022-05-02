using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
    [TestFixture]
    public class ProvincesTableTests : TestsWithCacheDb
    {
        [Test]
        public async Task SelectAllInRegion_Works()
        {
            var (region, expectedProvinces, _) =
                CreateRegionWithProvincesAndCities("Region", 2, 0);

            await db.Provinces.InsertMany(expectedProvinces);

            var provinces = await db.Provinces.SelectAllInRegion(region);

            Assert.That(provinces, Is.EquivalentTo(expectedProvinces));
        }
    }
}
