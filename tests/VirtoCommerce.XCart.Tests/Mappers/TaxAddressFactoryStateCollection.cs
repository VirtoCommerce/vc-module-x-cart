using Xunit;

namespace VirtoCommerce.XCart.Tests.Mappers;

// Every class registering or reading AbstractTypeFactory<TaxAddress> (process-global) joins this
// collection, so xUnit never runs them concurrently.
[CollectionDefinition(Name)]
public class TaxAddressFactoryStateCollection
{
    public const string Name = "AbstractTypeFactory<TaxAddress> state";
}
