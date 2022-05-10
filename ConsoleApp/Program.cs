using System.Threading.Tasks;
using Library;
using Library.Caching;
using Library.Parsing;

namespace ConsoleApp
{
    class Program
    {
        public static async Task Main()
        {
            var phoneBook = CachingSqlitePhoneBook.Open(
                new ParsingSitePhoneBook(),
                "Data Source=cache.db"
            );

            await foreach (Region r in phoneBook.GetAllRegions())
            {
                await foreach (Province p in phoneBook.GetAllProvincesInRegion(r))
                {
                    _ = p;
                }

                await foreach (Province p in phoneBook.GetAllProvincesInRegion(r))
                {
                    _ = p;
                }

                break;
            }
        }
    }
}
