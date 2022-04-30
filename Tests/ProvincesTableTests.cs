using System.Collections.Generic;
using System.Threading.Tasks;
using Library;
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
            Region region = new("https://example.com", "Example");

            List<Province> expectedProvinces = new(2);

            for (int i = 0; i < expectedProvinces.Capacity; ++i)
            {
                Province p = new(region, $"https://example.com/{i}", $"Province{i}");
                expectedProvinces.Add(p);
            }

            await db.Provinces.InsertMany(expectedProvinces);

            var provinces = await db.Provinces.SelectAllInRegion(region);

            Assert.That(provinces, Is.EquivalentTo(expectedProvinces));
        }
    }
}
