using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Extensions
{
    public static class RewardExtensions
    {
        public static void ApplyRewards(this PaymentMethod paymentMethod, Currency currency, ICollection<PromotionReward> rewards)
            => paymentMethod.DiscountAmount = rewards
                .Where(r => r.IsValid)
                .OfType<PaymentReward>()
                .Where(r => r.PaymentMethod.IsNullOrEmpty() || r.PaymentMethod.EqualsIgnoreCase(paymentMethod.Code))
                .Sum(reward => reward.GetTotalAmount(paymentMethod.Price - paymentMethod.DiscountAmount, 1, currency));

        public static void ApplyRewards(this ShippingRate shippingRate, Currency currency, ICollection<PromotionReward> rewards)
            => shippingRate.DiscountAmount = rewards
                .Where(r => r.IsValid)
                .OfType<ShipmentReward>()
                .Where(r => r.ShippingMethod.IsNullOrEmpty() || shippingRate.ShippingMethod != null && r.ShippingMethod.EqualsIgnoreCase(shippingRate.ShippingMethod.Code))
                .Sum(reward => reward.GetTotalAmount(shippingRate.Rate, 1, currency));

        public static void ApplyRewards(this CartAggregate aggregate, ICollection<PromotionReward> rewards)
        {
            var shoppingCart = aggregate.Cart;

            shoppingCart.Discounts?.Clear();
            shoppingCart.DiscountAmount = 0M;

            // remove the (added) gifts, if corresponding valid reward is missing
            foreach (var lineItem in aggregate.GiftItems?.ToList() ?? Enumerable.Empty<LineItem>())
            {
                if (!rewards.OfType<GiftReward>().Any(re => re.IsValid && lineItem.EqualsReward(re)))
                {
                    shoppingCart.Items.Remove(lineItem);
                }
            }

            ApplyCartRewardsInternal(aggregate, rewards);
        }

        public static void ApplyRewards(this LineItem lineItem, Currency currency, IEnumerable<CatalogItemAmountReward> rewards)
        {
            var lineItemRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.ProductId.IsNullOrEmpty() || r.ProductId.EqualsIgnoreCase(lineItem.ProductId));

            lineItem.Discounts?.Clear();
            lineItem.DiscountAmount = Math.Max(0, lineItem.ListPrice - lineItem.SalePrice);
            lineItem.IsDiscountAmountRounded = true;

            if (lineItem.Quantity == 0)
            {
                return;
            }

            foreach (var reward in lineItemRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency.Code,
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                    Name = reward.Promotion?.Name,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetAmountPerItem(lineItem.ListPrice - lineItem.DiscountAmount, lineItem.Quantity, currency),
                };

                // Skip invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }

                lineItem.Discounts ??= new List<Discount>();
                lineItem.Discounts.Add(discount);
                lineItem.DiscountAmount += discount.DiscountAmount;
                lineItem.IsDiscountAmountRounded &= reward.RoundAmountPerItem;
            }
        }

        public static void ApplyRewards(this Shipment shipment, Currency currency, IEnumerable<ShipmentReward> rewards)
        {
            var shipmentRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.ShippingMethod.IsNullOrEmpty() || r.ShippingMethod.EqualsIgnoreCase(shipment.ShipmentMethodCode));

            shipment.Discounts?.Clear();
            shipment.DiscountAmount = 0M;

            foreach (var reward in shipmentRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency.Code,
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                    Name = reward.Promotion?.Name,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetTotalAmount(shipment.Price - shipment.DiscountAmount, 1, currency),
                };

                // Pass invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }
                shipment.Discounts ??= new List<Discount>();
                shipment.Discounts.Add(discount);
                shipment.DiscountAmount += discount.DiscountAmount;
            }
        }

        public static void ApplyRewards(this Payment payment, Currency currency, IEnumerable<PaymentReward> rewards)
        {
            var paymentRewards = rewards
                .Where(r => r.IsValid)
                .Where(r => r.PaymentMethod.IsNullOrEmpty() || r.PaymentMethod.EqualsIgnoreCase(payment.PaymentGatewayCode));

            payment.Discounts?.Clear();
            payment.DiscountAmount = 0M;

            foreach (var reward in paymentRewards)
            {
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = currency.Code,
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                    Name = reward.Promotion?.Name,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetTotalAmount(payment.Price - payment.DiscountAmount, 1, currency),
                };

                // Pass invalid discounts
                if (discount.DiscountAmount <= 0)
                {
                    continue;
                }
                payment.Discounts ??= new List<Discount>();
                payment.Discounts.Add(discount);
                payment.DiscountAmount += discount.DiscountAmount;
            }
        }

        public static async Task ApplyRewardsAsync(this CartAggregate aggregate, ICollection<PromotionReward> rewards)
        {
            var shoppingCart = aggregate.Cart;

            shoppingCart.Discounts?.Clear();
            shoppingCart.DiscountAmount = 0M;

            // remove the (added) gifts, if corresponding valid reward is missing
            foreach (var lineItem in aggregate.GiftItems?.ToList() ?? Enumerable.Empty<LineItem>())
            {
                if (!rewards.OfType<GiftReward>().Any(re => re.IsValid && lineItem.EqualsReward(re)))
                {
                    shoppingCart.Items.Remove(lineItem);
                }
            }

            // automatically add gift rewards to line items if the setting is enabled
            if (aggregate.IsSelectedForCheckout)
            {
                var availableGifts = (await aggregate.GetAvailableGiftsAsync(rewards)).ToList();

                if (availableGifts.Count > 0)
                {
                    var newGiftItemIds = availableGifts.Where(x => !x.HasLineItem).Select(x => x.Id).ToList();
                    await aggregate.AddGiftItemsAsync(newGiftItemIds, availableGifts); //add new items to cart
                }
            }

            ApplyCartRewardsInternal(aggregate, rewards);
        }

        private static void ApplyCartRewardsInternal(CartAggregate aggregate, ICollection<PromotionReward> rewards)
        {
            var shoppingCart = aggregate.Cart;

            var lineItemRewards = rewards.OfType<CatalogItemAmountReward>().ToList();
            foreach (var lineItem in aggregate.LineItems ?? [])
            {
                lineItem.ApplyRewards(aggregate.Currency, lineItemRewards);
            }

            var shipmentRewards = rewards.OfType<ShipmentReward>().ToList();
            foreach (var shipment in shoppingCart.Shipments ?? Enumerable.Empty<Shipment>())
            {
                shipment.ApplyRewards(aggregate.Currency, shipmentRewards);
            }

            var paymentRewards = rewards.OfType<PaymentReward>().ToList();
            foreach (var payment in shoppingCart.Payments ?? Enumerable.Empty<Payment>())
            {
                payment.ApplyRewards(aggregate.Currency, paymentRewards);
            }

            var subTotalExcludeDiscount = shoppingCart.Items.Where(li => li.SelectedForCheckout).Sum(li => (li.ListPrice - li.DiscountAmount) * li.Quantity);

            var cartRewards = rewards.OfType<CartSubtotalReward>();
            foreach (var reward in cartRewards.Where(reward => reward.IsValid))
            {
                //When a discount is applied to the cart subtotal, the tax calculation has already been applied, and is reflected in the tax subtotal.
                //Therefore, a discount applying to the cart subtotal will occur after tax.
                //For instance, if the cart subtotal is $100, and $15 is the tax subtotal, a cart - wide discount of 10 % will yield a total of $105($100 subtotal – $10 discount + $15 tax on the original $100).
                var discount = new Discount
                {
                    Coupon = reward.Coupon,
                    Currency = shoppingCart.Currency,
                    PromotionId = reward.PromotionId ?? reward.Promotion?.Id,
                    Name = reward.Promotion?.Name,
                    Description = reward.Promotion?.Description,
                    DiscountAmount = reward.GetTotalAmount(subTotalExcludeDiscount, 1, aggregate.Currency),
                };

                shoppingCart.Discounts ??= new List<Discount>();
                shoppingCart.Discounts.Add(discount);
                shoppingCart.DiscountAmount += discount.DiscountAmount;
            }
        }

        /// <summary>
        /// Return whether cart LineItem is equal to promotion Reward
        /// </summary>
        public static bool EqualsReward(this LineItem li, GiftReward reward)
        {
            return li.Quantity == reward.Quantity &&
                  (li.ProductId == reward.ProductId || li.ProductId.IsNullOrEmpty() && reward.ProductId.IsNullOrEmpty() &&
                  (li.Name == reward.Name || reward.Name.IsNullOrEmpty()) &&
                  (li.MeasureUnit == reward.MeasureUnit || reward.MeasureUnit.IsNullOrEmpty()) &&
                  (li.ImageUrl == reward.ImageUrl || reward.ImageUrl.IsNullOrEmpty())
                );
        }
    }
}
