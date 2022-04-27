using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;

namespace Library.Parsing
{
    public class ParsingSitePhoneBook : IPhoneBook
    {
        private const string siteHostName = "spravnik.com";
        private const string regionsPageUrl = "https://spravnik.com/rossiya";

        private const string regionsSelector = ".outSelectTown > div > a";

        private const string provincesSelector =
            ".outSelectTown > div:nth-child(2) > a";

        private const string citiesSelector =
            ".outSelectTown > div:nth-child(3) > a";

        private const string searchFormSelector = "form";

        private const string resultsRowsSelector =
            ".res > table > tbody > tr";

        private readonly IBrowsingContext context;

        public ParsingSitePhoneBook()
        {
            LoaderOptions loaderOptions = new()
            {
                Filter = request
                    => request.Address.HostName == siteHostName
            };

            var config = Configuration.Default
                .WithDefaultLoader(loaderOptions);

            config = config.WithRequester(
                new RateLimitingRequester(TimeSpan.FromMilliseconds(100))
            );

            context = BrowsingContext.New(config);
        }

        public async IAsyncEnumerable<Region> GetAllRegionsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrowAsync(
                regionsPageUrl,
                cancellationToken
            );

            var anchors = document
                .QuerySelectorAll<IHtmlAnchorElement>(regionsSelector);

            foreach (IHtmlAnchorElement a in anchors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string url = a.Href;
                string displayName = GetDisplayNameFrom(a);

                yield return new Region(url, displayName);
            }
        }

        public async IAsyncEnumerable<Province> GetAllProvincesInRegionAsync(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrowAsync(
                region.Url,
                cancellationToken
            );

            var anchors = document
                .QuerySelectorAll<IHtmlAnchorElement>(provincesSelector);

            foreach (IHtmlAnchorElement a in anchors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string url = a.Href;
                string displayName = GetDisplayNameFrom(a);

                yield return new Province(region, url, displayName);
            }
        }

        public async IAsyncEnumerable<City> GetAllCitiesInProvinceAsync(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrowAsync(
                province.Url,
                cancellationToken
            );

            var anchors = document
                .QuerySelectorAll<IHtmlAnchorElement>(citiesSelector);

            foreach (IHtmlAnchorElement a in anchors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string url = a.Href;
                string displayName = GetDisplayNameFrom(a);

                yield return new City(province, url, displayName);
            }
        }

        private static string GetDisplayNameFrom(IHtmlAnchorElement anchor)
        {
            return anchor.QuerySelector("h3")!.TextContent;
        }

        public IAsyncEnumerable<FoundRecord> SearchInAllAsync(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var regions = GetAllRegionsAsync(cancellationToken);

            return regions.SelectAsyncAndMerge(
                r => SearchInRegionAsync(r, criteria, cancellationToken)
            );
        }

        public IAsyncEnumerable<FoundRecord> SearchInRegionAsync(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var provinces =
                GetAllProvincesInRegionAsync(region, cancellationToken);

            return provinces.SelectAsyncAndMerge(
                p => SearchInProvinceAsync(p, criteria, cancellationToken)
            );
        }

        public IAsyncEnumerable<FoundRecord> SearchInProvinceAsync(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var cities =
                GetAllCitiesInProvinceAsync(province, cancellationToken);

            return cities.SelectAsyncAndMerge(
                c => SearchInCityAsync(c, criteria, cancellationToken)
            );
        }

        public async IAsyncEnumerable<FoundRecord> SearchInCityAsync(
            City city,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrowAsync(
                city.Url,
                cancellationToken
            );

            var form = document
                .QuerySelector<IHtmlFormElement>(searchFormSelector);

            if (form == null)
            {
                yield break;
            }

            var resultsPage = await form.SubmitAsync(new
            {
                soname = criteria.Surname,
                io = criteria.Initials
            });

            ThrowIfBadStatusCode(resultsPage);

            var rows = resultsPage
                .QuerySelectorAll<IHtmlTableRowElement>(resultsRowsSelector);

            foreach (IHtmlTableRowElement r in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string phoneNumber = r.QuerySelector("td")!.TextContent;
                string fullName = r.QuerySelector("td:nth-child(2)")!.TextContent;
                string address = r.QuerySelector("td:nth-child(3)")!.TextContent;

                string[] nameParts = fullName.Split(' ');

                string surname = nameParts[0];
                string initials = string.Join(' ', nameParts[1..^0]);

                yield return new FoundRecord(
                    surname,
                    initials,
                    phoneNumber,
                    address,
                    city
                );
            }
        }

        private async Task<IDocument> OpenDocumentOrThrowAsync(
            string url,
            CancellationToken cancellationToken = default
        )
        {
            var document = await context.OpenAsync(url, cancellationToken);
            ThrowIfBadStatusCode(document);

            return document;
        }

        private void ThrowIfBadStatusCode(IDocument document)
        {
            int statusCode = (int)document.StatusCode;

            if (statusCode is >= 400 and < 600)
            {
                throw new ParsingFailedException(statusCode);
            }
        }
    }
}
