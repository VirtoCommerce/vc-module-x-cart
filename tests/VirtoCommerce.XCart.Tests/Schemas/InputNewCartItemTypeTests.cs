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
    // These tests exercise the global, static AbstractTypeFactory<NewCartItem> registry, which has no
    // removal API in the consumed platform version. Rather than rely on a pristine global (which a
    // sibling test could have dirtied), each test establishes its own precondition: the base-type tests
    // force a direct "NewCartItem" name entry via RegisterType, which wins over the inheritance-chain
    // fallback that resolves a registered override. No XCart test constructs NewCartItem through the
    // factory, so the residual registration is benign for the rest of the suite.
    public class InputNewCartItemTypeTests
    {
        [Fact]
        public void ParseDictionary_WithOverride_YieldsOverrideType()
        {
            AbstractTypeFactory<NewCartItem>.OverrideType<NewCartItem, TestNewCartItem>();
            var type = BuildInitializedType();

            var parsed = type.ParseDictionary(new Dictionary<string, object>
            {
                ["productId"] = "p1",
                ["quantity"] = 2,
            });

            parsed.Should().BeOfType<TestNewCartItem>();
            ((NewCartItem)parsed).ProductId.Should().Be("p1");
            ((NewCartItem)parsed).Quantity.Should().Be(2);
        }

        [Fact]
        public void ParseDictionary_WithoutOverride_YieldsBaseType()
        {
            // RegisterType adds a direct "NewCartItem" name entry, which resolves to the base type even
            // if a sibling test registered an override. Proves GraphQL.NET picks the parameterless ctor
            // for the two-ctor NewCartItem without any [GraphQLConstructor] (same as InputNewWishlistItemType).
            AbstractTypeFactory<NewCartItem>.RegisterType<NewCartItem>();
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
            AbstractTypeFactory<NewCartItem>.RegisterType<NewCartItem>();
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
        // initialized instance. The factory override/registration must be set BEFORE this runs, because
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
