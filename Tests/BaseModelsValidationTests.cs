using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using NUnit.Framework;

namespace Tests
{
    //NOTE: Very boring tests
    [TestFixture]
    public abstract class BaseModelsValidationTests
    {
        private readonly IPhoneBook phoneBook;
        private static readonly SearchCriteria criteria = new("Иванов");

        public BaseModelsValidationTests(IPhoneBook phoneBook)
        {
            this.phoneBook = phoneBook;
        }

        [Test]
        public void MethodsThatHaveRegionParameter_WhenRegionIsInvalid_Throw(
           [ValueSource("InvalidRegions")] Region region
        )
        {
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                await phoneBook.GetAllProvincesInRegion(region).FirstAsync()
            );

            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                await phoneBook.SearchInRegion(region, criteria).FirstAsync()
            );
        }

        [Test]
        public void MethodsThatHaveRegionParameter_WhenRegionIsValid_DoNotThrow(
            [ValueSource("ValidRegions")] Region region
        )
        {
            Assert.DoesNotThrowAsync(
                async () =>
                await phoneBook.GetAllProvincesInRegion(region).FirstAsync()
            );

            Assert.DoesNotThrowAsync(
                async () =>
                await phoneBook.SearchInRegion(region, criteria).FirstAsync()
            );
        }

        [Test]
        public void MethodsThatHaveProvinceParameter_WhenProvinceIsInvalid_Throw(
            [ValueSource("InvalidProvinces")] Province province
        )
        {
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                await phoneBook.GetAllCitiesInProvince(province).FirstAsync()
            );

            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                await phoneBook.SearchInProvince(province, criteria).FirstAsync()
            );
        }

        [Test]
        public void MethodsThatHaveProvinceParameter_WhenProvinceIsValid_DoNotThrow(
            [ValueSource("ValidProvinces")] Province province
        )
        {
            Assert.DoesNotThrowAsync(
                async () =>
                await phoneBook.GetAllCitiesInProvince(province).FirstAsync()
            );

            Assert.DoesNotThrowAsync(
                async () =>
                await phoneBook.SearchInProvince(province, criteria).FirstAsync()
            );
        }

        [Test]
        public void MethodsThatHaveCityParameter_WhenCityIsInvalid_Throw(
            [ValueSource("InvalidCities")] City city
        )
        {
            Assert.ThrowsAsync<ArgumentException>(
                async () =>
                await phoneBook.SearchInCity(city, criteria).FirstAsync()
            );
        }

        [Test]
        public void MethodsThatHaveCityParameter_WhenCityIsValid_DoNotThrow(
            [ValueSource("ValidCities")] City city
        )
        {
            Assert.DoesNotThrowAsync(
                async () =>
                await phoneBook.SearchInCity(city, criteria).FirstAsync()
            );
        }

        private const string wrongSiteUrl = "https://google.com";

        private const string rootUrl = "https://spravnik.com";
        private const string regionsPageUrl = "https://spravnik.com/rossiya";

        //IMPORTANT: Those string are substring of each other.
        //That is, goodRegionUrlHttps is a substring of goodProvinceUrlHttps,
        //and goodProvinceUrlHttps is a substring of goodCityUrlHttps
        //(and the same is true for -Http strings)
        //This is crucial for correct tests

        private const string goodRegionUrlHttps =
            "https://spravnik.com/rossiya/vladimirskaya-oblast";

        private const string goodRegionUrlHttp =
            "http://spravnik.com/rossiya/vladimirskaya-oblast";

        private const string goodProvinceUrlHttps =
            "https://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr";

        private const string goodProvinceUrlHttp =
            "http://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr";

        private const string goodCityUrlHttps =
            "https://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr/vladimir";

        private const string goodCityUrlHttp =
            "http://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr/vladimir";

        public static List<Region> InvalidRegions()
        {
            List<Region> regions = new();

            regions.Add(new(wrongSiteUrl, "URL from the wrong site"));

            regions.Add(new("", "With empty URL"));
            regions.Add(new(rootUrl, "With root URL"));
            regions.Add(new(regionsPageUrl, "With Regions page URL"));

            regions.Add(new($"{regionsPageUrl}/", "With empty name in URL"));

            regions.Add(new(goodProvinceUrlHttps, "With Province URL"));
            regions.Add(new(goodCityUrlHttps, "With City URL"));

            return regions;
        }

        public static List<Region> ValidRegions()
        {
            List<Region> regions = new();

            regions.Add(new(goodRegionUrlHttps, "HTTPS"));
            regions.Add(new(goodRegionUrlHttp, "HTTP"));

            return regions;
        }

        public static List<Province> InvalidProvinces()
        {
            List<Province> provinces = new();

            Region badRegion = new(wrongSiteUrl, "Bad Region");
            Region goodRegion = new(goodRegionUrlHttps, "Good Region HTTPS");

            provinces.Add(new(badRegion, goodProvinceUrlHttps, "With bad Region"));

            provinces.Add(new(
                goodRegion,
                wrongSiteUrl,
                "With URL from the wrong site"
            ));

            provinces.Add(new(goodRegion, "", "With empty URL"));
            provinces.Add(new(goodRegion, rootUrl, "With root URL"));

            provinces.Add(new(
                goodRegion,
                regionsPageUrl,
                "With regions page URL"
            ));

            provinces.Add(new(
                goodRegion,
                $"{goodRegionUrlHttps}/",
                "With empty name in URL"
            ));

            provinces.Add(new(
                goodRegion,
                goodRegionUrlHttps,
                "With Region URL"
            ));

            provinces.Add(new(
                goodRegion,
                goodCityUrlHttps,
                "With City URL"
            ));

            Region region = new(
                "https://spravnik.com/rossiya/volgogradskaya-oblast",
                "Volgogradskaya Oblast"
            );

            provinces.Add(new(
                region,
                "https://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr",
                "In Vladimirskaya Oblast"
            ));

            return provinces;
        }

        public static List<Province> ValidProvinces()
        {
            List<Province> provinces = new();

            Region regionHttps = new(goodRegionUrlHttps, "Good Region HTTPS");
            provinces.Add(new(regionHttps, goodProvinceUrlHttps, "HTTPS"));

            Region regionHttp = new(goodRegionUrlHttp, "Good Region HTTP");
            provinces.Add(new(regionHttp, goodProvinceUrlHttp, "HTTP"));

            return provinces;
        }

        public static List<City> InvalidCities()
        {
            List<City> cities = new();

            Region goodRegion = new(goodRegionUrlHttps, "Good Region HTTPS");

            Province badProvince = new(goodRegion, wrongSiteUrl, "Bad Province");

            Province goodProvince = new(
                goodRegion,
                goodProvinceUrlHttps,
                "Good Province HTTTPS"
            );

            cities.Add(new(badProvince, goodCityUrlHttps, "With bad Province"));

            cities.Add(new(
                goodProvince,
                wrongSiteUrl,
                "With URL from the wrong site"
            ));

            cities.Add(new(goodProvince, "", "With empty URL"));
            cities.Add(new(goodProvince, rootUrl, "With root URL"));
            cities.Add(new(goodProvince, regionsPageUrl, "With regions page URL"));

            cities.Add(new(
                goodProvince,
                $"{goodProvinceUrlHttps}/",
                "With empty name in URL"
            ));

            cities.Add(new(goodProvince, goodRegionUrlHttps, "With Region URL"));

            cities.Add(new(
                goodProvince,
                goodProvinceUrlHttps,
                "With Province URL"
            ));

            Province province = new(
                goodRegion,
                "https://spravnik.com/rossiya/vladimirskaya-oblast/vyaznikovskij-rajon",
                "Vyaznikovskij Rajon"
            );

            cities.Add(new(
                province,
                "https://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr/vladimir",
                "In Vladimirskaya Oblast"
            ));

            return cities;
        }

        public static List<City> ValidCities()
        {
            List<City> cities = new();

            Region regionHttps = new(goodRegionUrlHttps, "Good Region HTTPS");

            Province provinceHttps = new(
                regionHttps,
                goodProvinceUrlHttps,
                "Good Province HTTPS"
            );

            cities.Add(new(
                provinceHttps,
                goodCityUrlHttps,
                "HTTPS"
            ));

            Region regionHttp = new(goodRegionUrlHttp, "Good Region HTTP");

            Province provinceHttp = new(
                regionHttp,
                goodProvinceUrlHttp,
                "Good Province HTTP"
            );

            cities.Add(new(
                provinceHttp,
                goodCityUrlHttp,
                "HTTP"
            ));

            return cities;
        }
    }
}
