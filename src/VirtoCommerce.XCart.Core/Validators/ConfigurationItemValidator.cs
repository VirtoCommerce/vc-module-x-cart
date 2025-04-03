using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.XCart.Core.Validators;

public class ConfigurationItemValidator : AbstractValidator<LineItem>, IConfigurationItemValidator
{
    private readonly ISettingsManager _settingsManager;
    private readonly IProductConfigurationSearchService _productConfigurationService;

    public ConfigurationItemValidator(ISettingsManager settingsManager, IProductConfigurationSearchService productConfigurationService)
    {
        _settingsManager = settingsManager;
        _productConfigurationService = productConfigurationService;

        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ConfigurationItems).NotEmpty();
        RuleFor(x => x).CustomAsync(ValidateAsync);
    }

    private async Task ValidateAsync(LineItem item, ValidationContext<LineItem> context, CancellationToken token)
    {
        var configuration = await ValidateConfiguration(item, context);
        if (configuration == null)
        {
            return;
        }

        var missingRequiredSectionIds = configuration.Sections
            .Where(x => x.IsRequired)
            .Select(x => x.Id)
            .Except(item.ConfigurationItems.Select(x => x.SectionId))
            .ToList();

        if (missingRequiredSectionIds.Count > 0)
        {
            context.AddFailure(CartErrorDescriber.MissingRequiredSections(item, missingRequiredSectionIds));
        }

        var limitOfFiles = await _settingsManager.GetValueAsync<int>(ProductConfigurationMaximumFiles);
        var sections = configuration.Sections.ToDictionary(x => x.Id);

        foreach (var configurationItem in item.ConfigurationItems)
        {
            if (!sections.TryGetValue(configurationItem.SectionId, out var section))
            {
                context.AddFailure(CartErrorDescriber.ConfigurationSectionNotFound(configurationItem, configurationItem.SectionId));
            }

            if (section != null && section.Type != configurationItem.Type)
            {
                context.AddFailure(CartErrorDescriber.ConfigurationSectionTypeMismatch(configurationItem, configurationItem.Type, configurationItem.SectionId));
            }

            switch (configurationItem.Type)
            {
                case ConfigurationSectionTypeProduct:
                    ValidateSectionTypeProduct(configurationItem, section, context);
                    break;
                case ConfigurationSectionTypeFile:
                    ValidateSectionTypeFile(configurationItem, section, context, limitOfFiles);
                    break;
                case ConfigurationSectionTypeText:
                    ValidateSectionTypeText(configurationItem, section, context);
                    break;
                default:
                    context.AddFailure(CartErrorDescriber.ConfigurationSectionUnknownType(configurationItem, configurationItem.Type, configurationItem.SectionId));
                    break;
            }
        }
    }

    private async Task<ProductConfiguration> ValidateConfiguration(LineItem item, ValidationContext<LineItem> context)
    {
        var searchCriteria = new ProductConfigurationSearchCriteria
        {
            ProductIds = [item.ProductId],
            IsActive = true
        };

        var searchResult = await _productConfigurationService.SearchNoCloneAsync(searchCriteria);
        var configuration = searchResult.Results.FirstOrDefault();

        if (configuration == null)
        {
            context.AddFailure(CartErrorDescriber.ConfigurationForProductNotFound(item, item.ProductId));
        }

        return configuration;
    }

    private static void ValidateSectionTypeFile(ConfigurationItem configurationItem, ProductConfigurationSection section, ValidationContext<LineItem> context, int limitOfFiles)
    {
        if (section != null && section.IsRequired && configurationItem.Files.IsNullOrEmpty())
        {
            context.AddFailure(CartErrorDescriber.AddingFileIsRequired(section));
        }

        if (configurationItem.Files.Count > limitOfFiles)
        {
            context.AddFailure(CartErrorDescriber.FilesQuantityLimitError(configurationItem, limitOfFiles));
        }
    }

    private static void ValidateSectionTypeText(ConfigurationItem configurationItem, ProductConfigurationSection section, ValidationContext<LineItem> context)
    {
        if (section != null && section.IsRequired && string.IsNullOrEmpty(configurationItem.CustomText))
        {
            context.AddFailure(CartErrorDescriber.CustomTextIsRequired(section));
        }
    }

    private static void ValidateSectionTypeProduct(ConfigurationItem configurationItem, ProductConfigurationSection section, ValidationContext<LineItem> context)
    {
        if (section != null)
        {
            if (section.IsRequired && string.IsNullOrEmpty(configurationItem.ProductId))
            {
                context.AddFailure(CartErrorDescriber.SelectedProductIsRequired(section));
            }

            if (section.Options.All(x => x.ProductId != configurationItem.ProductId))
            {
                context.AddFailure(CartErrorDescriber.ProductUnavailableForSectionError(nameof(CatalogProduct), configurationItem.ProductId, section.Id));
            }
        }

        if (configurationItem.Quantity <= 0)
        {
            context.AddFailure(CartErrorDescriber.ProductMinQuantityError(nameof(CatalogProduct), configurationItem.ProductId, configurationItem.Quantity, 1));
        }
    }
}
