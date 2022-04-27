using System;
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
            IPhoneBook inner = new ParsingSitePhoneBook();

            IPhoneBook phoneBook = CachingSqlitePhoneBook.Create(
                inner,
                "Data Source=cache.db"
            );

            await foreach (Region r in phoneBook.GetAllRegionsAsync())
            {
                Console.WriteLine(r);
            }
        }
    }
}
