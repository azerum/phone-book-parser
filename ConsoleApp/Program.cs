using System.Linq;
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

            Region first;

            await foreach (Region r in phoneBook.GetAllRegions())
            {
                first = r;

                await foreach (Province p in phoneBook.GetAllProvincesInRegion(first))
                {
                    _ = p;
                }

                break;
            }
        }
    }
}
