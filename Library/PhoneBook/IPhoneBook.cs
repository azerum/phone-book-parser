using System.Collections.Generic;
using System.Threading;

namespace Library
{
    public interface IPhoneBook
    {
        IAsyncEnumerable<Region> GetAllRegions(
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<FoundRecord> SearchInCity(
            City city,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );
    }
}
