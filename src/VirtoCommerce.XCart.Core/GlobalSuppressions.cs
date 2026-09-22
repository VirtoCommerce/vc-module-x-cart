using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Critical Code Smell", "S1699:Constructors should only call non-overridable methods",
    Scope = "namespaceanddescendants", Target = "~N:VirtoCommerce.XCart.Core.Schemas",
    Justification = "graphql-dotnet schema types build fields via the virtual Field(...) builder in the ctor; subclassed via OverrideGraphType so sealing does not apply.")]
