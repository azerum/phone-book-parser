using System.Linq;
using System.Threading.Tasks;
using Library;
using Library.Parsing;

namespace ConsoleApp
{
    class Program
    {
        public static async Task Main()
        {
            Region region = new(
                "http://spravnik.com/rossiya/vladimirskaya-oblast",
                "Region"
            );

            Province province = new(
                region,
                "http://spravnik.com/rossiya/vladimirskaya-oblast/oblastnoj-tsyentr",
                "Province"
            );

            ParsingSitePhoneBook phoneBook = new();
            await phoneBook.GetAllCitiesInProvince(province).FirstAsync();
        }
    }
}
