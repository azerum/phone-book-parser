using System;
using System.Linq;
using System.Threading.Tasks;
using Library;
using Library.Caching;
using Microsoft.Data.Sqlite;

namespace ConsoleApp
{
    class Program
    {
        public static async Task Main()
        {
            using SqliteConnection connection = new("Data Source=cache.db");
            connection.Open();

            CacheDb db = new(connection);

            Region region = new("")
        }
    }
}
