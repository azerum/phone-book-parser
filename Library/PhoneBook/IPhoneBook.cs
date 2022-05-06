using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Library
{
    /// <remarks>
    /// All implementations of this interface are not-thread safe
    /// (unless those implementations specifically mension that they are).
    /// 
    /// This means that users of the interface should NOT start a <see cref="Task"/>
    /// by calling the methods of this interface while there is some other
    /// inflight <see cref="Task"/> that is not yet awaited.
    ///
    /// As all methods of this interface return <see cref=IAsyncEnumerator{T}"/>,
    /// this means that the users should NOT call MoveNextAsync() method
    /// on an enumerator returned from the interface methods while there is
    /// another such infligh MoveNextAsync() that needs to be awaited.
    ///
    /// Most of the time, you will consume <see cref="IAsyncEnumerator{T}"/>
    /// using <c>await foreach</c>, which does not violate this rule and
    /// hence you shouldn't worry about it
    /// </remarks>
    public interface IPhoneBook
    {
        IAsyncEnumerable<Region> GetAllRegions(
            CancellationToken cancellationToken = default
        );

        /// <exception cref="ArgumentException">
        /// If <paramref name="region"/> is not a valid Region
        /// </exception>
        IAsyncEnumerable<Province> GetAllProvincesInRegion(
            Region region,
            CancellationToken cancellationToken = default
        );

        /// <exception cref="ArgumentException">
        /// If <paramref name="province"/> is not a valid Province
        /// </exception>
        IAsyncEnumerable<City> GetAllCitiesInProvince(
            Province province,
            CancellationToken cancellationToken = default
        );

        IAsyncEnumerable<FoundRecord> SearchInAll(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        /// <exception cref="ArgumentException">
        /// If <paramref name="region"/> is not a valid Region
        /// </exception>
        IAsyncEnumerable<FoundRecord> SearchInRegion(
            Region region,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        /// <exception cref="ArgumentException">
        /// If <paramref name="province"/> is not a valid Province
        /// </exception>
        IAsyncEnumerable<FoundRecord> SearchInProvince(
            Province province,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        /// <exception cref="ArgumentException">
        /// If <paramref name="city"/> is not a valid City
        /// </exception>
        IAsyncEnumerable<FoundRecord> SearchInCity(
            City city,
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );
    }
}
