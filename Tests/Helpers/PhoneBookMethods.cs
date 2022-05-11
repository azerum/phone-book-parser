using System;
using System.Collections.Generic;
using System.Linq;
using Library;

namespace Tests.Helpers
{
    public static class PhoneBookMethods
    {
        public static IEnumerable<Func<IPhoneBook, IAsyncEnumerable<object>>> All()
        {
            List<Func<IPhoneBook, IAsyncEnumerable<object>>> methods = new();

            Region region = new("https://example.com", "Dummy");
            Province province = new(region, "https://example.com/p", "Dummy");
            City city = new(province, "https://example.com/p/c", "Dummy");

            SearchCriteria criteria = new("Dummy");

            methods.Add(phoneBook => phoneBook.GetAllRegions());
            methods.Add(phoneBook => phoneBook.GetAllProvincesInRegion(region));
            methods.Add(phoneBook => phoneBook.GetAllCitiesInProvince(province));

            methods.Add(phoneBook => phoneBook.SearchInAll(criteria));
            methods.Add(phoneBook => phoneBook.SearchInRegion(region, criteria));
            methods.Add(phoneBook => phoneBook.SearchInProvince(province, criteria));
            methods.Add(phoneBook => phoneBook.SearchInCity(city, criteria));

            return methods;
        }

        public static IEnumerable<Func<IPhoneBook, IAsyncEnumerable<object>>>
        AllExceptSearchInCity()
        {
            return All().Where(
                m => m.Method.Name != nameof(IPhoneBook.SearchInCity)
            );
        }
    }
}
