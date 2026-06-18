using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CartModule.Data.Model;
using VirtoCommerce.CartModule.Data.Repositories;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// SQLite-compatible implementation of <see cref="ICartRawDatabaseCommand"/> for component tests.
    /// Ported from the LEO harness — the production command uses SQL Server / PostgreSQL syntax that
    /// SQLite cannot run, so this translates the few queries the cart flow actually issues.
    /// </summary>
    internal class SqliteCartRawDatabaseCommand : ICartRawDatabaseCommand
    {
        public Task SoftRemove(CartDbContext dbContext, IList<string> ids)
        {
            return ExecuteStoreQueryAsync(dbContext, "UPDATE Cart SET IsDeleted = 1 WHERE Id IN ({0})", ids);
        }

        public async Task<IList<ProductWishlistEntity>> FindWishlistsByProductsAsync(
            CartDbContext dbContext,
            string customerId,
            string organizationId,
            string storeId,
            IList<string> productIds)
        {
            var command = new Command();
            var commandTemplate = new StringBuilder();

            commandTemplate.Append(@"
                  SELECT c.Id, li.ProductId
                  FROM Cart c
                  LEFT JOIN CartLineItem li
                  ON c.Id = li.ShoppingCartId
                  WHERE c.IsDeleted = 0 AND c.Type = 'Wishlist'
                  AND li.IsGift = 0
                  AND li.ProductId IN (@productIds)");

            if (!string.IsNullOrEmpty(organizationId) && !string.IsNullOrEmpty(customerId))
            {
                command.Parameters.Add(new SqliteParameter("@customerId", customerId));
                command.Parameters.Add(new SqliteParameter("@organizationId", organizationId));

                commandTemplate.Append(@"
                    AND (c.CustomerId = @customerId AND c.OrganizationId IS NULL OR c.OrganizationId = @organizationId)
                ");
            }
            else if (!string.IsNullOrEmpty(customerId))
            {
                command.Parameters.Add(new SqliteParameter("@customerId", customerId));

                commandTemplate.Append(@"
                    AND c.CustomerId = @customerId AND c.OrganizationId IS NULL
                ");
            }

            command.Text = commandTemplate.ToString();
            AddArrayParameters(command, "@productIds", productIds);

            return await dbContext.Set<ProductWishlistEntity>().FromSqlRaw(command.Text, command.Parameters.ToArray()).ToListAsync();
        }

        private static Task<int> ExecuteStoreQueryAsync(CartDbContext dbContext, string commandTemplate, IEnumerable<string> parameterValues)
        {
            var command = CreateCommand(commandTemplate, parameterValues);

            return dbContext.Database.ExecuteSqlRawAsync(command.Text, command.Parameters.ToArray());
        }

        private static Command CreateCommand(string commandTemplate, IEnumerable<string> parameterValues)
        {
            var parameters = parameterValues.Select((v, i) => new SqliteParameter($"@p{i}", v)).ToArray();
            var parameterNames = string.Join(",", parameters.Select(p => p.ParameterName));

            return new Command
            {
                Text = string.Format(commandTemplate, parameterNames),
                Parameters = parameters.OfType<object>().ToList(),
            };
        }

        private static SqliteParameter[] AddArrayParameters<T>(Command cmd, string paramNameRoot, IEnumerable<T> values)
        {
            var parameters = new List<SqliteParameter>();
            var parameterNames = new List<string>();
            var paramNbr = 1;
            foreach (var value in values)
            {
                var paramName = $"{paramNameRoot}{paramNbr++}";
                parameterNames.Add(paramName);
                var p = new SqliteParameter(paramName, value);
                cmd.Parameters.Add(p);
                parameters.Add(p);
            }

            cmd.Text = cmd.Text.Replace(paramNameRoot, string.Join(",", parameterNames));

            return parameters.ToArray();
        }

        private class Command
        {
            public string Text { get; set; } = string.Empty;

            public IList<object> Parameters { get; set; } = [];
        }
    }
}
