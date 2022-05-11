using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using Library;

namespace Experiments
{
    class Program
    {
        delegate IAsyncEnumerable<string> SearchFunc(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        public static async Task Main(string[] args)
        {

        }
    }
}
