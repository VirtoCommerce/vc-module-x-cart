using FluentValidation;
using VirtoCommerce.CartModule.Core.Model;

namespace VirtoCommerce.XCart.Core.Validators;

public interface IConfigurationItemValidator : IValidator<LineItem>;
