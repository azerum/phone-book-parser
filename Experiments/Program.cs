using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Io;
using Library;
using Library.Caching;
using Library.Parsing;

namespace Experiments
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            if (
                args.Length != 3
                || !int.TryParse(args[0], out int maxConcurrency)
                || !int.TryParse(args[1], out int msBetweenGroups)
                || !int.TryParse(args[2], out int maxRequestsCount)
            )
            {
                Console.WriteLine("Usage: <max-concurrency> <ms-between-groups> <max-requests-count>");
                return;
            }

            TimeSpan intervalBetweenGroups =
                TimeSpan.FromMilliseconds(msBetweenGroups);

            using var source = CancelKeyPressTokenSource();
            var token = source.Token;

            List<string> urls = await GetUrlsAsync(maxConcurrency, token);

            CountingRequester requester = new();
            var context = MakeBrowsingContext(requester);

            double totalSeconds = 0.0;
            Stopwatch stopwatch = new();

            try
            {
                while (true)
                {
                    var tasks = urls
                        .Select(u => context.OpenAsync(u, token))
                        .ToList();

                    while (!token.IsCancellationRequested && tasks.Count > 0)
                    {
                        stopwatch.Restart();
                        var completed = await Task.WhenAny(tasks);
                        stopwatch.Stop();

                        tasks.Remove(completed);

                        var document = await completed;
                        int statusCode = (int)document.StatusCode;

                        double deltaT = stopwatch.Elapsed.TotalSeconds;
                        totalSeconds += deltaT;

                        Console.WriteLine(
                            $"{requester.RequestsCount / totalSeconds:F3} requests/s. {deltaT:F3}s"
                        );

                        if (statusCode is >= 400 and < 600)
                        {
                            Console.WriteLine($"Error: status code {statusCode}");
                            return;
                        }
                    }

                    await Task.Delay(intervalBetweenGroups, token);
                }
            }
            catch (Exception e) when (IsCancelledException(e))
            {
                ;
            }
        }

        private static CancellationTokenSource CancelKeyPressTokenSource()
        {
            CancellationTokenSource source = new();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine();

                source.Cancel();
            };

            return source;
        }

        private static async Task<List<string>> GetUrlsAsync(
            int count,
            CancellationToken cancellationToken
        )
        {
            IPhoneBook phoneBook = CachingSqlitePhoneBook.Open(
                new ParsingSitePhoneBook(),
                "Data Source=cache.db"
            );

            return await phoneBook
                .GetAllRegions(cancellationToken)
                .Take(count)
                .Select(r => r.Url)
                .ToListAsync(CancellationToken.None);
        }

        private static IBrowsingContext MakeBrowsingContext(
            IRequester requester
        )
        {
            LoaderOptions loaderOptions = new()
            {
                Filter = r => r.Address.HostName == "spravnik.com"
            };

            var config = Configuration.Default
                .WithDefaultLoader(loaderOptions)
                .WithRequester(requester);

            return BrowsingContext.New(config);
        }

        private static bool IsCancelledException(Exception e)
        {
            if (e is AggregateException aggregate)
            {
                return aggregate.InnerExceptions.All(IsCancelledException);
            }

            return e is OperationCanceledException or TaskCanceledException;
        }
    }
}
