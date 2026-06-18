using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Creates open in-memory SQLite connections and matching <see cref="DbContextOptions{TContext}"/>,
    /// calling <c>EnsureCreated()</c> so the schema exists for the lifetime of the connection.
    /// Each connection must be kept open for the duration of the test (an in-memory SQLite database
    /// is discarded when its last connection closes).
    /// </summary>
    internal static class SqliteTestDbContextFactory
    {
        private const string InMemoryConnectionString = "DataSource=:memory:";

        public static SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection(InMemoryConnectionString);
            connection.Open();

            return connection;
        }

        public static DbContextOptions<TContext> CreateDbContextOptions<TContext>(SqliteConnection connection)
            where TContext : DbContext
        {
            var options = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(connection)
                .Options;

            using var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
            context.Database.EnsureCreated();

            return options;
        }
    }
}
