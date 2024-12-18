using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using Address = VirtoCommerce.CartModule.Core.Model.Address;

namespace VirtoCommerce.XCart.Core.Models
{
    public class ExpCartAddress
    {
        public Optional<string> Id { get; set; }
        public Optional<string> Key { get; set; }
        public Optional<string> City { get; set; }
        public Optional<string> CountryCode { get; set; }
        public Optional<string> CountryName { get; set; }
        public Optional<string> Email { get; set; }
        public Optional<string> FirstName { get; set; }
        public Optional<string> LastName { get; set; }
        public Optional<string> MiddleName { get; set; }
        public Optional<string> Name { get; set; }
        public Optional<string> Line1 { get; set; }
        public Optional<string> Line2 { get; set; }
        public Optional<string> Organization { get; set; }
        public Optional<string> Phone { get; set; }
        public Optional<string> PostalCode { get; set; }
        public Optional<string> RegionId { get; set; }
        public Optional<string> RegionName { get; set; }
        public Optional<string> Zip { get; set; }
        public Optional<string> OuterId { get; set; }
        public Optional<string> Description { get; set; }
        public Optional<int> AddressType { get; set; }

        public virtual Address MapTo(Address address)
        {
            if (address == null)
            {
                address = AbstractTypeFactory<Address>.TryCreateInstance();
            }

            Optional.SetValue(Id, x => address.Key = x);
            Optional.SetValue(Key, x => address.Key = x);
            Optional.SetValue(City, x => address.City = x);
            Optional.SetValue(CountryCode, x => address.CountryCode = x);
            Optional.SetValue(CountryName, x => address.CountryName = x);
            Optional.SetValue(Email, x => address.Email = x);
            Optional.SetValue(FirstName, x => address.FirstName = x);
            Optional.SetValue(LastName, x => address.LastName = x);
            Optional.SetValue(Line1, x => address.Line1 = x);
            Optional.SetValue(Line2, x => address.Line2 = x);
            Optional.SetValue(MiddleName, x => address.MiddleName = x);
            Optional.SetValue(Name, x => address.Name = x);
            Optional.SetValue(Organization, x => address.Organization = x);
            Optional.SetValue(Phone, x => address.Phone = x);
            Optional.SetValue(PostalCode, x => address.PostalCode = x);
            Optional.SetValue(RegionId, x => address.RegionId = x);
            Optional.SetValue(RegionName, x => address.RegionName = x);
            Optional.SetValue(Zip, x => address.Zip = x);
            Optional.SetValue(OuterId, x => address.OuterId = x);
            Optional.SetValue(Description, x => address.Description = x);
            Optional.SetValue(AddressType, x => address.AddressType = (AddressType)x);

            return address;
        }
    }
}
