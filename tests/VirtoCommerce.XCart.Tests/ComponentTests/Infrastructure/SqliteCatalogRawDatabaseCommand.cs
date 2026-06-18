using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Data.Model;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// SQLite-compatible implementation of <see cref="ICatalogRawDatabaseCommand"/> for component tests.
    /// Ported from the LEO harness. Most methods are unused by the cart-product loading path and throw
    /// <see cref="NotImplementedException"/>; only the category-hierarchy walk and item removal — which
    /// the production code expresses as a recursive CTE that SQLite cannot run — are implemented with an
    /// EF-backed walk sufficient for the small catalogs used in tests.
    /// </summary>
    internal class SqliteCatalogRawDatabaseCommand : ICatalogRawDatabaseCommand
    {
        public Task<IList<string>> GetAllSeoDuplicatesIdsAsync(CatalogDbContext dbContext)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public Task<IList<string>> GetAllChildrenCategoriesIdsAsync(CatalogDbContext dbContext, IList<string> categoryIds)
        {
            throw new NotImplementedException();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public async Task<IList<CategoryHierarchyItem>> GetChildCategoriesAsync(CatalogDbContext dbContext, IList<string> categoryIds)
        {
            // Iterative walk of the category tree — the production path uses a recursive CTE which SQLite cannot
            // run via the platform's raw SQL. Tests exercise small catalogs, so an EF-backed walk is sufficient.
            if (categoryIds == null || categoryIds.Count == 0)
            {
                return [];
            }

            var result = new List<CategoryHierarchyItem>();
            var frontier = categoryIds.Distinct().ToList();
            var visited = new HashSet<string>(frontier);
            var depth = 0;

            while (frontier.Count > 0)
            {
                var children = await dbContext.Set<CategoryEntity>()
                    .Where(x => x.ParentCategoryId != null && frontier.Contains(x.ParentCategoryId))
                    .Select(x => new { x.Id, x.ParentCategoryId })
                    .ToListAsync();

                if (children.Count == 0)
                {
                    break;
                }

                depth++;

                var nextFrontier = new List<string>();

                foreach (var child in children)
                {
                    if (visited.Add(child.Id))
                    {
                        result.Add(new CategoryHierarchyItem { Id = child.Id, ParentCategoryId = child.ParentCategoryId, Depth = depth });
                        nextFrontier.Add(child.Id);
                    }
                }

                frontier = nextFrontier;
            }

            return result;
        }

        public Task<GenericSearchResult<AssociationEntity>> SearchAssociations(CatalogDbContext dbContext, ProductAssociationSearchCriteria criteria)
        {
            throw new NotImplementedException();
        }

        public Task<IList<CategoryEntity>> SearchCategoriesHierarchyAsync(CatalogDbContext dbContext, string categoryId)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveItemsAsync(CatalogDbContext dbContext, IList<string> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
            {
                return;
            }

            // For SQLite, use EF to remove items and rely on cascade delete for related entities.
            var items = await dbContext.Set<ItemEntity>()
                .Where(x => itemIds.Contains(x.Id) || (x.ParentId != null && itemIds.Contains(x.ParentId)))
                .ToListAsync();

            dbContext.Set<ItemEntity>().RemoveRange(items);
            await dbContext.SaveChangesAsync();
        }

        public Task RemoveCategoriesAsync(CatalogDbContext dbContext, IList<string> ids)
        {
            throw new NotImplementedException();
        }

        public Task RemoveCatalogsAsync(CatalogDbContext dbContext, IList<string> ids)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAllPropertyValuesAsync(CatalogDbContext dbContext, PropertyEntity catalogProperty, PropertyEntity categoryProperty, PropertyEntity itemProperty)
        {
            throw new NotImplementedException();
        }
    }
}
