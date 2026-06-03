using System;
using System.Collections.Generic;
using FluentAssertions;
using GraphQL.Resolvers;
using GraphQL.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Schemas;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Schemas
{
    // ParseDictionary resolves the actual type via the global, static AbstractTypeFactory<NewCartItem>.
    // Only the override-test mutates it, so that test removes its registration in a finally block. The
    // other tests never touch the factory, so they exercise the real production path (no module registers
    // NewCartItem, so resolution falls back to typeof(NewCartItem)).
    public class InputNewCartItemTypeTests
    {
        [Fact]
        public void ParseDictionary_WithOverride_YieldsOverrideType()
        {
            AbstractTypeFactory<NewCartItem>.OverrideType<NewCartItem, TestNewCartItem>();
            object parsed;
            try
            {
                var type = BuildInitializedType();

                parsed = type.ParseDictionary(new Dictionary<string, object>
                {
                    ["productId"] = "p1",
                    ["quantity"] = 2,
                });
            }
            finally
            {
                AbstractTypeFactory<NewCartItem>.RemoveType<TestNewCartItem>();
            }

            parsed.Should().BeOfType<TestNewCartItem>();
            ((NewCartItem)parsed).ProductId.Should().Be("p1");
            ((NewCartItem)parsed).Quantity.Should().Be(2);
        }

        [Fact]
        public void ParseDictionary_WithoutOverride_YieldsBaseType()
        {
            // Empty factory == production state (the upstream module registers no NewCartItem type).
            // Proves GraphQL.NET picks the parameterless ctor for the two-ctor NewCartItem without any
            // [GraphQLConstructor] annotation (same binding as the existing InputNewWishlistItemType).
            var type = BuildInitializedType();

            var parsed = type.ParseDictionary(new Dictionary<string, object>
            {
                ["productId"] = "p1",
                ["quantity"] = 2,
            });

            parsed.Should().BeOfType<NewCartItem>();
            ((NewCartItem)parsed).ProductId.Should().Be("p1");
            ((NewCartItem)parsed).Quantity.Should().Be(2);
        }

        [Fact]
        public void ParseDictionary_RoundTrips_ScalarFields()
        {
            // Scalar-field parity guard for the InputObjectGraphType -> ExtendableInputObjectGraphType
            // re-base. The re-base swaps the base class uniformly across all fields via one shared
            // ParseDictionary path, so scalar parity de-risks it. Nested-list fidelity (dynamicProperties,
            // configurationSections) is an orthogonal GraphQL.NET concern unaffected by the re-base and is
            // covered end-to-end by the LEO-side override test and the integration test.
            var type = BuildInitializedType();

            var createdDate = new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc);
            var parsed = (NewCartItem)type.ParseDictionary(new Dictionary<string, object>
            {
                ["productId"] = "p1",
                ["quantity"] = 5,
                ["price"] = 12.34m,
                ["comment"] = "hello",
                ["createdDate"] = createdDate,
            });

            parsed.ProductId.Should().Be("p1");
            parsed.Quantity.Should().Be(5);
            parsed.Price.Should().Be(12.34m);
            parsed.Comment.Should().Be("hello");
            parsed.CreatedDate.Should().Be(createdDate);
        }

        // CompileToObject (used by ParseDictionary) requires every field's ResolvedType to be wired,
        // which only happens during a full schema initialization. Reference the input type as a query
        // argument and initialize a minimal schema so the field graph is resolved, then return the
        // initialized instance. Any factory override must be registered BEFORE this runs, because
        // ExtendableInputObjectGraphType.Initialize resolves the actual type via the factory.
        private static InputNewCartItemType BuildInitializedType()
        {
            var type = new InputNewCartItemType();

            var query = new ObjectGraphType { Name = "Query" };
            query.AddField(new FieldType
            {
                Name = "probe",
                Type = typeof(StringGraphType),
                Arguments = new QueryArguments(new QueryArgument(type) { Name = "item" }),
                Resolver = new FuncFieldResolver<string>(_ => null),
            });

            var schema = new Schema { Query = query };
            schema.Initialize();

            return type;
        }

        private sealed class TestNewCartItem : NewCartItem
        {
        }
    }
}
