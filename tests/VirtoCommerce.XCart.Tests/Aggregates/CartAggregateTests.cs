using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers;
using VirtoCommerce.XCart.Tests.Helpers.Stubs;
using Xunit;
using Address = VirtoCommerce.CartModule.Core.Model.Address;
using AddressType = VirtoCommerce.CoreModule.Core.Common.AddressType;

namespace VirtoCommerce.XCart.Tests.Aggregates
{
    public class CartAggregateTests : XCartMoqHelper
    {
        private readonly CartAggregate aggregate;

        public CartAggregateTests()
        {
            aggregate = new CartAggregate(
                _marketingPromoEvaluatorMock.Object,
                _shoppingCartTotalsCalculatorMock.Object,
                _taxProviderSearchServiceMock.Object,
                _cartProductServiceMock.Object,
                _dynamicPropertyUpdaterService.Object,
                _mapperMock.Object,
                _memberService.Object,
                _genericPipelineLauncherMock.Object,
                _configurationItemValidatorMock.Object,
                _fileUploadService.Object);

            var cart = GetCart();
            var member = GetMember();
            var store = GetStore();
            var currency = GetCurrency();

            aggregate.GrabCart(cart, store, member, currency);

            aggregate.RecalculateAsync().GetAwaiter().GetResult();
        }

        #region UpdateCartComment

        [Fact]
        public async Task UpdateCartComment_ShouldSaveCommentToAggregate()
        {
            // Arrange
            var comment = _fixture.Create<string>();

            // Act
            await aggregate.UpdateCartComment(comment);

            // Assert
            aggregate.Cart.Should().NotBeNull();
            aggregate.Cart.Comment.Should().Be(comment);
        }

        #endregion UpdateCartComment

        #region AddItemAsync

        [Fact]
        public async Task AddItemAsync_ShouldThrowArgumentNullException_IfNewCartItemIsNull()
        {
            // Arrange
            NewCartItem newCartItem = null;

            // Act
            var action = async () => await aggregate.AddItemAsync(newCartItem);

            // Assert
            await action.Should().ThrowExactlyAsync<ArgumentNullException>("NewCartItem is null");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task AddItemAsync_ShouldThrow_IfQuantityLessOrEqualZero(int quantity)
        {
            // Arrange
            var productId = _fixture.Create<string>();
            var newCartItem = new NewCartItem(productId, quantity);

            var shoppingCart = _fixture.Create<ShoppingCart>();
            shoppingCart.Items = Enumerable.Empty<LineItem>().ToList();

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), new[] { productId }))
                .ReturnsAsync(new List<CartProduct>() { new CartProduct(new CatalogProduct()) });

            // Act
            var aggregateAfterAddItem = await aggregate.AddItemAsync(newCartItem);

            // Assert
            aggregateAfterAddItem.GetValidationErrors().Should().NotBeEmpty();
            aggregateAfterAddItem.GetValidationErrors().Should().Contain(x => x.ErrorCode == "GreaterThanValidator");
            aggregateAfterAddItem.GetValidationErrors().Should().Contain(x => x.ErrorCode == "NotNullValidator");
        }

        #endregion AddItemAsync

        #region AddItemsAsync

        [Fact]
        public async Task AddItemsAsync_ItemsExist_ShouldAddNewItems()
        {
            // Arrange
            var productId1 = _fixture.Create<string>();
            var newCartItem1 = new NewCartItem(productId1, 1);

            var productId2 = _fixture.Create<string>();
            var newCartItem2 = new NewCartItem(productId2, 2);

            var newCartItems = new List<NewCartItem>() { newCartItem1, newCartItem2 };

            _cartProductServiceMock
                .Setup(x => x.GetCartProductsByIdsAsync(It.IsAny<CartAggregate>(), new[] { productId1, productId2 }))
                .ReturnsAsync(
                    new List<CartProduct>()
                    {
                        new CartProduct(new CatalogProduct() { Id = productId1, IsActive = true, IsBuyable = true }),
                        new CartProduct(new CatalogProduct() { Id = productId2, IsActive = true, IsBuyable = true }),
                    });


            _mapperMock
                .Setup(m => m.Map(It.IsAny<CartProduct>(), It.IsAny<Action<IMappingOperationOptions<object, LineItem>>>()))
                .Returns<CartProduct, Action<IMappingOperationOptions<object, LineItem>>>((cartProduct, options) =>
                {
                    return new LineItem()
                    {
                        ProductId = cartProduct.Id
                    };
                });

            var cartAggregate = GetValidCartAggregate();
            cartAggregate.ValidationRuleSet = new string[] { "default" };
            cartAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();

            // Act
            var newAggregate = await cartAggregate.AddItemsAsync(newCartItems);

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.ProductId == newCartItem1.ProductId);
            cartAggregate.Cart.Items.Should().Contain(x => x.ProductId == newCartItem2.ProductId);
        }

        #endregion AddItemsAsync

        #region ChangeItemPriceAsync

        [Fact]
        public async Task ChangeItemPriceAsync_LineItemNotFound_ShouldNotChangeItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            await cartAggregate.ChangeItemPriceAsync(new PriceAdjustment
            {
                LineItemId = _fixture.Create<string>(),
                LineItem = _fixture.Create<LineItem>(),
                NewPrice = _fixture.Create<decimal>()
            });

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.ListPrice == lineItem.ListPrice && x.SalePrice == lineItem.SalePrice);
        }

        [Fact]
        public async Task ChangeItemPriceAsync_LineItemFound_ShouldChangeItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };
            var newPrice = _fixture.Create<decimal>();

            // Act
            await cartAggregate.ChangeItemPriceAsync(new PriceAdjustment
            {
                LineItem = lineItem,
                LineItemId = lineItem.Id,
                NewPrice = newPrice
            });

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.ListPrice == newPrice && x.SalePrice == newPrice);
        }

        #endregion ChangeItemPriceAsync

        #region ChangeItemQuantityAsync

        [Fact]
        public async Task ChangeItemQuantityAsync_LineItemNotFound_ShouldThrowValidationException()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            var cartAggregateAfterChangeItemQty = await cartAggregate.ChangeItemQuantityAsync(new ItemQtyAdjustment
            {
                LineItemId = _fixture.Create<string>(),
                NewQuantity = 5,
                CartProduct = _fixture.Create<CartProduct>()
            });

            // Assert
            cartAggregateAfterChangeItemQty.GetValidationErrors().Should().NotBeEmpty();
            cartAggregateAfterChangeItemQty.GetValidationErrors().Should().Contain(x => x.ErrorCode == "LINE_ITEM_NOT_FOUND");
        }

        #endregion ChangeItemQuantityAsync

        #region ChangeItemCommentAsync

        [Fact]
        public async Task ChangeItemCommentAsync_LineItemNotFound_ShouldNotChangeItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            await cartAggregate.ChangeItemCommentAsync(new NewItemComment(
                _fixture.Create<string>(),
                _fixture.Create<string>()
            ));

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.Note == lineItem.Note && x.Id == lineItem.Id);
        }

        [Fact]
        public async Task ChangeItemCommentAsync_LineItemFound_ShouldChangeItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };
            var newComment = _fixture.Create<string>();

            // Act
            await cartAggregate.ChangeItemCommentAsync(new NewItemComment(lineItem.Id, newComment));

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.Note == newComment && x.Id == lineItem.Id);
        }

        #endregion ChangeItemCommentAsync

        #region RemoveItemAsync

        [Fact]
        public async Task RemoveItemAsync_LineItemNotFound_ShouldNotRemoveItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            await cartAggregate.RemoveItemAsync(_fixture.Create<string>());

            // Assert
            cartAggregate.Cart.Items.Should().Contain(x => x.Id == lineItem.Id);
        }

        [Fact]
        public async Task RemoveItemAsync_LineItemFound_ShouldRemoveItem()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            await cartAggregate.RemoveItemAsync(lineItem.Id);

            // Assert
            cartAggregate.Cart.Items.Should().NotContain(x => x.Id == lineItem.Id);
        }

        #endregion RemoveItemAsync

        #region AddCouponAsync

        [Fact]
        public async Task AddCouponAsync_CouponNotFound_ShouldAddCoupon()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var coupon = _fixture.Create<string>();
            cartAggregate.Cart.Coupons = new List<string> { coupon };
            var newCoupon = _fixture.Create<string>();

            // Act
            await cartAggregate.AddCouponAsync(newCoupon);

            // Assert
            cartAggregate.Cart.Coupons.Should().Contain(newCoupon);
            cartAggregate.Cart.Coupons.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddCouponAsync_CouponFound_ShouldContainOnlyOneCouponWithSameCode()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var coupon = _fixture.Create<string>();
            cartAggregate.Cart.Coupons = new List<string> { coupon };

            // Act
            await cartAggregate.AddCouponAsync(coupon);

            // Assert
            cartAggregate.Cart.Coupons.Should().ContainSingle(coupon);
        }

        #endregion AddCouponAsync

        #region RemoveCouponAsync

        [Fact]
        public async Task RemoveCouponAsync_CouponFound_ShouldRemoveCoupon()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var firstCoupon = _fixture.Create<string>();
            var secondCoupon = _fixture.Create<string>();

            cartAggregate.Cart.Coupons = new List<string>
            {
                firstCoupon,
                secondCoupon,
            };

            // Act
            await cartAggregate.RemoveCouponAsync(firstCoupon);

            // Assert
            cartAggregate.Cart.Coupons.Should().ContainSingle(secondCoupon);
        }

        [Fact]
        public async Task RemoveCouponAsync_CouponNotFound_ShouldPass()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var firstCoupon = _fixture.Create<string>();
            var notExistingCoupon = _fixture.Create<string>();

            cartAggregate.Cart.Coupons = new List<string> { firstCoupon };

            // Act
            await cartAggregate.RemoveCouponAsync(notExistingCoupon);

            // Assert
            cartAggregate.Cart.Coupons.Should().ContainSingle(firstCoupon);
        }

        [Fact]
        public async Task RemoveCouponAsync_Null_ShouldRemoveAllCoupons()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var firstCoupon = _fixture.Create<string>();
            var secondCoupon = _fixture.Create<string>();

            cartAggregate.Cart.Coupons = new List<string>
            {
                firstCoupon,
                secondCoupon,
            };

            // Act
            await cartAggregate.RemoveCouponAsync(null);

            // Assert
            cartAggregate.Cart.Coupons.Should().BeEmpty();
        }

        #endregion RemoveCouponAsync

        #region ClearAsync

        [Fact]
        public async Task ClearAsync_ShouldClearCartItems()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var lineItem = _fixture.Create<LineItem>();
            cartAggregate.Cart.Items = new List<LineItem> { lineItem };

            // Act
            await cartAggregate.ClearAsync();

            // Assert
            cartAggregate.Cart.Items.Should().HaveCount(0);
        }

        #endregion ClearAsync

        #region AddShipmentAsync

        [Fact]
        public async Task AddShipmentAsync_ShipmentFound_MustContainSameShipment()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var shipment = new Shipment
            {
                ShipmentMethodCode = "shippingMethodCode",
                ShipmentMethodOption = "OptionName",
                Price = 777,
            };
            var shippingRate = new ShippingRate
            {
                OptionName = "OptionName",
                ShippingMethod = new StubShippingMethod("shippingMethodCode"),
                Rate = 777,
            };
            var shippingRates = new List<ShippingRate> { shippingRate };
            cartAggregate.Cart.Shipments = new List<Shipment> { shipment };

            // Act
            await cartAggregate.AddShipmentAsync(shipment, shippingRates);

            // Assert
            cartAggregate.Cart.Shipments.Should().Contain(shipment);
        }

        #endregion AddShipmentAsync

        #region RemoveShipmentAsync

        [Fact]
        public async Task RemoveShipmentAsync_ShipmentFound_ShouldRemoveShipment()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var fistShipment = new Shipment
            {
                Id = _fixture.Create<string>(),
            };
            var secondShipment = new Shipment
            {
                Id = _fixture.Create<string>(),
            };
            cartAggregate.Cart.Shipments = new List<Shipment>
            {
                fistShipment,
                secondShipment,
            };

            // Act
            await cartAggregate.RemoveShipmentAsync(fistShipment.Id);

            // Assert
            cartAggregate.Cart.Shipments.Should().ContainSingle(x => x.Id == secondShipment.Id);
        }

        #endregion RemoveShipmentAsync

        #region AddPaymentAsync

        [Fact]
        public async Task AddPaymentAsync_PaymentFound_MustContainSamePayment()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var payment = _fixture.Create<Payment>();
            payment.PaymentGatewayCode = null;
            var paymentMethod = _fixture.Create<PaymentMethod>();
            var paymentMethods = new List<PaymentMethod> { paymentMethod };
            cartAggregate.Cart.Payments = new List<Payment> { payment };

            // Act
            await cartAggregate.AddPaymentAsync(payment, paymentMethods);

            // Assert
            cartAggregate.Cart.Payments.Should().Contain(payment);
        }

        #endregion AddPaymentAsync

        #region MergeWithCartAsync

        [Fact]
        public async Task MergeWithCartAsync_CartsHaveItems_ItemsMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Coupons = Enumerable.Empty<string>().ToList();
            sourceAggregate.Cart.Shipments = Enumerable.Empty<Shipment>().ToList();
            sourceAggregate.Cart.Payments = Enumerable.Empty<Payment>().ToList();

            var sourceProduct1 = _fixture.Create<CartProduct>();
            var sourceProduct2 = _fixture.Create<CartProduct>();

            sourceProduct1.Id = "source1";
            sourceProduct2.Id = "source2";

            sourceAggregate.CartProducts.Add(sourceProduct1.Id, sourceProduct1);
            sourceAggregate.CartProducts.Add(sourceProduct2.Id, sourceProduct2);

            var sourceLineItem1 = _fixture.Create<LineItem>();
            sourceLineItem1.ProductId = sourceProduct1.Id;

            var sourceLineItem2 = _fixture.Create<LineItem>();
            sourceLineItem2.ProductId = sourceProduct2.Id;

            sourceAggregate.Cart.Items = new List<LineItem> { sourceLineItem1, sourceLineItem2 };

            var destinationAggregate = GetValidCartAggregate();

            var destinationProduct1 = _fixture.Create<CartProduct>();
            destinationProduct1.Id = "dest1";

            var destinationLineItem1 = _fixture.Create<LineItem>();
            destinationLineItem1.ProductId = destinationProduct1.Id;

            var destinationLineItem2 = _fixture.Create<LineItem>();
            destinationLineItem2.ProductId = sourceProduct2.Id;
            var quantity = destinationLineItem2.Quantity;

            destinationAggregate.Cart.Items = new List<LineItem> { destinationLineItem1, destinationLineItem2 };

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Items.Should().HaveCount(3);
            destinationAggregate.Cart.Items.Should().Contain(x => x.ProductId == sourceLineItem1.ProductId && x.Quantity == sourceLineItem1.Quantity);
            destinationAggregate.Cart.Items.Should().Contain(x => x.ProductId == destinationLineItem1.ProductId && x.Quantity == destinationLineItem1.Quantity);
            destinationAggregate.Cart.Items.Should().Contain(x => x.ProductId == destinationLineItem2.ProductId && x.Quantity == sourceLineItem2.Quantity + quantity);
        }

        [Fact]
        public async Task MergeWithCartAsync_CartsHaveCoupons_CouponsMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();
            sourceAggregate.Cart.Shipments = Enumerable.Empty<Shipment>().ToList();
            sourceAggregate.Cart.Payments = Enumerable.Empty<Payment>().ToList();

            var sourceCoupon1 = _fixture.Create<string>();
            var sourceCoupon2 = _fixture.Create<string>();

            sourceAggregate.Cart.Coupons = new List<string> { sourceCoupon1, sourceCoupon2 };

            var destinationAggregate = GetValidCartAggregate();
            var destinationCoupon1 = _fixture.Create<string>();

            destinationAggregate.Cart.Coupons = new List<string> { destinationCoupon1, sourceCoupon2 };

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Coupons.Should().HaveCount(3);
            destinationAggregate.Cart.Coupons.Should().Contain(sourceCoupon1);
            destinationAggregate.Cart.Coupons.Should().Contain(sourceCoupon2);
            destinationAggregate.Cart.Coupons.Should().Contain(destinationCoupon1);
        }

        [Fact]
        public async Task MergeWithCartAsync_TargetCartHasShipments_ShipmentNotMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();
            sourceAggregate.Cart.Coupons = Enumerable.Empty<string>().ToList();
            sourceAggregate.Cart.Payments = Enumerable.Empty<Payment>().ToList();

            var sourceShipment = _fixture.Create<Shipment>();

            sourceAggregate.Cart.Shipments = new List<Shipment> { sourceShipment };

            var destinationAggregate = GetValidCartAggregate();
            var destinationShipment = _fixture.Create<Shipment>();

            destinationAggregate.Cart.Shipments = new List<Shipment> { destinationShipment };

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Shipments.Should().HaveCount(1);
            destinationAggregate.Cart.Shipments.Should().Contain(destinationShipment);
        }

        [Fact]
        public async Task MergeWithCartAsync_TargetCartDoesntHaveShipments_ShipmentMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();
            sourceAggregate.Cart.Coupons = Enumerable.Empty<string>().ToList();
            sourceAggregate.Cart.Payments = Enumerable.Empty<Payment>().ToList();

            var sourceShipment = _fixture.Create<Shipment>();

            sourceAggregate.Cart.Shipments = new List<Shipment> { sourceShipment };

            var destinationAggregate = GetValidCartAggregate();

            destinationAggregate.Cart.Shipments = new List<Shipment>();

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Shipments.Should().HaveCount(1);
            destinationAggregate.Cart.Shipments.Should().Contain(sourceShipment);
        }

        [Fact]
        public async Task MergeWithCartAsync_TargetCartHasPayments_PaymentNotMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();
            sourceAggregate.Cart.Coupons = Enumerable.Empty<string>().ToList();
            sourceAggregate.Cart.Shipments = Enumerable.Empty<Shipment>().ToList();

            var sourcePayment = _fixture.Create<Payment>();

            sourceAggregate.Cart.Payments = new List<Payment> { sourcePayment };

            var destinationAggregate = GetValidCartAggregate();
            var destinationPayments = _fixture.Create<Payment>();

            destinationAggregate.Cart.Payments = new List<Payment> { destinationPayments };

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Payments.Should().HaveCount(1);
            destinationAggregate.Cart.Payments.Should().Contain(destinationPayments);
        }

        [Fact]
        public async Task MergeWithCartAsync_TargetCartDoesntHavePayments_PaymentMerged()
        {
            // Arrange
            var sourceAggregate = GetValidCartAggregate();
            sourceAggregate.Cart.Items = Enumerable.Empty<LineItem>().ToList();
            sourceAggregate.Cart.Coupons = Enumerable.Empty<string>().ToList();
            sourceAggregate.Cart.Shipments = Enumerable.Empty<Shipment>().ToList();

            var sourcePayment = _fixture.Create<Payment>();

            sourceAggregate.Cart.Payments = new List<Payment> { sourcePayment };

            var destinationAggregate = GetValidCartAggregate();

            destinationAggregate.Cart.Payments = new List<Payment>();

            // Act
            await destinationAggregate.MergeWithCartAsync(sourceAggregate);

            // Assert
            destinationAggregate.Cart.Payments.Should().HaveCount(1);
            destinationAggregate.Cart.Payments.Should().Contain(sourcePayment);
        }

        #endregion MergeWithCartAsync

        #region ValidateAsync

        [Fact]
        public async Task ValidateAsync_CartValid_CartValidated()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var context = new CartValidationContext();

            // Act
            await cartAggregate.ValidateAsync(context, "default");

            // Assert
            cartAggregate.IsValidated.Should().BeTrue();
        }

        #endregion ValidateAsync

        #region ValidateCouponAsync

        [Fact]
        public async Task ValidateCouponAsync_ValidCoupon_CouponValidated()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            cartAggregate.Cart.Items = new List<LineItem> { _fixture.Create<LineItem>() };

            var coupon = _fixture.Create<string>();
            var context = new PromotionEvaluationContext
            {
                Coupon = coupon,
            };

            var stub = new PromotionResult();
            stub.Rewards.Add(new StubPromotionReward
            {
                Coupon = coupon,
                IsValid = true
            });

            _mapperMock.Setup(x => x.Map<PromotionEvaluationContext>(It.Is<CartAggregate>(x => x == cartAggregate)))
                .Returns(context);

            _marketingPromoEvaluatorMock
               .Setup(x => x.EvaluatePromotionAsync(It.Is<PromotionEvaluationContext>(x => x.Coupon == coupon)))
               .ReturnsAsync(stub);

            _genericPipelineLauncherMock.Setup(x => x.Execute(It.IsAny<PromotionEvaluationContextCartMap>()))
                .Callback<PromotionEvaluationContextCartMap>(x =>
                {
                    x.PromotionEvaluationContext = _mapperMock.Object.Map<PromotionEvaluationContext>(cartAggregate);
                });

            // Act
            var result = await cartAggregate.ValidateCouponAsync(coupon);

            // Assert
            result.Should().BeTrue();
        }

        #endregion ValidateCouponAsync

        #region EvaluatePromotionsAsync(PromotionEvaluationContext evalContext)

        [Fact]
        public async Task EvaluatePromotionsAsync_HasCart_PromotionEvaluated()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            cartAggregate.Cart.Items = new List<LineItem> { _fixture.Create<LineItem>() };

            var context = new PromotionEvaluationContext();

            var promoResult = new PromotionResult();
            var promoReward = new StubPromotionReward
            {
                Id = _fixture.Create<string>(),
                IsValid = true
            };
            promoResult.Rewards.Add(promoReward);

            _mapperMock.Setup(x => x.Map<PromotionEvaluationContext>(It.Is<CartAggregate>(x => x == cartAggregate)))
                .Returns(context);

            _marketingPromoEvaluatorMock
               .Setup(x => x.EvaluatePromotionAsync(It.Is<PromotionEvaluationContext>(x => x == context)))
               .ReturnsAsync(promoResult);

            _genericPipelineLauncherMock.Setup(x => x.Execute(It.IsAny<PromotionEvaluationContextCartMap>()))
                .Callback<PromotionEvaluationContextCartMap>(x =>
                {
                    x.PromotionEvaluationContext = _mapperMock.Object.Map<PromotionEvaluationContext>(cartAggregate);
                });

            // Act
            var result = await cartAggregate.EvaluatePromotionsAsync();

            // Assert
            result.Rewards.Should().ContainSingle(x => x.Id == promoReward.Id);
        }

        #endregion EvaluatePromotionsAsync(PromotionEvaluationContext evalContext)

        #region RecalculateAsync

        [Fact]
        public async Task RecalculateAsync_HasPromoRewards_CalculateTotalsCalled()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            cartAggregate.Cart.Items = new List<LineItem> { _fixture.Create<LineItem>() };

            var context = new PromotionEvaluationContext();

            var promoResult = new PromotionResult();
            var promoReward = new StubPromotionReward
            {
                Id = _fixture.Create<string>(),
                IsValid = true
            };
            promoResult.Rewards.Add(promoReward);

            _mapperMock.Setup(x => x.Map<PromotionEvaluationContext>(It.Is<CartAggregate>(x => x == cartAggregate)))
                .Returns(context);

            _marketingPromoEvaluatorMock
               .Setup(x => x.EvaluatePromotionAsync(It.Is<PromotionEvaluationContext>(x => x == context)))
               .ReturnsAsync(promoResult);

            // Act
            var result = await cartAggregate.RecalculateAsync();

            // Assert
            _shoppingCartTotalsCalculatorMock.Verify(x => x.CalculateTotals(It.Is<ShoppingCart>(x => x == cartAggregate.Cart)), Times.Exactly(2));
        }

        public static IEnumerable<object[]> Data =>
        [
            //                                                                         Expected        Expected  Expected       Expected
            // MidpointRounding,             ListPrice, Quantity, RewardAmount, Round, DiscountAmount, SubTotal, DiscountTotal, Total
            [MidpointRounding.AwayFromZero, 49.95m, 10, 10m, false, 4.995m, 499.50m, 49.95m, 449.55m],
            [MidpointRounding.ToZero, 49.95m, 10, 10m, false, 4.995m, 499.50m, 49.95m, 449.55m],
            [MidpointRounding.AwayFromZero, 49.95m, 10, 10m, true, 5.000m, 499.50m, 50.00m, 449.50m],
            [MidpointRounding.ToZero, 49.95m, 10, 10m, true, 4.990m, 499.50m, 49.90m, 449.60m],

            [MidpointRounding.AwayFromZero, 0.01m, 10, 10m, false, 0.001m, 0.10m, 0.01m, 0.09m],
            [MidpointRounding.ToZero, 0.01m, 10, 10m, false, 0.001m, 0.10m, 0.01m, 0.09m],
            [MidpointRounding.AwayFromZero, 0.01m, 10, 10m, true, 0.000m, 0.10m, 0.00m, 0.10m],
            [MidpointRounding.ToZero, 0.01m, 10, 10m, true, 0.000m, 0.10m, 0.00m, 0.10m],

            [MidpointRounding.AwayFromZero, 7.50m, 1, 3m, false, 0.225m, 7.50m, 0.23m, 7.27m],
            [MidpointRounding.ToZero, 7.50m, 1, 3m, false, 0.225m, 7.50m, 0.22m, 7.28m],
            [MidpointRounding.AwayFromZero, 7.50m, 1, 3m, true, 0.230m, 7.50m, 0.23m, 7.27m],
            [MidpointRounding.ToZero, 7.50m, 1, 3m, true, 0.220m, 7.50m, 0.22m, 7.28m],

            [MidpointRounding.AwayFromZero, 422.50m, 1, 45m, false, 190.125m, 422.50m, 190.13m, 232.37m],
            [MidpointRounding.ToZero, 422.50m, 1, 45m, false, 190.125m, 422.50m, 190.12m, 232.38m],
            [MidpointRounding.AwayFromZero, 422.50m, 10, 45m, false, 190.125m, 4225.00m, 1901.25m, 2323.75m],
            [MidpointRounding.ToZero, 422.50m, 10, 45m, false, 190.125m, 4225.00m, 1901.25m, 2323.75m],
            [MidpointRounding.AwayFromZero, 422.50m, 1, 45m, true, 190.130m, 422.50m, 190.13m, 232.37m],
            [MidpointRounding.ToZero, 422.50m, 1, 45m, true, 190.120m, 422.50m, 190.12m, 232.38m],
            [MidpointRounding.AwayFromZero, 422.50m, 10, 45m, true, 190.130m, 4225.00m, 1901.30m, 2323.70m],
            [MidpointRounding.ToZero, 422.50m, 10, 45m, true, 190.120m, 4225.00m, 1901.20m, 2323.80m],
        ];

        [Theory]
        [MemberData(nameof(Data))]
        public async Task RecalculateAsync_DiscountAppliedProperly(
            MidpointRounding midpointRounding,
            decimal listPrice,
            int quantity,
            decimal rewardAmount,
            bool roundRewardAmountPerItem,
            decimal expectedDiscountAmount,
            decimal expectedSubTotal,
            decimal expectedDiscountTotal,
            decimal expectedTotal)
        {
            // Arrange
            var currency = _fixture.Create<Currency>();
            currency.MidpointRounding = midpointRounding.ToString();

            var lineItem = new LineItem
            {
                Currency = currency.Code,
                ListPrice = listPrice,
                SalePrice = listPrice,
                Quantity = quantity,
            };

            var cart = new ShoppingCart
            {
                Currency = currency.Code,
                Items = new List<LineItem> { lineItem },
            };

            var cartAggregate = GetValidCartAggregate(cart, currency);

            var context = new PromotionEvaluationContext();

            var promotionResult = new PromotionResult();
            var reward = new CatalogItemAmountReward
            {
                Amount = rewardAmount,
                AmountType = RewardAmountType.Relative,
                RoundAmountPerItem = roundRewardAmountPerItem,
                IsValid = true,
            };
            promotionResult.Rewards.Add(reward);

            _mapperMock
                .Setup(x => x.Map<PromotionEvaluationContext>(It.IsAny<CartAggregate>()))
                .Returns(context);

            _marketingPromoEvaluatorMock
                .Setup(x => x.EvaluatePromotionAsync(It.IsAny<PromotionEvaluationContext>()))
                .ReturnsAsync(promotionResult);

            // Act
            await cartAggregate.RecalculateAsync();

            // Assert
            Assert.Equal(expectedSubTotal, cart.SubTotal);
            Assert.Equal(expectedDiscountTotal, cart.DiscountTotal);
            Assert.Equal(expectedTotal, cart.Total);

            Assert.Equal(expectedSubTotal, lineItem.ListTotal);
            Assert.Equal(expectedDiscountAmount, lineItem.DiscountAmount);
            Assert.Equal(expectedDiscountTotal, lineItem.DiscountTotal);
            Assert.Equal(expectedTotal, lineItem.ExtendedPrice);
            Assert.Equal(roundRewardAmountPerItem, lineItem.IsDiscountAmountRounded);
        }

        #endregion RecalculateAsync

        #region AddCartAddressAsync

        [Fact]
        public async Task AddOrUpdateCartAddressByTypeAsync_AddressExists_ShouldUpdateAddress()
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();
            var oldAddress = new Address
            {
                Name = "existing_address",
                AddressType = AddressType.BillingAndShipping,
            };
            cartAggregate.Cart.Addresses = new List<Address> { oldAddress };

            var newAddress = new Address
            {
                Name = "new_address",
                Key = "key",
                AddressType = AddressType.BillingAndShipping,
            };

            // Act
            await cartAggregate.AddOrUpdateCartAddressByTypeAsync(newAddress);

            // Assert
            newAddress.Key.Should().BeNull();
            cartAggregate.Cart.Addresses.Should().ContainSingle(x => x.Name.EqualsIgnoreCase(newAddress.Name)).And.NotContain(x => x.Name.EqualsIgnoreCase(oldAddress.Name));
        }

        [Theory]
        [InlineData(AddressType.Billing)]
        [InlineData(AddressType.Shipping)]
        [InlineData(AddressType.BillingAndShipping)]
        [InlineData(AddressType.Pickup)]
        public async Task AddOrUpdateCartAddressByTypeAsync_PaymentAndShipmentExist_ShouldNotUpdatePaymentAndShipmentAddresses(AddressType addressType)
        {
            // Arrange
            var cartAggregate = GetValidCartAggregate();

            var newAddress = new Address
            {
                Name = "new_address",
                AddressType = addressType,
            };

            // Act
            await cartAggregate.AddOrUpdateCartAddressByTypeAsync(newAddress);

            // Assert
            cartAggregate.Cart.Shipments.Select(x => x.DeliveryAddress).Should().NotContain(x => x.Name.EqualsIgnoreCase(newAddress.Name));
            cartAggregate.Cart.Payments.Select(x => x.BillingAddress).Should().NotContain(x => x.Name.EqualsIgnoreCase(newAddress.Name));
        }

        #endregion AddCartAddressAsync
    }
}
