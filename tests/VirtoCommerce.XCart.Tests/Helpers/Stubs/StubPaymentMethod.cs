using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.XCart.Tests.Helpers.Stubs
{
    public class StubPaymentMethod : PaymentMethod
    {
        public StubPaymentMethod(string code) : base(code)
        {
        }

        public override PaymentMethodType PaymentMethodType => throw new System.NotImplementedException();

        public override PaymentMethodGroupType PaymentMethodGroupType => throw new System.NotImplementedException();
    }
}
