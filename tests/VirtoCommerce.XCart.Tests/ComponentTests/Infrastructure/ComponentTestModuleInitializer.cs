using System.Runtime.CompilerServices;
using System.Security.Claims;
using GraphQL;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Centralized static initialization for the component-test harness. Runs automatically via
    /// <see cref="ModuleInitializerAttribute"/> before any test executes — no manual calls needed.
    /// <para>
    /// Initializes the static claim-type configuration (so <c>ClaimsPrincipal.GetUserId()</c> resolves
    /// the test user) and the GraphQL global switches that the Xapi module sets on startup. These are
    /// process-wide and idempotent, so doing them once in a module initializer matches production.
    /// </para>
    /// </summary>
    internal static class ComponentTestModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Required for ClaimsPrincipal.GetUserId()/GetUserName() to resolve the test principal.
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];
            ClaimsPrincipalExtensions.UserNameClaimTypes = [ClaimTypes.Name];

            // Match production: the Xapi module enables legacy type naming for backward compatibility
            // and disables NRT-based nullability inference.
#pragma warning disable CS0618 // Type or member is obsolete
            GlobalSwitches.UseLegacyTypeNaming = true;
#pragma warning restore CS0618
            GlobalSwitches.InferFieldNullabilityFromNRTAnnotations = false;
        }
    }
}
