using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ShippingModule.Core.Model;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers;
using VirtoCommerce.XCart.Tests.Helpers.Stubs;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Validators
{
    public class CartShipmentValidatorTests : XCartMoqHelper
    {
        private readonly ShippingRate _shippingRate;

        public CartShipmentValidatorTests()
        {
            _shippingRate = new ShippingRate
            {
                OptionName = ":)",
                ShippingMethod = new StubShippingMethod("shippingMethodCode"),
                Rate = 777,
            };

            _context.AvailShippingRates = new List<ShippingRate>()
            {
                _shippingRate
            };
        }


        [Fact]
        public async Task ValidateShipment_RuleSetDefault_ShipmentMethodCodeIsNull_Invalid()
        {
            // Arrange
            var shipment = new CartModule.Core.Model.Shipment
            {
                ShipmentMethodCode = null
            };

            // Act
            var validator = new CartShipmentValidator();
            var result = await validator.ValidateAsync(new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = _context.AvailShippingRates
            });

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().Contain(x => x.ErrorCode == "SHIPMENT_METHOD_CODE_REQUIRED");
        }

        [Fact]
        public async Task ValidateShipment_RuleSetDefault_ShipmentMethodCodeIsEmpty_Invalid()
        {
            // Arrange
            var shipment = new CartModule.Core.Model.Shipment
            {
                ShipmentMethodCode = string.Empty
            };

            // Act
            var validator = new CartShipmentValidator();
            var result = await validator.ValidateAsync(new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = _context.AvailShippingRates
            });

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().Contain(x => x.ErrorCode == "SHIPMENT_METHOD_CODE_REQUIRED");
        }

        [Fact]
        public async Task ValidateShipment_RuleSetDefault_UnavailableMethodError()
        {
            // Arrange
            var shipment = new CartModule.Core.Model.Shipment
            {
                ShipmentMethodCode = "UnavailableShipmentMethod"
            };

            // Act
            var validator = new CartShipmentValidator();
            var result = await validator.ValidateAsync(new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = _context.AvailShippingRates
            });

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().Contain(x => x.ErrorCode == "SHIPMENT_METHOD_UNAVAILABLE");
        }

        [Fact]
        public async Task ValidateShipment_RuleSetDefault_PriceError()
        {
            // Arrange
            var shipment = new CartModule.Core.Model.Shipment
            {
                ShipmentMethodCode = "shippingMethodCode",
                ShipmentMethodOption = ":)",
            };

            shipment.Price = _shippingRate.Rate + 1;

            // Act
            var validator = new CartShipmentValidator();
            var result = await validator.ValidateAsync(new ShipmentValidationContext
            {
                Shipment = shipment,
                AvailShippingRates = _context.AvailShippingRates
            });

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().Contain(x => x.ErrorCode == "SHIPMENT_METHOD_PRICE_CHANGED");
        }
    }
}
