using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Library;

namespace Experiments
{
    class Program
    {
        delegate IAsyncEnumerable<string> SearchFunc(
            SearchCriteria criteria,
            CancellationToken cancellationToken = default
        );

        public static void Main(string[] args)
        {

        }
    }
}
