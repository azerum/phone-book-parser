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

        //'serchForm' typo is intended as it is used on the site
        private const string searchFormSelector = "serchForm form"; 

        private const string resultsRowsSelector =
            ".res > table > tbody > tr";

        private readonly IBrowsingContext context;

        public ParsingSitePhoneBook()
            : this(new RateLimitingRequester(TimeSpan.FromMilliseconds(150)))
        {

        }

        public ParsingSitePhoneBook(IRequester requester)
        {
            LoaderOptions loaderOptions = new()
            {
                Filter = request
                    => request.Address.HostName == siteHostName
            };

            var config = Configuration.Default
                .WithDefaultLoader(loaderOptions);

            config = config.WithRequester(requester);

            context = BrowsingContext.New(config);
        }

        public async IAsyncEnumerable<Region> GetAllRegions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrow(
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

        public async IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrow(
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

        public async IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrow(
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

        public IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var regions = GetAllRegions(cancellationToken);

            return regions.SelectAsyncAndMerge(
                r => SearchInRegion(r, criteria, cancellationToken)
            );
        }

        public IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var provinces = GetAllProvincesInRegion(region, cancellationToken);

            return provinces.SelectAsyncAndMerge(
                p => SearchInProvince(p, criteria, cancellationToken)
            );
        }

        public IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        )
        {
            var cities = GetAllCitiesInProvince(province, cancellationToken);

            return cities.SelectAsyncAndMerge(
                c => SearchInCity(c, criteria, cancellationToken)
            );
        }

        public async IAsyncEnumerable<FoundRecord> SearchInCity(
            City city,
            SearchCriteria criteria,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            var document = await OpenDocumentOrThrow(
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

                string[] nameParts = fullName.Split(' ', 2);

                string surname = nameParts[0];
                string initials = nameParts[1];

                yield return new FoundRecord(
                    surname,
                    initials,
                    phoneNumber,
                    address,
                    city
                );
            }
        }

        private async Task<IDocument> OpenDocumentOrThrow(
            string url,
            CancellationToken cancellationToken = default
        )
        {
            var document = await context.OpenAsync(url, cancellationToken);
            ThrowIfBadStatusCode(document);

            return document;
        }

        private static void ThrowIfBadStatusCode(IDocument document)
        {
            int statusCode = (int)document.StatusCode;

            if (statusCode is >= 400 and < 600)
            {
                throw new ParsingFailedException(statusCode);
            }
        }
    }
}
