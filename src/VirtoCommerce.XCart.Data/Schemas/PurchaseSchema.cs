using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CartModule.Core.Model.Search;
using VirtoCommerce.CartModule.Core.Services;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Commands;
using VirtoCommerce.XCart.Core.Commands.BaseCommands;
using VirtoCommerce.XCart.Core.Extensions;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Queries;
using VirtoCommerce.XCart.Core.Schemas;
using VirtoCommerce.XCart.Data.Authorization;
using VirtoCommerce.XPurchase.Schemas;
using static VirtoCommerce.Xapi.Core.ModuleConstants;
using static VirtoCommerce.XCart.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Data.Schemas
{
    public class PurchaseSchema : ISchemaBuilder
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IShoppingCartService _cartService;
        private readonly IShoppingCartSearchService _shoppingCartSearchService;
        private readonly IDistributedLockService _distributedLockService;
        private readonly IUserManagerCore _userManagerCore;
        private readonly IMemberResolver _memberResolver;

        public const string CartPrefix = "Cart";

        public PurchaseSchema(
            IMediator mediator,
            IAuthorizationService authorizationService,
            IShoppingCartService cartService,
            IShoppingCartSearchService shoppingCartSearchService,
            IDistributedLockService distributedLockService,
            IUserManagerCore userManagerCore,
            IMemberResolver memberResolver)
        {
            _mediator = mediator;
            _authorizationService = authorizationService;
            _cartService = cartService;
            _shoppingCartSearchService = shoppingCartSearchService;
            _distributedLockService = distributedLockService;
            _userManagerCore = userManagerCore;
            _memberResolver = memberResolver;
        }

        public void Build(ISchema schema)
        {
            ValueConverter.Register<ExpCartAddress, Optional<ExpCartAddress>>(x => new Optional<ExpCartAddress>(x));

            //Mutations
            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddItemType!){ addItem(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "productId": "9cbd8f316e254a679ba34a900fccb076",
            ///          "quantity": 1
            ///          "dynamicProperties": [
            ///             {
            ///                 "name": "ItemProperty",
            ///                 "value": "test value"
            ///            }]
            ///      }
            ///   }
            /// }
            /// </example>
            var addItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                           .Name("addItem")
                                           .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddItemType>>(), SchemaConstants.CommandName)
                                           .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                           {
                                               var cartCommand = context.GetCartCommand<AddCartItemCommand>();

                                               await CheckAuthByCartCommandAsync(context, cartCommand);

                                               //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                               var cartAggregate = await _mediator.Send(cartCommand);

                                               //store cart aggregate in the user context for future usage in the graph types resolvers
                                               context.SetExpandedObjectGraph(cartAggregate);
                                               return cartAggregate;
                                           })
                                           .FieldType;

            schema.Mutation.AddField(addItemField);

            schema.Mutation.AddField(FieldBuilder
                .Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                .Name("addGiftItems")
                .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddGiftItemsType>>(), SchemaConstants.CommandName)
                .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                {
                    var cartCommand = context.GetCartCommand<AddGiftItemsCommand>();

                    await CheckAuthByCartCommandAsync(context, cartCommand);

                    var cartAggregate = await _mediator.Send(cartCommand);

                    //store cart aggregate in the user context for future usage in the graph types resolvers
                    context.SetExpandedObjectGraph(cartAggregate);
                    return cartAggregate;
                })
                .FieldType
            );

            schema.Mutation.AddField(FieldBuilder
                .Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                .Name("rejectGiftItems")
                .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRejectGiftItemsType>>(), SchemaConstants.CommandName)
                .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                {
                    var cartCommand = context.GetCartCommand<RejectGiftCartItemsCommand>();

                    await CheckAuthByCartCommandAsync(context, cartCommand);

                    var cartAggregate = await _mediator.Send(cartCommand);

                    //store cart aggregate in the user context for future usage in the graph types resolvers
                    context.SetExpandedObjectGraph(cartAggregate);
                    return cartAggregate;
                })
                .FieldType
            );

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputClearCartType!){ clearCart(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart"
            ///      }
            ///   }
            /// }
            /// </example>
            var clearCartField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                             .Name("clearCart")
                                             .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputClearCartType>>(), SchemaConstants.CommandName)
                                             .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                             {
                                                 var cartCommand = context.GetCartCommand<ClearCartCommand>();

                                                 await CheckAuthByCartCommandAsync(context, cartCommand);

                                                 //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                 var cartAggregate = await _mediator.Send(cartCommand);

                                                 //store cart aggregate in the user context for future usage in the graph types resolvers
                                                 context.SetExpandedObjectGraph(cartAggregate);
                                                 return cartAggregate;
                                             }).FieldType;

            schema.Mutation.AddField(clearCartField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputChangeCommentType!){ changeComment(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "comment": "Hi, Virto!"
            ///      }
            ///   }
            /// }
            /// </example>
            var changeCommentField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("changeComment")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeCommentType>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                 {
                                                     var cartCommand = context.GetCartCommand<ChangeCommentCommand>();

                                                     await CheckAuthByCartCommandAsync(context, cartCommand);

                                                     //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                     var cartAggregate = await _mediator.Send(cartCommand);

                                                     //store cart aggregate in the user context for future usage in the graph types resolvers
                                                     context.SetExpandedObjectGraph(cartAggregate);
                                                     return cartAggregate;
                                                 })
                                                 .FieldType;

            schema.Mutation.AddField(changeCommentField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputChangeCartItemPriceType!){ changeCartItemPrice(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "lineItemId": "9cbd8f316e254a679ba34a900fccb076",
            ///          "price": 777
            ///      }
            ///   }
            /// }
            /// </example>
            var changeCartItemPriceField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                       .Name("changeCartItemPrice")
                                                       .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputChangeCartItemPriceType>>(), SchemaConstants.CommandName)
                                                       .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                       {
                                                           var cartCommand = context.GetCartCommand<ChangeCartItemPriceCommand>();

                                                           await CheckAuthByCartCommandAsync(context, cartCommand);

                                                           //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                           var cartAggregate = await _mediator.Send(cartCommand);

                                                           //store cart aggregate in the user context for future usage in the graph types resolvers
                                                           context.SetExpandedObjectGraph(cartAggregate);
                                                           return cartAggregate;
                                                       }).FieldType;

            schema.Mutation.AddField(changeCartItemPriceField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputChangeCartItemQuantityType!){ changeCartItemQuantity(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "lineItemId": "9cbd8f316e254a679ba34a900fccb076",
            ///          "quantity": 777
            ///      }
            ///   }
            /// }
            /// </example>
            var changeCartItemQuantityField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                          .Name("changeCartItemQuantity")
                                                          .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputChangeCartItemQuantityType>>(), SchemaConstants.CommandName)
                                                          .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                          {
                                                              var cartCommand = context.GetCartCommand<ChangeCartItemQuantityCommand>();

                                                              await CheckAuthByCartCommandAsync(context, cartCommand);

                                                              //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                              var cartAggregate = await _mediator.Send(cartCommand);

                                                              //store cart aggregate in the user context for future usage in the graph types resolvers
                                                              context.SetExpandedObjectGraph(cartAggregate);
                                                              return cartAggregate;
                                                          }).FieldType;

            schema.Mutation.AddField(changeCartItemQuantityField);


            var changeCartItemsQuantityField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                          .Name("changeCartItemsQuantity")
                                                          .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputChangeCartItemsQuantityType>>(), SchemaConstants.CommandName)
                                                          .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                          {
                                                              var cartCommand = context.GetCartCommand<ChangeCartItemsQuantityCommand>();

                                                              await CheckAuthByCartCommandAsync(context, cartCommand);

                                                              //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                              var cartAggregate = await _mediator.Send(cartCommand);

                                                              //store cart aggregate in the user context for future usage in the graph types resolvers
                                                              context.SetExpandedObjectGraph(cartAggregate);
                                                              return cartAggregate;
                                                          }).FieldType;

            schema.Mutation.AddField(changeCartItemsQuantityField);

            var changeCartConfiguredItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                              .Name("changeCartConfiguredItem")
                                              .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputChangeCartConfiguredItemType>>(), SchemaConstants.CommandName)
                                              .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                              {
                                                  var cartCommand = context.GetCartCommand<ChangeCartConfiguredLineItemCommand>();

                                                  await CheckAuthByCartCommandAsync(context, cartCommand);

                                                  //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                  var cartAggregate = await _mediator.Send(cartCommand);

                                                  //store cart aggregate in the user context for future usage in the graph types resolvers
                                                  context.SetExpandedObjectGraph(cartAggregate);
                                                  return cartAggregate;
                                              }).FieldType;

            schema.Mutation.AddField(changeCartConfiguredItemField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputChangeCartItemCommentType!){ changeCartItemComment(command: $command) }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "lineItemId": "9cbd8f316e254a679ba34a900fccb076",
            ///          "comment": "verynicecomment"
            ///      }
            ///   }
            /// }
            /// </example>
            var changeCartItemCommentField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                          .Name("changeCartItemComment")
                                                          .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeCartItemCommentType>(), SchemaConstants.CommandName)
                                                          .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                          {
                                                              var cartCommand = context.GetCartCommand<ChangeCartItemCommentCommand>();

                                                              await CheckAuthByCartCommandAsync(context, cartCommand);

                                                              //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                              var cartAggregate = await _mediator.Send(cartCommand);

                                                              //store cart aggregate in the user context for future usage in the graph types resolvers
                                                              context.SetExpandedObjectGraph(cartAggregate);
                                                              return cartAggregate;
                                                          }).FieldType;

            schema.Mutation.AddField(changeCartItemCommentField);

            #region Change selected items

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputChangeCartItemSelectedType!){ changeCartItemSelected(command: $command) { id } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "lineItemId": "9cbd8f316e254a679ba34a900fccb076",
            ///          "selectedForCheckout": false
            ///      }
            ///   }
            /// }
            /// </example>
            var changeCartItemSelectedField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                          .Name("changeCartItemSelected")
                                                          .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeCartItemSelectedType>(), SchemaConstants.CommandName)
                                                          .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                          {
                                                              var cartCommand = context.GetCartCommand<ChangeCartItemSelectedCommand>();

                                                              await CheckAuthByCartCommandAsync(context, cartCommand);

                                                              var cartAggregate = await _mediator.Send(cartCommand);

                                                              context.SetExpandedObjectGraph(cartAggregate);
                                                              return cartAggregate;
                                                          }).FieldType;

            schema.Mutation.AddField(changeCartItemSelectedField);

            var selectCartItems = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                              .Name("selectCartItems")
                                              .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeCartItemsSelectedType>(), SchemaConstants.CommandName)
                                              .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                              {
                                                  var cartCommand = context.GetCartCommand<ChangeCartItemsSelectedCommand>();
                                                  cartCommand.SelectedForCheckout = true;

                                                  await CheckAuthByCartCommandAsync(context, cartCommand);

                                                  var cartAggregate = await _mediator.Send(cartCommand);
                                                  context.SetExpandedObjectGraph(cartAggregate);

                                                  return cartAggregate;
                                              }).FieldType;

            schema.Mutation.AddField(selectCartItems);

            var unSelectCartItems = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                .Name("unSelectCartItems")
                                                .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeCartItemsSelectedType>(), SchemaConstants.CommandName)
                                                .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                {
                                                    var cartCommand = context.GetCartCommand<ChangeCartItemsSelectedCommand>();

                                                    await CheckAuthByCartCommandAsync(context, cartCommand);

                                                    var cartAggregate = await _mediator.Send(cartCommand);
                                                    context.SetExpandedObjectGraph(cartAggregate);

                                                    return cartAggregate;
                                                }).FieldType;

            schema.Mutation.AddField(unSelectCartItems);

            var selectAllCartItems = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("selectAllCartItems")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeAllCartItemsSelectedType>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                 {
                                                     var cartCommand = context.GetCartCommand<ChangeAllCartItemsSelectedCommand>();
                                                     cartCommand.SelectedForCheckout = true;

                                                     await CheckAuthByCartCommandAsync(context, cartCommand);

                                                     var cartAggregate = await _mediator.Send(cartCommand);
                                                     context.SetExpandedObjectGraph(cartAggregate);

                                                     return cartAggregate;
                                                 }).FieldType;

            schema.Mutation.AddField(selectAllCartItems);

            var unSelectAllCartItems = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                   .Name("unSelectAllCartItems")
                                                   .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangeAllCartItemsSelectedType>(), SchemaConstants.CommandName)
                                                   .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                   {
                                                       var cartCommand = context.GetCartCommand<ChangeAllCartItemsSelectedCommand>();

                                                       await CheckAuthByCartCommandAsync(context, cartCommand);

                                                       var cartAggregate = await _mediator.Send(cartCommand);
                                                       context.SetExpandedObjectGraph(cartAggregate);

                                                       return cartAggregate;
                                                   }).FieldType;

            schema.Mutation.AddField(unSelectAllCartItems);

            #endregion

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputRemoveItemType!){ removeCartItem(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "lineItemId": "9cbd8f316e254a679ba34a900fccb076"
            ///      }
            ///   }
            /// }
            /// </example>
            var removeCartItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                  .Name("removeCartItem")
                                                  .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveItemType>>(), SchemaConstants.CommandName)
                                                  .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<RemoveCartItemCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  }).FieldType;

            schema.Mutation.AddField(removeCartItemField);

            var removeCartItemsField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                  .Name("removeCartItems")
                                                  .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveItemsType>>(), SchemaConstants.CommandName)
                                                  .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<RemoveCartItemsCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  }).FieldType;

            schema.Mutation.AddField(removeCartItemsField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddCouponType!){ addCoupon(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "couponCode": "verynicecouponcode"
            ///      }
            ///   }
            /// }
            /// </example>
            var addCouponField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                             .Name("addCoupon")
                                             .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddCouponType>>(), SchemaConstants.CommandName)
                                             .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                             {
                                                 var cartCommand = context.GetCartCommand<AddCouponCommand>();

                                                 await CheckAuthByCartCommandAsync(context, cartCommand);

                                                 //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                 var cartAggregate = await _mediator.Send(cartCommand);

                                                 //store cart aggregate in the user context for future usage in the graph types resolvers
                                                 context.SetExpandedObjectGraph(cartAggregate);
                                                 return cartAggregate;
                                             }).FieldType;

            schema.Mutation.AddField(addCouponField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputRemoveCouponType!){ removeCoupon(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "couponCode": "verynicecouponcode"
            ///      }
            ///   }
            /// }
            /// </example>
            var removeCouponField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                .Name("removeCoupon")
                                                .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveCouponType>>(), SchemaConstants.CommandName)
                                                .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                {
                                                    var cartCommand = context.GetCartCommand<RemoveCouponCommand>();

                                                    await CheckAuthByCartCommandAsync(context, cartCommand);

                                                    //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                    var cartAggregate = await _mediator.Send(cartCommand);

                                                    //store cart aggregate in the user context for future usage in the graph types resolvers
                                                    context.SetExpandedObjectGraph(cartAggregate);
                                                    return cartAggregate;
                                                }).FieldType;

            schema.Mutation.AddField(removeCouponField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputRemoveShipmentType!){ removeShipment(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "shipmentId": "7777-7777-7777-7777"
            ///      }
            ///   }
            /// }
            /// </example>
            var removeShipmentField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                  .Name("removeShipment")
                                                  .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveShipmentType>>(), SchemaConstants.CommandName)
                                                  .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<RemoveShipmentCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  }).FieldType;

            schema.Mutation.AddField(removeShipmentField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddOrUpdateCartShipmentType!){ addOrUpdateCartShipment(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "shipment": {
            ///             "id": "7557d2ee-8173-46bb-95eb-64f23e1316e3",
            ///             "fulfillmentCenterId": "tulsa-branch",
            ///             "height": 10,
            ///             "length": 10,
            ///             "measureUnit": "cm",
            ///             "shipmentMethodCode": "FixedRate",
            ///             "shipmentMethodOption": "Ground",
            //              "volumetricWeight": 10,
            ///             "weight": 50,
            ///             "weightUnit": "kg",
            ///             "width": 10,
            ///             "currency": "USD",
            ///             "price": 10,
            ///             "dynamicProperties": [
            ///                 {
            ///                     "name": "ShipmentProperty",
            ///                     "value": "test value"
            ///                 }],
            ///             "deliveryAddress": {
            ///                 "city": "CityName",
            ///                 "countryCode": "RU",
            ///                 "countryName": "Russia",
            ///                 "email": "Email@test",
            ///                 "firstName": "First test name",
            ///                 "key": "KeyTest",
            ///                 "lastName": "Last name test",
            ///                 "line1": "Address Line 1",
            ///                 "line2": "Address line 2",
            ///                 "middleName": "Test Middle Name",
            ///                 "name": "First name address",
            ///                 "organization": "OrganizationTestName",
            ///                 "phone": "88005553535",
            ///                 "postalCode": "12345",
            ///                 "regionId": "RegId",
            ///                 "regionName": "Region Name",
            ///                 "zip": "12345",
            ///                 "addressType": 2
            ///           }
            ///        }
            ///      }
            ///   }
            /// }
            /// </example>
            var addOrUpdateCartShipmentField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                           .Name("addOrUpdateCartShipment")
                                                           .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddOrUpdateCartShipmentType>>(), SchemaConstants.CommandName)
                                                           .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                           {
                                                               var cartCommand = context.GetCartCommand<AddOrUpdateCartShipmentCommand>();

                                                               await CheckAuthByCartCommandAsync(context, cartCommand);

                                                               //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                               var cartAggregate = await _mediator.Send(cartCommand);

                                                               //store cart aggregate in the user context for future usage in the graph types resolvers
                                                               context.SetExpandedObjectGraph(cartAggregate);
                                                               return cartAggregate;
                                                           }).FieldType;

            schema.Mutation.AddField(addOrUpdateCartShipmentField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddOrUpdateCartPaymentType!){ addOrUpdateCartPayment(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "payment": {
            ///             "id": "7557d2ee-8173-46bb-95eb-64f23e1316e3",
            ///             "outerId": "ID1",
            ///             "paymentGatewayCode": "ManualTestPaymentMethod",
            ///             "dynamicProperties": [
            ///                 {
            ///                     "name": "PaymentProperty",
            ///                     "value": "test value"
            ///                 }],
            ///             "billingAddress": {
            ///                 "city": "CityName",
            ///                 "countryCode": "RU",
            ///                 "countryName": "Russia",
            ///                 "email": "Email@test",
            ///                 "firstName": "First test name",
            ///                 "key": "KeyTest",
            ///                 "lastName": "Last name test",
            ///                 "line1": "Address Line 1",
            ///                 "line2": "Address line 2",
            ///                 "middleName": "Test Middle Name",
            ///                 "name": "First name address",
            ///                 "organization": "OrganizationTestName",
            ///                 "phone": "88005553535",
            ///                 "postalCode": "12345",
            ///                 "regionId": "RegId",
            ///                 "regionName": "Region Name",
            ///                 "zip": "12345",
            ///                 "addressType": 1
            ///             },
            ///             "currency": "USD",
            ///             "price": "1001",
            ///             "amount": "1001"
            ///          }
            ///      }
            ///   }
            /// }
            /// </example>
            var addOrUpdateCartPaymentField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                          .Name("addOrUpdateCartPayment")
                                                          .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddOrUpdateCartPaymentType>>(), SchemaConstants.CommandName)
                                                           .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                           {
                                                               var cartCommand = context.GetCartCommand<AddOrUpdateCartPaymentCommand>();

                                                               await CheckAuthByCartCommandAsync(context, cartCommand);

                                                               //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                               var cartAggregate = await _mediator.Send(cartCommand);

                                                               //store cart aggregate in the user context for future usage in the graph types resolvers
                                                               context.SetExpandedObjectGraph(cartAggregate);
                                                               return cartAggregate;
                                                           }).FieldType;

            schema.Mutation.AddField(addOrUpdateCartPaymentField);

            var validateCouponQueryField = new FieldType
            {
                Name = "validateCoupon",
                Arguments = new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "cartId", Description = "Cart Id" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId", Description = "Store Id" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "currencyCode", Description = "Currency code (\"USD\")" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "userId", Description = "User Id" },
                    new QueryArgument<StringGraphType> { Name = "cultureName", Description = "Culture name (\"en-Us\")" },
                    new QueryArgument<StringGraphType> { Name = "cartName", Description = "Cart name" },
                    new QueryArgument<StringGraphType> { Name = "cartType", Description = "Cart type" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "coupon", Description = "Cart promo coupon code" }),
                Type = typeof(BooleanGraphType),
                Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var query = context.GetCartQuery<ValidateCouponQuery>();
                    query.CartId = context.GetArgumentOrValue<string>("cartId");
                    query.Coupon = context.GetArgumentOrValue<string>("coupon");

                    await CheckAuthByCartParamsAsync(context, query);

                    return await _mediator.Send(query);
                })
            };
            schema.Query.AddField(validateCouponQueryField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:MergeCartType!){ mergeCart(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "secondCartId": "7777-7777-7777-7777"
            ///      }
            ///   }
            /// }
            /// </example>
            var margeCartField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                             .Name("mergeCart")
                                             .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputMergeCartType>>(), SchemaConstants.CommandName)
                                             .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                             {
                                                 var cartCommand = context.GetCartCommand<MergeCartCommand>();

                                                 await CheckAuthByCartCommandAsync(context, cartCommand);

                                                 //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                 var cartAggregate = await _mediator.Send(cartCommand);

                                                 //store cart aggregate in the user context for future usage in the graph types resolvers
                                                 context.SetExpandedObjectGraph(cartAggregate);
                                                 return cartAggregate;
                                             }).FieldType;

            schema.Mutation.AddField(margeCartField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputRemoveCartType!){ removeCart(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "cartId": "7777-7777-7777-7777"
            ///      }
            ///   }
            /// }
            /// </example>
            var removeCartField = FieldBuilder.Create<CartAggregate, bool>(typeof(BooleanGraphType))
                                              .Name("removeCart")
                                              .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveCartType>>(), SchemaConstants.CommandName)
                                              .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                              {
                                                  var command = context.GetArgument(GenericTypeHelper.GetActualType<RemoveCartCommand>(), SchemaConstants.CommandName) as RemoveCartCommand;

                                                  await CheckAuthAsyncByCartId(context, command.CartId);

                                                  return await _mediator.Send(command);
                                              })
                                              .FieldType;

            schema.Mutation.AddField(removeCartField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputClearShipmentsType!){ clearShipments(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "cartId": "7777-7777-7777-7777"
            ///      }
            ///   }
            /// }
            /// </example>
            var clearShipmentsField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                  .Name("clearShipments")
                                                  .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputClearShipmentsType>>(), SchemaConstants.CommandName)
                                                  .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<ClearShipmentsCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  })
                                                  .FieldType;

            schema.Mutation.AddField(clearShipmentsField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputClearPaymentsType!){ clearPayments(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "cartId": "7777-7777-7777-7777"
            ///      }
            ///   }
            /// }
            /// </example>
            var clearPaymentsField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("clearPayments")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputClearPaymentsType>>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<ClearPaymentsCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  })
                                                 .FieldType;

            schema.Mutation.AddField(clearPaymentsField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddOrUpdateCartAddressType!){ addOrUpdateCartAddress(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "address": {
            ///             "line1":"st street 1"
            ///         }
            ///      }
            ///   }
            /// }
            /// </example>
            var addOrUpdateCartAddress = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("addOrUpdateCartAddress")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddOrUpdateCartAddressType>>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                  {
                                                      var cartCommand = context.GetCartCommand<AddOrUpdateCartAddressCommand>();

                                                      await CheckAuthByCartCommandAsync(context, cartCommand);

                                                      //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                                      var cartAggregate = await _mediator.Send(cartCommand);

                                                      //store cart aggregate in the user context for future usage in the graph types resolvers
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  })
                                                 .FieldType;

            schema.Mutation.AddField(addOrUpdateCartAddress);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputRemoveCartAddressType!){ removeCartAddress(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "cartId": "7777-7777-7777-7777",
            ///          "addressId": "111"
            ///      }
            ///   }
            /// }
            /// </example>
            var removeCartAddressField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                              .Name("removeCartAddress")
                                              .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveCartAddressType>>(), SchemaConstants.CommandName)
                                              .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                              {
                                                  var cartCommand = context.GetCartCommand<RemoveCartAddressCommand>();

                                                  await CheckAuthByCartCommandAsync(context, cartCommand);

                                                  var cartAggregate = await _mediator.Send(cartCommand);
                                                  context.SetExpandedObjectGraph(cartAggregate);
                                                  return cartAggregate;
                                              })
                                              .FieldType;

            schema.Mutation.AddField(removeCartAddressField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// mutation ($command: InputAddItemsType!) { addItemsCart(command: $command) }
            /// "variables": {
            ///    "command": {
            ///         "storeId": "Electronics",
            ///         "cartName": "default",
            ///         "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///         "currencyCode": "USD",
            ///         "cultureName": "en-US",
            ///         "cartType": "cart",
            ///         "cartId": "",
            ///         "cartItems": [{
            ///             "productId": "1111-1111-1111-1111",
            ///             "quantity": 2,
            ///             "dynamicProperties": [
            ///                 {
            ///                     "name": "ItemProperty",
            ///                     "value": "test value"
            ///                 }]
            ///         },
            ///         {
            ///             "productId": "2222-2222-2222-2222",
            ///             "quantity": 5
            ///         }]
            ///     }
            /// }
            /// </example>
            var addItemsCartField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("addItemsCart")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddItemsType>>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                 {
                                                     var cartCommand = context.GetCartCommand<AddCartItemsCommand>();

                                                     await CheckAuthByCartCommandAsync(context, cartCommand);

                                                     var cartAggregate = await _mediator.Send(cartCommand);

                                                     context.SetExpandedObjectGraph(cartAggregate);

                                                     return cartAggregate;
                                                 })
                                                 .FieldType;

            schema.Mutation.AddField(addItemsCartField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// mutation ($command: InputAddItemsBulkType!) { addItemsCart(command: $command) }
            /// "variables": {
            ///    "command": {
            ///         "storeId": "Electronics",
            ///         "cartName": "default",
            ///         "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///         "currencyCode": "USD",
            ///         "cultureName": "en-US",
            ///         "cartType": "cart",
            ///         "cartId": "",
            ///         "cartItems": [{
            ///             "productSku": "1111-1111-1111-1111",
            ///             "quantity": 2
            ///         },
            ///         {
            ///             "productSku": "2222-2222-2222-2222",
            ///             "quantity": 5
            ///         }]
            ///     }
            /// }
            /// </example>
            var addItemsBulkCartField = FieldBuilder.Create<BulkCartResult, BulkCartResult>(GraphTypeExtenstionHelper.GetActualType<BulkCartType>())
                                                 .Name("addBulkItemsCart")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddBulkItemsType>>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                 {
                                                     var cartCommand = context.GetCartCommand<AddCartItemsBulkCommand>();

                                                     if (!string.IsNullOrEmpty(cartCommand.CartId))
                                                     {
                                                         await CheckAuthAsyncByCartId(context, cartCommand.CartId);
                                                     }
                                                     else
                                                     {
                                                         await CheckAuthByCartParamsAsync(context, cartCommand);
                                                     }

                                                     var result = await _mediator.Send(cartCommand);

                                                     context.SetExpandedObjectGraph(result.Cart);

                                                     return result;
                                                 })
                                                 .FieldType;

            schema.Mutation.AddField(addItemsBulkCartField);

            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": mutation ($command: InputAddOrUpdateCartAddressType!) { addCartAddress(command: $command) }
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "address": {
            ///             "line1":"st street 1"
            ///           }
            ///       }
            ///    }
            /// }
            /// </example>
            var addCartAddressField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                 .Name("addCartAddress")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddOrUpdateCartAddressType>>(), SchemaConstants.CommandName)
                                                 .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                 {
                                                     var cartCommand = context.GetCartCommand<AddCartAddressCommand>();

                                                     await CheckAuthByCartCommandAsync(context, cartCommand);

                                                     var cartAggregate = await _mediator.Send(cartCommand);
                                                     context.SetExpandedObjectGraph(cartAggregate);
                                                     return cartAggregate;
                                                 })
                                                 .FieldType;

            schema.Mutation.AddField(addCartAddressField);

            var updateCartDynamicPropertiesField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                     .Name("updateCartDynamicProperties")
                                                     .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputUpdateCartDynamicPropertiesType>>(), SchemaConstants.CommandName)
                                                     .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                     {
                                                         var cartCommand = context.GetCartCommand<UpdateCartDynamicPropertiesCommand>();

                                                         await CheckAuthByCartCommandAsync(context, cartCommand);

                                                         var cartAggregate = await _mediator.Send(cartCommand);
                                                         context.SetExpandedObjectGraph(cartAggregate);
                                                         return cartAggregate;
                                                     })
                                                     .FieldType;

            schema.Mutation.AddField(updateCartDynamicPropertiesField);

            var updateCartItemDynamicPropertiesField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                        .Name("updateCartItemDynamicProperties")
                                                        .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputUpdateCartItemDynamicPropertiesType>>(), SchemaConstants.CommandName)
                                                        .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                        {
                                                            var cartCommand = context.GetCartCommand<UpdateCartItemDynamicPropertiesCommand>();

                                                            await CheckAuthByCartCommandAsync(context, cartCommand);

                                                            var cartAggregate = await _mediator.Send(cartCommand);
                                                            context.SetExpandedObjectGraph(cartAggregate);
                                                            return cartAggregate;
                                                        })
                                                        .FieldType;

            schema.Mutation.AddField(updateCartItemDynamicPropertiesField);

            var updateCartPaymentDynamicPropertiesField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                            .Name("updateCartPaymentDynamicProperties")
                                                            .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputUpdateCartPaymentDynamicPropertiesType>>(), SchemaConstants.CommandName)
                                                            .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                            {
                                                                var cartCommand = context.GetCartCommand<UpdateCartPaymentDynamicPropertiesCommand>();

                                                                await CheckAuthByCartCommandAsync(context, cartCommand);

                                                                var cartAggregate = await _mediator.Send(cartCommand);
                                                                context.SetExpandedObjectGraph(cartAggregate);
                                                                return cartAggregate;
                                                            })
                                                            .FieldType;

            schema.Mutation.AddField(updateCartPaymentDynamicPropertiesField);

            var updateCartShipmentDynamicPropertiesField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                                               .Name("updateCartShipmentDynamicProperties")
                                                               .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputUpdateCartShipmentDynamicPropertiesType>>(), SchemaConstants.CommandName)
                                                               .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                                               {
                                                                   var cartCommand = context.GetCartCommand<UpdateCartShipmentDynamicPropertiesCommand>();

                                                                   await CheckAuthByCartCommandAsync(context, cartCommand);

                                                                   var cartAggregate = await _mediator.Send(cartCommand);
                                                                   context.SetExpandedObjectGraph(cartAggregate);
                                                                   return cartAggregate;
                                                               })
                                                               .FieldType;

            schema.Mutation.AddField(updateCartShipmentDynamicPropertiesField);

            var changePurchaseOrderNumberField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                     .Name("changePurchaseOrderNumber")
                                     .Argument(GraphTypeExtenstionHelper.GetActualType<InputChangePurchaseOrderNumber>(), SchemaConstants.CommandName)
                                     .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                     {
                                         var cartCommand = context.GetCartCommand<ChangePurchaseOrderNumberCommand>();

                                         await CheckAuthByCartCommandAsync(context, cartCommand);

                                         var cartAggregate = await _mediator.Send(cartCommand);
                                         context.SetExpandedObjectGraph(cartAggregate);
                                         return cartAggregate;
                                     })
                                     .FieldType;

            schema.Mutation.AddField(changePurchaseOrderNumberField);

            //Mutations
            /// <example>
            /// This is an example JSON request for a mutation
            /// {
            ///   "query": "mutation ($command:InputAddItemType!){ refreshCart(command: $command) {  total { formatedAmount } } }",
            ///   "variables": {
            ///      "command": {
            ///          "storeId": "Electronics",
            ///          "cartName": "default",
            ///          "userId": "b57d06db-1638-4d37-9734-fd01a9bc59aa",
            ///          "language": "en-US",
            ///          "currency": "USD",
            ///          "cartType": "cart",
            ///          "cartId": "",
            ///      }
            ///   }
            /// }
            /// </example>
            var refreshCartField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<CartType>())
                                           .Name("refreshCart")
                                           .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<RefreshCartType>>(), SchemaConstants.CommandName)
                                           //PT-5394: Write the unit-tests for successfully mapping input variable to the command
                                           .ResolveSynchronizedAsync(CartPrefix, "userId", _distributedLockService, async context =>
                                           {
                                               var cartCommand = context.GetCartCommand<RefreshCartCommand>();

                                               await CheckAuthByCartCommandAsync(context, cartCommand);

                                               //We need to add cartAggregate to the context to be able use it on nested cart types resolvers (e.g for currency)
                                               var cartAggregate = await _mediator.Send(cartCommand);

                                               //store cart aggregate in the user context for future usage in the graph types resolvers
                                               context.SetExpandedObjectGraph(cartAggregate);
                                               return cartAggregate;
                                           })
                                           .FieldType;

            schema.Mutation.AddField(refreshCartField);


            #region Wishlists

            // Queries

            var listField = new FieldType
            {
                Name = "wishlist",
                Arguments = new QueryArguments(
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "listId", Description = "List Id" },
                        new QueryArgument<StringGraphType> { Name = "cultureName", Description = "Culture name (\"en-Us\")" }),
                Type = GraphTypeExtenstionHelper.GetActualType<WishlistType>(),
                Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var getListQuery = AbstractTypeFactory<GetWishlistQuery>.TryCreateInstance();
                    getListQuery.ListId = context.GetArgument<string>("listId");
                    getListQuery.IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();
                    context.CopyArgumentsToUserContext();

                    var cartAggregate = await _mediator.Send(getListQuery);

                    if (cartAggregate == null)
                    {
                        return null;
                    }

                    context.UserContext["storeId"] = cartAggregate.Cart.StoreId;

                    var wishlistUserContext = await InitializeWishlistUserContext(context, cart: cartAggregate.Cart);
                    await AuthorizeAsync(context, wishlistUserContext);

                    context.SetExpandedObjectGraph(cartAggregate);

                    return cartAggregate;
                })
            };
            schema.Query.AddField(listField);

            var listConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<WishlistType, object>()
                .Name("wishlists")
                .Argument<StringGraphType>("storeId", "Store Id")
                .Argument<StringGraphType>("userId", "User Id")
                .Argument<StringGraphType>("currencyCode", "Currency Code")
                .Argument<StringGraphType>("cultureName", "Culture Name")
                .Argument<StringGraphType>("scope", "List scope")
                .Argument<StringGraphType>("sort", "The sort expression")
                .PageSize(Connections.DefaultPageSize);

            listConnectionBuilder.ResolveAsync(async context => await ResolveListConnectionAsync(_mediator, context));
            schema.Query.AddField(listConnectionBuilder.FieldType);

            // Mutations
            // Add list
            var addListField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                                                 .Name("createWishlist")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputCreateWishlistType>>(), SchemaConstants.CommandName)
                                                  .ResolveAsync(async context =>
                                                  {
                                                      var commandType = GenericTypeHelper.GetActualType<CreateWishlistCommand>();
                                                      var command = (CreateWishlistCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                                                      await AuthorizeByListIdAsync(context, command, command.Scope);
                                                      var cartAggregate = await _mediator.Send(command);
                                                      context.SetExpandedObjectGraph(cartAggregate);
                                                      return cartAggregate;
                                                  })
                                                 .FieldType;

            schema.Mutation.AddField(addListField);

            // Clone list
            var cloneListField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                                                .Name("cloneWishlist")
                                                .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputCloneWishlistType>>(), SchemaConstants.CommandName)
                                                .ResolveAsync(async context =>
                                                {
                                                    var commandType = GenericTypeHelper.GetActualType<CloneWishlistCommand>();
                                                    var command = (CloneWishlistCommand)context.GetArgument(commandType, SchemaConstants.CommandName);

                                                    await AuthorizeByListIdAsync(context, command, command.Scope);
                                                    var cartAggregate = await _mediator.Send(command);
                                                    context.SetExpandedObjectGraph(cartAggregate);
                                                    return cartAggregate;
                                                })
                                                .FieldType;

            schema.Mutation.AddField(cloneListField);

            // Change list
            var changeListField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                                                 .Name("changeWishlist")
                                                 .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputChangeWishlistType>>(), SchemaConstants.CommandName)
                                                 .ResolveAsync(async context =>
                                                 {
                                                     var commandType = GenericTypeHelper.GetActualType<ChangeWishlistCommand>();
                                                     var command = (ChangeWishlistCommand)context.GetArgument(commandType, SchemaConstants.CommandName);

                                                     await AuthorizeByListIdAsync(context, command, command.Scope);
                                                     var cartAggregate = await _mediator.Send(command);
                                                     context.SetExpandedObjectGraph(cartAggregate);
                                                     return cartAggregate;
                                                 })
                                                .FieldType;

            schema.Mutation.AddField(changeListField);

            // Rename list
            var renameListField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                                     .Name("renameWishlist")
                                     .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRenameWishlistType>>(), SchemaConstants.CommandName)
                                     .ResolveAsync(async context =>
                                     {
                                         var commandType = GenericTypeHelper.GetActualType<RenameWishlistCommand>();
                                         var command = (RenameWishlistCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                                         await AuthorizeByListIdAsync(context, command);
                                         var cartAggregate = await _mediator.Send(command);
                                         context.SetExpandedObjectGraph(cartAggregate);
                                         return cartAggregate;
                                     })
                                     .DeprecationReason("Obsolete. Use 'changeWishlist' instead.")
                                     .FieldType;

            schema.Mutation.AddField(renameListField);

            // Remove list
            var removeListField = FieldBuilder.Create<CartAggregate, bool>(typeof(BooleanGraphType))
                         .Name("removeWishlist")
                         .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveWishlistType>>(), SchemaConstants.CommandName)
                         .ResolveAsync(async context =>
                         {
                             var commandType = GenericTypeHelper.GetActualType<RemoveWishlistCommand>();
                             var command = (RemoveWishlistCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                             await AuthorizeByListIdAsync(context, command);
                             await _mediator.Send(command);
                             return true;
                         })
                         .FieldType;

            schema.Mutation.AddField(removeListField);

            // Add product to list
            var addListItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                         .Name("addWishlistItem")
                         .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddWishlistItemType>>(), SchemaConstants.CommandName)
                         .ResolveAsync(async context =>
                         {
                             var commandType = GenericTypeHelper.GetActualType<AddWishlistItemCommand>();
                             var command = (AddWishlistItemCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                             var cartAggregate = await _mediator.Send(command);
                             context.UserContext["storeId"] = cartAggregate.Cart.StoreId;
                             await AuthorizeByListIdAsync(context, command);
                             context.SetExpandedObjectGraph(cartAggregate);
                             return cartAggregate;
                         })
                         .FieldType;

            schema.Mutation.AddField(addListItemField);

            // Update wishlist item
            var updateListItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                         .Name("updateWishListItems")
                         .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputUpdateWishlistItemsType>>(), SchemaConstants.CommandName)
                         .ResolveAsync(async context =>
                         {
                             var commandType = GenericTypeHelper.GetActualType<UpdateWishlistItemsCommand>();
                             var command = (UpdateWishlistItemsCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                             var cartAggregate = await _mediator.Send(command);
                             context.UserContext["storeId"] = cartAggregate.Cart.StoreId;
                             await AuthorizeByListIdAsync(context, command);
                             context.SetExpandedObjectGraph(cartAggregate);
                             return cartAggregate;
                         })
                         .FieldType;

            schema.Mutation.AddField(updateListItemField);

            // Add product to wishlists
            var addWishlistBulkItemField = FieldBuilder.Create<BulkCartAggregateResult, BulkCartAggregateResult>(GraphTypeExtenstionHelper.GetActualType<BulkWishlistType>())
                .Name("addWishlistBulkItem")
                .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddWishlistBulkItemType>>(), SchemaConstants.CommandName)
                .ResolveAsync(async context =>
                {
                    var commandType = GenericTypeHelper.GetActualType<AddWishlistBulkItemCommand>();
                    var command = (AddWishlistBulkItemCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                    await AuthorizeByListIdsAsync(context, command, command.ListIds);
                    var cartAggregateList = await _mediator.Send(command);

                    foreach (var cartAggregate in cartAggregateList.CartAggregates)
                    {
                        context.SetExpandedObjectGraph(cartAggregate);
                    }
                    return cartAggregateList;
                })
                .FieldType;

            schema.Mutation.AddField(addWishlistBulkItemField);

            // Add products to wishlist
            var addWishlistItemsField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                                     .Name("addWishlistItems")
                                     .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputAddWishlistItemsType>>(), SchemaConstants.CommandName)
                                     .ResolveAsync(async context =>
                                     {
                                         var command = context.GetArgument<AddWishlistItemsCommand>(SchemaConstants.CommandName);
                                         await AuthorizeByListIdAsync(context, command);
                                         var cartAggregate = await _mediator.Send(command);
                                         context.SetExpandedObjectGraph(cartAggregate.Cart);
                                         return cartAggregate;
                                     })
                                     .FieldType;

            schema.Mutation.AddField(addWishlistItemsField);

            // Remove product from list
            var removeListItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                         .Name("removeWishlistItem")
                         .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveWishlistItemType>>(), SchemaConstants.CommandName)
                         .ResolveAsync(async context =>
                         {
                             var commandType = GenericTypeHelper.GetActualType<RemoveWishlistItemCommand>();
                             var command = (RemoveWishlistItemCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                             await AuthorizeByListIdAsync(context, command);
                             var cartAggregate = await _mediator.Send(command);
                             context.SetExpandedObjectGraph(cartAggregate);
                             return cartAggregate;
                         })
                         .FieldType;

            schema.Mutation.AddField(removeListItemField);

            // Remove products from list
            var removeListItemsField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                         .Name("removeWishlistItems")
                         .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputRemoveWishlistItemsType>>(), SchemaConstants.CommandName)
                         .ResolveAsync(async context =>
                         {
                             var commandType = GenericTypeHelper.GetActualType<RemoveWishlistItemsCommand>();
                             var command = (RemoveWishlistItemsCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                             await AuthorizeByListIdAsync(context, command);
                             var cartAggregate = await _mediator.Send(command);
                             context.SetExpandedObjectGraph(cartAggregate);
                             return cartAggregate;
                         })
                         .FieldType;

            schema.Mutation.AddField(removeListItemsField);

            // Move product to another list
            var moveListItemField = FieldBuilder.Create<CartAggregate, CartAggregate>(GraphTypeExtenstionHelper.GetActualType<WishlistType>())
                     .Name("moveWishlistItem")
                     .Argument(GraphTypeExtenstionHelper.GetActualComplexType<NonNullGraphType<InputMoveWishlistItemType>>(), SchemaConstants.CommandName)
                     .ResolveAsync(async context =>
                     {
                         var commandType = GenericTypeHelper.GetActualType<MoveWishlistItemCommand>();
                         var command = (MoveWishlistItemCommand)context.GetArgument(commandType, SchemaConstants.CommandName);
                         await AuthorizeByListIdsAsync(context, command, [command.ListId, command.DestinationListId]);
                         var cartAggregate = await _mediator.Send(command);
                         context.SetExpandedObjectGraph(cartAggregate);
                         return cartAggregate;
                     })
                     .FieldType;

            schema.Mutation.AddField(moveListItemField);

            #endregion Wishlists
        }

        private async Task<object> ResolveListConnectionAsync(IMediator mediator, IResolveConnectionContext<object> context)
        {
            var first = context.First;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());

            var query = context.GetCartQuery<SearchWishlistQuery>();
            query.Scope = context.GetArgument<string>("scope");
            query.Skip = skip;
            query.Take = first ?? context.PageSize ?? Connections.DefaultPageSize;
            query.Sort = context.GetArgument<string>("sort");
            query.IncludeFields = context.SubFields.Values.GetAllNodesPaths(context).ToArray();

            context.CopyArgumentsToUserContext();

            await AuthorizeAsync(context, query);

            var response = await mediator.Send(query);
            foreach (var cartAggregate in response.Results)
            {
                context.SetExpandedObjectGraph(cartAggregate);
            }

            return new PagedConnection<CartAggregate>(response.Results, query.Skip, query.Take, response.TotalCount);
        }

        private async Task CheckAuthByCartCommandAsync(IResolveFieldContext context, CartCommand cartCommand)
        {
            if (!string.IsNullOrEmpty(cartCommand.CartId))
            {
                await CheckAuthAsyncByCartId(context, cartCommand.CartId);
            }
            else
            {
                await CheckAuthByCartParamsAsync(context, cartCommand);
            }
        }

        private async Task CheckAuthByCartParamsAsync(IResolveFieldContext context, ICartRequest request)
        {
            var criteria = new ShoppingCartSearchCriteria
            {
                StoreId = request.StoreId,
                CustomerId = request.UserId ?? AnonymousUser.UserName,
                OrganizationId = request.OrganizationId,
                Name = request.CartName,
                Currency = request.CurrencyCode,
                Type = request.CartType,
                LanguageCode = request.CultureName,
            };

            var cartSearchResult = await _shoppingCartSearchService.SearchAsync(criteria);
            var cart = cartSearchResult.Results.FirstOrDefault(x => request.CartType != null || x.Type == null);

            if (cart == null)
            {
                await AuthorizeAsync(context, request.UserId);
            }
            else
            {
                await AuthorizeAsync(context, cart);
            }
        }

        private async Task CheckAuthAsyncByCartId(IResolveFieldContext context, string cartId)
        {
            var cart = await _cartService.GetByIdAsync(cartId, CartResponseGroup.Default.ToString());

            if (cart == null)
            {
                return;
            }

            await AuthorizeAsync(context, cart);
        }

        private async Task AuthorizeByListIdAsync(IResolveFieldContext context, WishlistCommand command, string scope = null)
        {
            var wishlistUserContext = await InitializeWishlistUserContext(context, listId: command?.ListId, userId: command?.UserId, scope: scope);
            await AuthorizeByListAsync(context, wishlistUserContext, command);
        }

        private async Task AuthorizeByListIdsAsync(IResolveFieldContext context, WishlistCommand command, IList<string> listIds)
        {
            var currentUserId = context.GetCurrentUserId();
            var currentOrganizationId = context.GetCurrentOrganizationId();
            var contact = await _memberResolver.ResolveMemberByIdAsync(currentUserId) as Contact;
            var carts = await _cartService.GetAsync(listIds);

            foreach (var cart in carts)
            {
                var wishlistUserContext = new WishlistUserContext
                {
                    CurrentUserId = currentUserId,
                    CurrentOrganizationId = currentOrganizationId,
                    CurrentContact = contact,
                    Cart = cart,
                };
                InitializeWishlistUserContextScope(wishlistUserContext);

                await AuthorizeByListAsync(context, wishlistUserContext, command);
            }
        }

        private async Task AuthorizeByListIdsAsync(IResolveFieldContext context, AddWishlistBulkItemCommand command, IList<string> listIds)
        {
            var currentUserId = context.GetCurrentUserId();
            var currentOrganizationId = context.GetCurrentOrganizationId();
            var contact = await _memberResolver.ResolveMemberByIdAsync(currentUserId) as Contact;
            var carts = await _cartService.GetAsync(listIds);

            foreach (var cart in carts)
            {
                var wishlistUserContext = new WishlistUserContext
                {
                    CurrentUserId = currentUserId,
                    CurrentOrganizationId = currentOrganizationId,
                    CurrentContact = contact,
                    Cart = cart,
                };
                InitializeWishlistUserContextScope(wishlistUserContext);

                var addItemCommand = new AddWishlistItemCommand(cart.Id, command.ProductId)
                {
                    Quantity = command.Quantity,
                };
                await AuthorizeByListAsync(context, wishlistUserContext, addItemCommand);
            }
        }

        private async Task AuthorizeByListAsync(IResolveFieldContext context, WishlistUserContext wishlistUserContext, WishlistCommand command = null)
        {
            object resource = wishlistUserContext;
            if (command != null)
            {
                command.WishlistUserContext = wishlistUserContext;
                resource = command;
            }
            await AuthorizeAsync(context, resource);
        }

        private async Task<WishlistUserContext> InitializeWishlistUserContext(IResolveFieldContext context, string listId = null, ShoppingCart cart = null, string userId = null, string scope = null)
        {
            var currentUserId = context.GetCurrentUserId();

            if (cart is null && !string.IsNullOrEmpty(listId))
            {
                cart = await _cartService.GetByIdAsync(listId);
            }

            var wishlistUserContext = new WishlistUserContext
            {
                CurrentUserId = currentUserId,
                CurrentOrganizationId = context.GetCurrentOrganizationId(),
                CurrentContact = await _memberResolver.ResolveMemberByIdAsync(currentUserId) as Contact,
                Cart = cart,
                UserId = userId,
            };
            InitializeWishlistUserContextScope(wishlistUserContext, scope);

            return wishlistUserContext;
        }

        private static void InitializeWishlistUserContextScope(WishlistUserContext context, string scope = null)
        {
            if (string.IsNullOrEmpty(scope) && context.Cart is not null)
            {
                scope = string.IsNullOrEmpty(context.Cart.OrganizationId)
                    ? PrivateScope
                    : OrganizationScope;
            }

            context.Scope = scope;
        }

        private async Task AuthorizeAsync(IResolveFieldContext context, object resource)
        {
            await _userManagerCore.CheckCurrentUserState(context, allowAnonymous: true);
            var authorizationResult = await _authorizationService.AuthorizeAsync(context.GetCurrentPrincipal(), resource, new CanAccessCartAuthorizationRequirement());

            if (!authorizationResult.Succeeded)
            {
                throw context.IsAuthenticated()
                    ? AuthorizationError.Forbidden()
                    : AuthorizationError.AnonymousAccessDenied();
            }
        }
    }
}
