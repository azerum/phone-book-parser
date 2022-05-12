using System;
using System.Collections.Generic;
using System.Threading;
using Library;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CachingPhoneBook
{
    public class DisposeTests : TestsBase
    {
        sealed class FakePhoneBook : IPhoneBook, IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }

            public IAsyncEnumerable<Region> GetAllRegions(
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<Province> GetAllProvincesInRegion(
                Region region,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<City> GetAllCitiesInProvince(
                Province province,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<FoundRecord> SearchInAll(
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<FoundRecord> SearchInRegion(
                Region region,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<FoundRecord> SearchInProvince(
                Province province,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<FoundRecord> SearchInCity(
                City city,
                SearchCriteria criteria,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotImplementedException();
            }
        }

        private FakePhoneBook fakePhoneBook;

        protected override IPhoneBook GetInnerForNextTest()
        {
            fakePhoneBook = new FakePhoneBook();
            return fakePhoneBook;
        }

        [Test]
        public void AllMethodsThrowIfDisposed(
            [ValueSource(typeof(PhoneBookMethods), "All")]
            MethodToTest method
        )
        {
            cachingBook.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(
                () => method.Call(cachingBook).Consume()
            );
        }

        [Test]
        public void Dispose_IfInnerIsDisposable_DisposesIt()
        {
            cachingBook.Dispose();
            Assert.IsTrue(fakePhoneBook.Disposed);
        }
    }
}
