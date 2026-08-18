using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.XCart.Core;
using VirtoCommerce.XCart.Core.Models;
using VirtoCommerce.XCart.Core.Services;
using CartAddress = VirtoCommerce.CartModule.Core.Model.Address;
using TaxAddress = VirtoCommerce.TaxModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Data.Services;

public class XCartMapper : IXCartMapper
{
    public virtual TaxAddress ToTaxAddress(CartAddress source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<TaxAddress>.TryCreateInstance();

        result.AddressType = source.AddressType;
        result.Key = source.Key;
        result.Name = source.Name;
        result.Organization = source.Organization;
        result.CountryCode = source.CountryCode;
        result.CountryName = source.CountryName;
        result.City = source.City;
        result.PostalCode = source.PostalCode;
        result.Zip = source.Zip;
        result.Line1 = source.Line1;
        result.Line2 = source.Line2;
        result.RegionId = source.RegionId;
        result.RegionName = source.RegionName;
        result.FirstName = source.FirstName;
        result.MiddleName = source.MiddleName;
        result.LastName = source.LastName;
        result.Phone = source.Phone;
        result.Email = source.Email;
        result.OuterId = source.OuterId;
        result.IsDefault = source.IsDefault;
        result.Description = source.Description;

        return result;
    }

    public virtual GiftItem ToGiftItem(GiftReward source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<GiftItem>.TryCreateInstance();

        result.Id = source.Id;
        result.IsValid = source.IsValid;
        result.Description = source.Description;
        result.CouponAmount = source.CouponAmount;
        result.Coupon = source.Coupon;
        result.CouponMinOrderAmount = source.CouponMinOrderAmount;
        result.PromotionId = source.PromotionId;
        result.Promotion = source.Promotion;
        result.RewardType = source.RewardType;
        result.Name = source.Name;
        result.CategoryId = source.CategoryId;
        result.ProductId = source.ProductId;
        result.Quantity = source.Quantity;
        result.MeasureUnit = source.MeasureUnit;
        result.ImageUrl = source.ImageUrl;

        return result;
    }

    public virtual GiftLineItem ToGiftLineItem(GiftItem source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<GiftLineItem>.TryCreateInstance();

        result.Id = source.Id;
        result.Name = source.Name;
        result.CategoryId = source.CategoryId;
        result.ProductId = source.ProductId;
        result.Quantity = source.Quantity;
        result.MeasureUnit = source.MeasureUnit;
        result.ImageUrl = source.ImageUrl;
        result.CatalogId = source.CatalogId;
        result.Sku = source.Sku;

        return result;
    }

    public virtual LineItem ToLineItem(CartProduct source, CartMappingContext context)
    {
        if (source == null)
        {
            return null;
        }

        var lineItem = context?.Builder?.Create(source) ?? AbstractTypeFactory<LineItem>.TryCreateInstance();

        lineItem.Name = source.GetName(context?.CultureName);
        lineItem.Currency = context?.CurrencyCode;

        lineItem.CatalogId = source.Product.CatalogId;
        lineItem.CategoryId = source.Product.CategoryId;
        lineItem.DynamicProperties = [];

        if (source.Price != null)
        {
            lineItem.Currency = source.Price.Currency.Code;
            lineItem.DiscountAmount = source.Price.DiscountAmount.InternalAmount;
            lineItem.PriceId = source.Price.PricelistId;
            lineItem.SalePrice = source.Price.SalePrice.InternalAmount;
            lineItem.TaxDetails = source.Price.TaxDetails;
            lineItem.TaxPercentRate = source.Price.TaxPercentRate;
            lineItem.Discounts = source.Price.Discounts;
            lineItem.ListPrice = source.Price.ListPrice.InternalAmount;
        }

        lineItem.Height = source.Product.Height;
        lineItem.ImageUrl = source.Product.ImgSrc;
        lineItem.Length = source.Product.Length;
        lineItem.MeasureUnit = source.Product.MeasureUnit;
        lineItem.ProductOuterId = source.Product.OuterId;
        lineItem.ProductId = source.Product.Id;
        lineItem.ProductType = source.Product.ProductType;
        lineItem.Sku = source.Product.Code;
        lineItem.TaxType = source.Product.TaxType;
        lineItem.Weight = source.Product.Weight;
        lineItem.WeightUnit = source.Product.WeightUnit;
        lineItem.Width = source.Product.Width;
        lineItem.FulfillmentCenterId = source.Inventory?.FulfillmentCenterId;
        lineItem.FulfillmentCenterName = source.Inventory?.FulfillmentCenterName;
        lineItem.VendorId = source.Product.Vendor;

        return lineItem;
    }

    public virtual IEnumerable<TaxLine> ToTaxLines(ShippingRate source)
    {
        if (source == null)
        {
            return null;
        }

        var taxLine = AbstractTypeFactory<TaxLine>.TryCreateInstance();
        taxLine.Id = string.Join("&", source.ShippingMethod.Code, source.OptionName);
        taxLine.Code = source.ShippingMethod.Code;
        taxLine.TaxType = source.ShippingMethod.TaxType;
        taxLine.Amount = source.DiscountAmount > 0 ? source.DiscountAmount : source.Rate;

        return [taxLine];
    }

    public virtual IEnumerable<TaxLine> ToTaxLines(PaymentMethod source)
    {
        if (source == null)
        {
            return null;
        }

        var taxLine = AbstractTypeFactory<TaxLine>.TryCreateInstance();
        taxLine.Id = source.Code;
        taxLine.Code = source.Code;
        taxLine.TaxType = source.TaxType;
        taxLine.Amount = source.Total > 0 ? source.Total : source.Price;

        return [taxLine];
    }

    public virtual ProductPromoEntry ToProductPromoEntry(LineItem source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<ProductPromoEntry>.TryCreateInstance();

        result.CatalogId = source.CatalogId;
        result.CategoryId = source.CategoryId;
        result.Code = source.Sku;
        result.Discount = source.DiscountTotal;
        result.Price = source.SalePrice;
        result.ProductId = source.ProductId;
        result.Quantity = source.Quantity;

        return result;
    }

    public virtual PriceEvaluationContext ToPriceEvaluationContext(CartAggregate source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<PriceEvaluationContext>.TryCreateInstance();

        result.Language = source.Cart.LanguageCode;
        result.StoreId = source.Cart.StoreId;
        result.CatalogId = source.Store.Catalog;
        result.Currency = source.Cart.Currency;
        result.OrganizationId = source.Cart.OrganizationId;

        var contact = source.Member;
        if (contact != null)
        {
            result.CustomerId = contact.Id;

            var address = contact.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Shipping)
                       ?? contact.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Billing);

            if (address != null)
            {
                result.GeoCity = address.City;
                result.GeoCountry = address.CountryCode;
                result.GeoState = address.RegionName;
                result.GeoZipCode = address.PostalCode;
            }

            if (contact.Groups != null)
            {
                result.UserGroups = contact.Groups.ToArray();
            }
        }

        return result;
    }

    public virtual PriceEvaluationContext ToPriceEvaluationContext(CartProductsRequest source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<PriceEvaluationContext>.TryCreateInstance();

        result.Language = source.CultureName;
        result.StoreId = source.Store.Id;
        result.CatalogId = source.Store.Catalog;
        result.Currency = source.Currency.Code;

        var contact = source.Member;
        if (contact != null)
        {
            result.CustomerId = contact.Id;

            var address = contact.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Shipping)
                       ?? contact.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Billing);

            if (address != null)
            {
                result.GeoCity = address.City;
                result.GeoCountry = address.CountryCode;
                result.GeoState = address.RegionName;
                result.GeoZipCode = address.PostalCode;
            }

            if (contact.Groups != null)
            {
                result.UserGroups = contact.Groups.ToArray();
            }
        }

        return result;
    }

    public virtual TaxEvaluationContext ToTaxEvaluationContext(CartAggregate source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<TaxEvaluationContext>.TryCreateInstance();
        source.MapTo(result, this);

        return result;
    }
}
