using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CartModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Configuration;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.CatalogModule.Core.Search;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.XCart.Core.Validators;
using VirtoCommerce.XCart.Tests.Helpers;
using Xunit;
using static VirtoCommerce.CatalogModule.Core.ModuleConstants;

namespace VirtoCommerce.XCart.Tests.Validators
{
    public class ConfigurationItemValidatorTests : XCartMoqHelper
    {
        private const string FilesRequiredErrorCode = "CONFIGURATION_SECTION_FILES_REQUIRED";
        private const string FilesLimitErrorCode = "CONFIGURATION_SECTION_FILE_LIMIT";
        private const string TestProductId = "test-product";
        private const string FileSectionId = "test-file-section";
        private const int MaximumFiles = 5;

        private readonly Mock<ISettingsManager> _settingsManagerMock = new();
        private readonly Mock<IProductConfigurationSearchService> _productConfigurationSearchServiceMock = new();

        public ConfigurationItemValidatorTests()
        {
            _settingsManagerMock
                .Setup(x => x.GetObjectSettingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ObjectSettingEntry { Value = MaximumFiles });
        }

        [Fact]
        public async Task ValidateFileSection_RequiredSectionWithOnlyEmptyFile_Invalid()
        {
            // Arrange
            var validator = CreateValidator(sectionIsRequired: true);
            var lineItem = BuildLineItem(0);

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.ErrorCode == FilesRequiredErrorCode);
        }

        [Fact]
        public async Task ValidateFileSection_RequiredSectionWithNonEmptyFile_Valid()
        {
            // Arrange
            var validator = CreateValidator(sectionIsRequired: true);
            var lineItem = BuildLineItem(1024);

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateFileSection_RequiredSectionWithEmptyAndNonEmptyFiles_Valid()
        {
            // Arrange
            var validator = CreateValidator(sectionIsRequired: true);
            var lineItem = BuildLineItem(0, 1024);

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateFileSection_OptionalSectionWithOnlyEmptyFile_Valid()
        {
            // Arrange
            var validator = CreateValidator(sectionIsRequired: false);
            var lineItem = BuildLineItem(0);

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateFileSection_RequiredSectionWithoutFiles_Invalid()
        {
            // Arrange
            var validator = CreateValidator(sectionIsRequired: true);
            var lineItem = BuildLineItem();

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.ErrorCode == FilesRequiredErrorCode);
        }

        [Fact]
        public async Task ValidateFileSection_FilesWithinConfiguredLimit_NoLimitFailure()
        {
            // Guards the maximum-files rule that sits next to the required-files rule:
            // the settings mock must resolve to a real limit, otherwise the "valid" cases
            // above would be passing for the wrong reason.
            // Arrange
            var validator = CreateValidator(sectionIsRequired: true);
            var lineItem = BuildLineItem(1024, 2048);

            // Act
            var result = await validator.ValidateAsync(lineItem, TestContext.Current.CancellationToken);

            // Assert
            result.Errors.Should().NotContain(x => x.ErrorCode == FilesLimitErrorCode);
        }

        private ConfigurationItemValidator CreateValidator(bool sectionIsRequired)
        {
            var configuration = new ProductConfiguration
            {
                Id = "test-configuration",
                ProductId = TestProductId,
                IsActive = true,
                Sections =
                [
                    new ProductConfigurationSection
                    {
                        Id = FileSectionId,
                        Name = "Attachments",
                        Type = ConfigurationSectionTypeFile,
                        IsRequired = sectionIsRequired,
                    },
                ],
            };

            _productConfigurationSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<ProductConfigurationSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new ProductConfigurationSearchResult
                {
                    TotalCount = 1,
                    Results = [configuration],
                });

            return new ConfigurationItemValidator(_settingsManagerMock.Object, _productConfigurationSearchServiceMock.Object);
        }

        private static LineItem BuildLineItem(params long[] fileSizes)
        {
            var files = fileSizes
                .Select((size, index) => new ConfigurationItemFile
                {
                    Id = $"test-file-{index}",
                    Name = $"test-file-{index}.txt",
                    Url = $"/files/test-file-{index}.txt",
                    ContentType = "text/plain",
                    Size = size,
                })
                .ToList<ConfigurationItemFile>();

            return new LineItem
            {
                ProductId = TestProductId,
                ConfigurationItems =
                [
                    new ConfigurationItem
                    {
                        SectionId = FileSectionId,
                        SectionName = "Attachments",
                        Type = ConfigurationSectionTypeFile,
                        Files = files,
                    },
                ],
            };
        }
    }
}
