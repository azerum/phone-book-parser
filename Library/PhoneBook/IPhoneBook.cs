using System.Collections.Generic;
using System.Threading;

namespace Library
{
    /// <remarks>
    /// <para>
    /// All implementation of this interface are NOT thread-safe, meaning that:
    /// </para>
    /// <para>
    /// - You CANNOT consume multiple IAsyncEnumerables that were returned
    /// from methods of this interface in parallel
    /// </para>
    /// <para>
    /// - But you CAN consume multiple IAsyncEnumerables that are inflight if
    /// you consume one enumerable at a time. That is, the following code
    /// is correct:
    /// </para>
    /// <code>
    /// IPhoneBook phoneBook = new SomePhoneBook();
    ///
    /// var regions = phoneBook.GetAllRegionsAsync().GetAsyncEnumerator();
    /// 
    /// var cities = phoneBook
    ///     .GetAllCitiesInProvince(someProvince)
    ///     .GetAsyncEnumerator();
    ///
    /// while (true)
    /// {
    ///     bool thereIsARegion = await regions.MoveNextAsync();
    ///     bool thereIsACity = await cities.MoveNextAsync();
    ///
    ///     if (!thereIsARegion || !thereIsACity) break;
    ///
    ///     Console.WriteLine(regions.Current);
    ///     Console.WriteLine(cities.Current);
    /// }
    /// </code>
    /// <para>
    /// The code is correct as Task from <c>cities.MoveNextAsync()</c> is
    /// started only AFTER Task from <c>regions.MoveNextAsync()</c> is
    /// completed.
    /// 
    /// Compare to the following were the tasks are run
    /// concurrently (this code has undefined behaviour):
    /// </para>
    /// <code>
    /// IPhoneBook phoneBook = new SomePhoneBook();
    ///
    /// var regions = phoneBook.GetAllRegionsAsync().GetAsyncEnumerator();
    /// 
    /// var cities = phoneBook
    ///     .GetAllCitiesInProvince(someProvince)
    ///     .GetAsyncEnumerator();
    ///
    /// while (true)
    /// {
    ///     bool[] bools = await Task.WhenAll(
    ///         regions.MoveNextAsync().AsTask(),
    ///         cities.MoveNextAsync().AsTask()
    ///     );
    ///
    ///     if (bools.Any(b => !b)) break; 
    ///
    ///     Console.WriteLine(regions.Current);
    ///     Console.WriteLine(cities.Current);
    /// }
    /// </code>
    /// </remarks>
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
