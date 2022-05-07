using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ConsoleApp
{
    class Program
    {
        public static async Task Main()
        {
            await using SqliteConnection connection =
                new("Data Source=cache.db");

            connection.Open();

            SqlBuilder builder = new();

            var selectProvincesTemplate = builder.AddTemplate(@"
                SELECT
                    p.Id,
                    p.Url as PUrl,
                    p.DisplayName as PDisplayName,
                    r.Url as RUrl,
                    r.DisplayName as RDisplayName
                FROM
                    Provinces as p
                INNER JOIN
                    Regions as r
                ON
                    p.RegionId = r.Id
                /**leftjoin**/
                /**where**/
            ");

            builder.LeftJoin("Cities as c ON c.ProvinceId = p.Id");
            builder.Where("c.Id IS NULL");

            Console.WriteLine(selectProvincesTemplate.RawSql);
            Console.WriteLine(selectProvincesTemplate.Parameters);
        }
    }
}
