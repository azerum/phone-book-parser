using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleApp;
using Library;
using Library.Caching;
using Library.Parsing;
using Spectre.Console;

namespace Experiments
{
    class Program
    {
        delegate IAsyncEnumerable<FoundRecord> SearchFunc(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        private const string moreChoicesText =
            "[grey](Используйте стрелки вверх/вниз для прокрутки списка)[/]";

        private const int pageSize = 10;

        public static async Task Main(string[] args)
        {
            using var phoneBook = CachingSqlitePhoneBook.Open(
                new ParsingSitePhoneBook(),
                "Data Source=cache.db"
            );

            using var source = CancelKeyPressTokenSource();
            var token = source.Token;

            SearchFunc doSearch = await SelectSearchFunc(phoneBook, token);
            SearchCriteria criteria = new("Иванов");

            await foreach (FoundRecord r in doSearch(criteria, token))
            {
                Console.WriteLine(r);
            }
        }

        private static CancellationTokenSource CancelKeyPressTokenSource()
        {
            CancellationTokenSource source = new();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                source.Cancel();

                //Add empty line after "^C" in Unix/Linux
                Console.WriteLine();
            };

            return source;
        }

        private static async Task<SearchFunc> SelectSearchFunc(
            IPhoneBook phoneBook,
            CancellationToken cancellationToken = default
        )
        {
            var regions = phoneBook.GetAllRegions(cancellationToken);
            Region? r = await SelectRegionFrom(regions);

            if (r == null)
            {
                return phoneBook.SearchInAll;
            }

            var provinces = phoneBook
                .GetAllProvincesInRegion(r, cancellationToken);

            Province? p = await SelectProvinceFrom(provinces);

            if (p == null)
            {
                return (criteria, token)
                    => phoneBook.SearchInRegion(r, criteria, token);
            }

            var cities = phoneBook.GetAllCitiesInProvince(p, cancellationToken);
            City? c = await SelectCityFrom(cities);

            if (c == null)
            {
                return (criteria, token)
                    => phoneBook.SearchInProvince(p, criteria, token);
            }

            return (criteria, token) =>
                phoneBook.SearchInCity(c, criteria, token);
        }

        private static async Task<Region?> SelectRegionFrom(
            IAsyncEnumerable<Region> regions
        )
        {
            //Specre.Console does not allow to use nullable types
            //in SelectionPrompt, perhaps because it calls .ToString()
            //on every choice object to print it, and you can't call
            //null.ToString()

            //That's why we introduce NullWrapper type that wraps value or null.
            //It it wraps value, it's ToString() method simply calls inner
            //value's ToString(). If it wraps null, it's ToString() returns
            //the specified text

            var choices = await regions
                .Select(NullWrapper<Region>.Of)
                .ToListAsync();

            choices.Insert(
                0,
                NullWrapper<Region>.Null("(Искать во всех регионах)")
            );

            var wrappedChoice = AnsiConsole.Prompt(
                new SelectionPrompt<NullWrapper<Region>>()
                    .Title("Выберите [green]регион[/], в котором нужно искать")
                    .AddChoices(choices)
                    .PageSize(pageSize)
                    .MoreChoicesText(moreChoicesText)
            );

            return wrappedChoice.Inner;
        }

        private static async Task<Province?> SelectProvinceFrom(
            IAsyncEnumerable<Province> provinces
        )
        {
            var choices = await provinces
                .Select(NullWrapper<Province>.Of)
                .ToListAsync();

            choices.Insert(
                0,
                NullWrapper<Province>.Null("(Искать во всех районах региона)")
            );

            var wrappedChoice = AnsiConsole.Prompt(
                new SelectionPrompt<NullWrapper<Province>>()
                    .Title("Выберите [green]район[/] региона")
                    .AddChoices(choices)
                    .PageSize(pageSize)
                    .MoreChoicesText(moreChoicesText)
            );

            return wrappedChoice.Inner;
        }

        private static async Task<City?> SelectCityFrom(IAsyncEnumerable<City> cities)
        {
            var choices = await cities
                .Select(NullWrapper<City>.Of)
                .ToListAsync();

            choices.Insert(
                0,
                NullWrapper<City>.Null("(Искать во всех городах района)")
            );

            var wrappedChoice = AnsiConsole.Prompt(
                new SelectionPrompt<NullWrapper<City>>()
                    .Title("Выберите [green]город[/] района")
                    .AddChoices(choices)
                    .PageSize(pageSize)
                    .MoreChoicesText(moreChoicesText)
            );

            return wrappedChoice.Inner;
        }
    }
}
