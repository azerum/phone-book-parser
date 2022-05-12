using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Library;

namespace Tests.Helpers
{
    public static class PhoneBookMethods
    {
        public static IEnumerable<MethodToTest> All()
        {
            List<MethodToTest> methods = new();

            CancellationToken none = CancellationToken.None;

            Region region = new("https://example.com", "Dummy");
            Province province = new(region, "https://example.com/p", "Dummy");
            City city = new(province, "https://example.com/p/c", "Dummy");

            SearchCriteria criteria = new("Dummy");

            methods.Add(new(
                phoneBook => phoneBook.GetAllRegions(none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.GetAllProvincesInRegion(region, none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.GetAllCitiesInProvince(province, none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.SearchInAll(criteria, none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.SearchInRegion(region, criteria, none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.SearchInProvince(province, criteria, none)
            ));

            methods.Add(new(
                phoneBook => phoneBook.SearchInCity(city, criteria, none)
            ));

            return methods;
        }

        public static IEnumerable<MethodToTest> AllExceptSearchInCity()
        {
            return All().Where(
                m => m.Name != nameof(IPhoneBook.SearchInCity)
            );
        }
    }
}
