using System.Collections.Generic;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.XCart.Core.Models
{
    public sealed class ExpCartPayment
    {
        public Optional<string> Id { get; set; }
        public Optional<string> OuterId { get; set; }
        public Optional<string> PaymentGatewayCode { get; set; }
        public Optional<string> Currency { get; set; }
        public Optional<decimal> Price { get; set; }
        public Optional<decimal> Amount { get; set; }
        public Optional<string> Purpose { get; set; }
        public Optional<string> Comment { get; set; }
        public Optional<string> VendorId { get; set; }

        public Optional<ExpCartAddress> BillingAddress { get; set; }

        public IList<DynamicPropertyValue> DynamicProperties { get; set; }

        public Payment MapTo(Payment payment)
        {
            if (payment == null)
            {
                payment = AbstractTypeFactory<Payment>.TryCreateInstance();
            }

            Optional.SetValue(Id, x => payment.Id = x);
            Optional.SetValue(OuterId, x => payment.OuterId = x);
            Optional.SetValue(PaymentGatewayCode, x => payment.PaymentGatewayCode = x);
            Optional.SetValue(Currency, x => payment.Currency = x);
            Optional.SetValue(Price, x => payment.Price = x);
            Optional.SetValue(Amount, x => payment.Amount = x);
            Optional.SetValue(Purpose, x => payment.Purpose = x);
            Optional.SetValue(Comment, x => payment.Comment = x);
            Optional.SetValue(VendorId, x => payment.VendorId = x);
            Optional.SetValue(BillingAddress, x => payment.BillingAddress = x?.MapTo(payment.BillingAddress));

            return payment;
        }
    }
}
