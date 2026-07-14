using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Data.Validators;
using Xunit;

namespace VirtoCommerce.XCart.Tests.Validators;

// VCST-5103 guard: a factory OverrideType must flow through ICartValidatorRegistry (the AddXCart bridge).
// A dedicated probe type with its own AbstractTypeFactory<SeamProbeValidator> keeps the mutation from
// bleeding into the parallel XCartMoqHelper-based tests.
public class CartValidatorRegistrySeamTests
{
    [Fact]
    public async Task Registry_HonorsAbstractTypeFactoryOverride_WhenRegistrationBridgedThroughFactory()
    {
        AbstractTypeFactory<SeamProbeValidator>.OverrideType<SeamProbeValidator, SeamProbeValidatorOverride>();
        try
        {
            var services = new ServiceCollection();
            // Same bridge shape as production AddXCart.
            services.AddTransient<ICartValidator<SeamProbeContext>>(_ => AbstractTypeFactory<SeamProbeValidator>.TryCreateInstance());
            using var provider = services.BuildServiceProvider();

            var registry = new CartValidatorRegistry(provider);
            var errors = await registry.ValidateAsync(new SeamProbeContext());

            errors.Should().ContainSingle(x => x.ErrorCode == SeamProbeValidatorOverride.MarkerCode);
        }
        finally
        {
            AbstractTypeFactory<SeamProbeValidator>.RemoveType<SeamProbeValidatorOverride>();
        }
    }

    public class SeamProbeContext
    {
    }

    public class SeamProbeValidator : AbstractValidator<SeamProbeContext>, ICartValidator<SeamProbeContext>
    {
    }

    public sealed class SeamProbeValidatorOverride : SeamProbeValidator
    {
        public const string MarkerCode = "SEAM_PROBE_OVERRIDE";

        public SeamProbeValidatorOverride()
        {
            RuleFor(x => x).Custom((_, context) =>
                context.AddFailure(new ValidationFailure(string.Empty, "probe") { ErrorCode = MarkerCode }));
        }
    }
}
