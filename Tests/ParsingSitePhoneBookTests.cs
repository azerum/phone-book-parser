using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Library;
using Library.Parsing;
using NUnit.Framework;

namespace Tests
{
    public class ParsingSitePhoneBookTests
    {
        private readonly ParsingSitePhoneBook phoneBook = new();

        [Test]
        public async Task SearchInCity_HandlesPagination()
        {
			var (city, criteria) = CityAndCriteria();
			var expected = ExpectedRecords(city);

			var actual = await phoneBook.SearchInCity(city, criteria)
				.ToListAsync();

			try
            {
				Assert.That(actual, Is.EquivalentTo(expected));
			}
			catch (Exception e)
            {
				;
				throw;
            } 

            static (City, SearchCriteria) CityAndCriteria()
            {
                Region region = new(
                    "https://spravnik.com/rossiya/arhangyelskaya-oblast",
                    "Архангельская область"
                );

                Province province = new(
                    region,
                    "https://spravnik.com/rossiya/arhangyelskaya-oblast/nyandomskij-rajon",
                    "Няндомский район"
                );

                City city = new(
                    province,
                    "https://spravnik.com/rossiya/arhangyelskaya-oblast/nyandomskij-rajon/nyandoma",
                    "Няндома"
                );

                SearchCriteria criteria = new("Иванов");

                return (city, criteria);
            }

			//Those are manually fetches records that was parsed using
			//'parse-results-page.js' script in browser. Pretty hacky,
			//but we need to test our parser somehow
            static List<FoundRecord> ExpectedRecords(City city)
            {
                List<FoundRecord> records = new();

                records.Add(new(
                    "Иванов",
                    "Виталий Владимирович",
                    "6-14-47",
                    "60 Лет Октября, дом 26, кв. 74",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Виктор Иванович",
                    "6-14-88",
                    "Строителей, дом 20, кв. 60",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Владимир Валерьянович",
                    "6-18-12",
                    "60 Лет Октября, дом 29, кв. 9",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Андрей Викторович",
                    "6-21-85",
                    "Урицкого, дом 12, кв. 11",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Михаил Александрович",
                    "6-28-86",
                    "60 Лет Октября, дом 22, кв. 27",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Виталий Михайлович",
                    "6-30-61",
                    "Ленина, дом 39, кв. 21",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Валерий Валентинович",
                    "6-32-51",
                    "Ленина, дом 48, кв. 34",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Федор Васильевич",
                    "6-37-30",
                    "Парковый, дом 8",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Александр Николаевич",
                    "6-40-01",
                    "Строителей, дом 18, кв. 65",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Александр Николаевич",
                    "6-46-30",
                    "60 Лет Октября, дом 28, кв. 74",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Евгений Валерьянович",
                    "6-47-67",
                    "Фадеева, дом 8, кв. 81",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Александр Николаевич",
                    "6-50-29",
                    "Тульская, дом 40, кв. 4",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Юрий Павлович",
                    "6-52-81",
                    "Павлика Морозова, дом 7, кв. 27",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Николай Григорьевич",
                    "6-55-86",
                    "Клубный, дом 21, кв. 2",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Владимир Викторович",
                    "6-71-37",
                    "Пролетарская, дом 15",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Андрей Евгеньевич",
                    "6-73-08",
                    "Островского, дом 20, кв. 7",
                    city
                ));

                records.Add(new(
                    "Иванов",
                    "Евгений Владимирович",
                    "6-77-98",
                    "Ленина, дом 52, кв. 88",
                    city
                ));

                return records;
			}
        }
    }
}
