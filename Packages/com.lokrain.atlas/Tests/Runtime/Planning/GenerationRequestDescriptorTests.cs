#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Core.Map;
using NUnit.Framework;

namespace Lokrain.Atlas.Planning.Tests
{
    public sealed class GenerationRequestDescriptorTests
    {
        private const string RecipeSymbolText =
            "lokrain.atlas.tests.recipe.primary";

        private const string FirstRouteStepSymbolText =
            "lokrain.atlas.tests.route_step.first";

        private const string SecondRouteStepSymbolText =
            "lokrain.atlas.tests.route_step.second";

        private const string FirstImplementationSymbolText =
            "lokrain.atlas.tests.implementation.first";

        private const string SecondImplementationSymbolText =
            "lokrain.atlas.tests.implementation.second";

        [Test]
        public void Constructor_WithoutOverrides_StoresRecipeSymbolAndRunSettings()
        {
            Symbol recipeSymbol = Symbol.Create(RecipeSymbolText);
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor descriptor = new(
                recipeSymbol,
                runSettings);

            Assert.That(descriptor.GenerationRecipeDefinitionSymbol, Is.SameAs(recipeSymbol));
            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Is.Empty);
        }

        [Test]
        public void Constructor_WithOverrides_StoresOverridesInOrder()
        {
            Symbol recipeSymbol = Symbol.Create(RecipeSymbolText);
            GenerationRunSettings runSettings = CreateRunSettings();

            OperationImplementationOverrideDescriptor firstOverride =
                CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText);

            OperationImplementationOverrideDescriptor secondOverride =
                CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText);

            GenerationRequestDescriptor descriptor = new(
                recipeSymbol,
                runSettings,
                new[]
                {
                    firstOverride,
                    secondOverride
                });

            Assert.That(descriptor.GenerationRecipeDefinitionSymbol, Is.SameAs(recipeSymbol));
            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(2));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(firstOverride));
            Assert.That(descriptor.OperationImplementationOverrides[1], Is.SameAs(secondOverride));
        }

        [Test]
        public void Constructor_WithOverrideSourceList_StoresSnapshot()
        {
            Symbol recipeSymbol = Symbol.Create(RecipeSymbolText);
            GenerationRunSettings runSettings = CreateRunSettings();

            OperationImplementationOverrideDescriptor firstOverride =
                CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText);

            OperationImplementationOverrideDescriptor secondOverride =
                CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText);

            List<OperationImplementationOverrideDescriptor> overrides = new()
            {
                firstOverride
            };

            GenerationRequestDescriptor descriptor = new(
                recipeSymbol,
                runSettings,
                overrides);

            overrides.Add(secondOverride);

            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(1));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(firstOverride));
        }

        [Test]
        public void OperationImplementationOverrides_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            OperationImplementationOverrideDescriptor firstOverride =
                CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText);

            GenerationRequestDescriptor descriptor = new(
                Symbol.Create(RecipeSymbolText),
                CreateRunSettings(),
                new[]
                {
                    firstOverride
                });

            Assert.That(
                descriptor.OperationImplementationOverrides,
                Is.InstanceOf<ICollection<OperationImplementationOverrideDescriptor>>());

            ICollection<OperationImplementationOverrideDescriptor> overrides =
                (ICollection<OperationImplementationOverrideDescriptor>)descriptor.OperationImplementationOverrides;

            Assert.That(overrides.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => overrides.Add(firstOverride));

            Assert.Throws<NotSupportedException>(
                overrides.Clear);
        }

        [Test]
        public void Constructor_WithNullRecipeSymbol_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequestDescriptor(null!, CreateRunSettings()));
        }

        [Test]
        public void Constructor_WithNullRunSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequestDescriptor(
                    Symbol.Create(RecipeSymbolText),
                    null!));
        }

        [Test]
        public void Constructor_WithNullOverridesCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new GenerationRequestDescriptor(
                    Symbol.Create(RecipeSymbolText),
                    CreateRunSettings(),
                    null!));
        }

        [Test]
        public void Constructor_WithNullOverrideEntry_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new GenerationRequestDescriptor(
                    Symbol.Create(RecipeSymbolText),
                    CreateRunSettings(),
                    new OperationImplementationOverrideDescriptor?[]
                    {
                        CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateRouteStepOverrideSymbols_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new GenerationRequestDescriptor(
                    Symbol.Create(RecipeSymbolText),
                    CreateRunSettings(),
                    new[]
                    {
                        CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                        CreateOverride(FirstRouteStepSymbolText, SecondImplementationSymbolText)
                    }));
        }

        [Test]
        public void Create_WithoutOverrides_WithValidSymbolValue_ReturnsDescriptor()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor descriptor =
                GenerationRequestDescriptor.Create(RecipeSymbolText, runSettings);

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(Symbol.Create(RecipeSymbolText)));

            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Is.Empty);
        }

        [Test]
        public void Create_WithOverrides_WithValidSymbolValue_ReturnsDescriptor()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            OperationImplementationOverrideDescriptor firstOverride =
                CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText);

            GenerationRequestDescriptor descriptor = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    firstOverride
                });

            Assert.That(
                descriptor.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(Symbol.Create(RecipeSymbolText)));

            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Has.Count.EqualTo(1));
            Assert.That(descriptor.OperationImplementationOverrides[0], Is.SameAs(firstOverride));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Invalid.Symbol")]
        public void Create_WithInvalidRecipeSymbolValue_ThrowsArgumentException(string? recipeSymbol)
        {
            Assert.Throws<ArgumentException>(
                () => GenerationRequestDescriptor.Create(
                    recipeSymbol,
                    CreateRunSettings()));
        }

        [Test]
        public void Create_WithNullRunSettings_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenerationRequestDescriptor.Create(
                    RecipeSymbolText,
                    null!));
        }

        [Test]
        public void Create_WithNullOverridesCollection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => GenerationRequestDescriptor.Create(
                    RecipeSymbolText,
                    CreateRunSettings(),
                    null!));
        }

        [Test]
        public void TryCreate_WithValidSymbolValueAndRunSettings_ReturnsTrueAndDescriptor()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            bool succeeded = GenerationRequestDescriptor.TryCreate(
                RecipeSymbolText,
                runSettings,
                out GenerationRequestDescriptor? descriptor);

            Assert.That(succeeded, Is.True);
            Assert.That(descriptor, Is.Not.Null);

            Assert.That(
                descriptor!.GenerationRecipeDefinitionSymbol,
                Is.EqualTo(Symbol.Create(RecipeSymbolText)));

            Assert.That(descriptor.RunSettings, Is.SameAs(runSettings));
            Assert.That(descriptor.OperationImplementationOverrides, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Invalid.Symbol")]
        public void TryCreate_WithInvalidRecipeSymbolValue_ReturnsFalse(string? recipeSymbol)
        {
            bool succeeded = GenerationRequestDescriptor.TryCreate(
                recipeSymbol,
                CreateRunSettings(),
                out GenerationRequestDescriptor? descriptor);

            Assert.That(succeeded, Is.False);
            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void TryCreate_WithNullRunSettings_ReturnsFalse()
        {
            bool succeeded = GenerationRequestDescriptor.TryCreate(
                RecipeSymbolText,
                null,
                out GenerationRequestDescriptor? descriptor);

            Assert.That(succeeded, Is.False);
            Assert.That(descriptor, Is.Null);
        }

        [Test]
        public void IsValid_WithValidSymbolValueAndRunSettings_ReturnsTrue()
        {
            bool isValid = GenerationRequestDescriptor.IsValid(
                RecipeSymbolText,
                CreateRunSettings());

            Assert.That(isValid, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Invalid.Symbol")]
        public void IsValid_WithInvalidRecipeSymbolValue_ReturnsFalse(string? recipeSymbol)
        {
            bool isValid = GenerationRequestDescriptor.IsValid(
                recipeSymbol,
                CreateRunSettings());

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValid_WithNullRunSettings_ReturnsFalse()
        {
            bool isValid = GenerationRequestDescriptor.IsValid(
                RecipeSymbolText,
                null);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Equals_WithSameRecipeRunSettingsAndOverrides_ReturnsTrue()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor left = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                    CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText)
                });

            GenerationRequestDescriptor right = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                    CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText)
                });

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentRecipeSymbol_ReturnsFalse()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor left =
                GenerationRequestDescriptor.Create("lokrain.atlas.tests.recipe.first", runSettings);

            GenerationRequestDescriptor right =
                GenerationRequestDescriptor.Create("lokrain.atlas.tests.recipe.second", runSettings);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentRunSettings_ReturnsFalse()
        {
            GenerationRequestDescriptor left = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                new GenerationRunSettings(new Grid(256, 256), new Seed(123UL)));

            GenerationRequestDescriptor right = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                new GenerationRunSettings(new Grid(512, 256), new Seed(123UL)));

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentOverrideImplementationSymbol_ReturnsFalse()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor left = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText)
                });

            GenerationRequestDescriptor right = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, SecondImplementationSymbolText)
                });

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithSameOverridesInDifferentOrder_ReturnsFalse()
        {
            GenerationRunSettings runSettings = CreateRunSettings();

            GenerationRequestDescriptor left = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                    CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText)
                });

            GenerationRequestDescriptor right = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                runSettings,
                new[]
                {
                    CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText),
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText)
                });

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            GenerationRequestDescriptor descriptor =
                GenerationRequestDescriptor.Create(RecipeSymbolText, CreateRunSettings());

            Assert.That(descriptor.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationRequestDescriptor descriptor =
                GenerationRequestDescriptor.Create(RecipeSymbolText, CreateRunSettings());

            Assert.That(descriptor.Equals("GenerationRequestDescriptor"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRequestDescriptor? left = null;
            GenerationRequestDescriptor? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRequestDescriptor? left =
                GenerationRequestDescriptor.Create(RecipeSymbolText, CreateRunSettings());

            GenerationRequestDescriptor? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsDescriptorSummary()
        {
            GenerationRequestDescriptor descriptor = GenerationRequestDescriptor.Create(
                RecipeSymbolText,
                CreateRunSettings(),
                new[]
                {
                    CreateOverride(FirstRouteStepSymbolText, FirstImplementationSymbolText),
                    CreateOverride(SecondRouteStepSymbolText, SecondImplementationSymbolText)
                });

            string value = descriptor.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRequestDescriptor(GenerationRecipeDefinitionSymbol: lokrain.atlas.tests.recipe.primary, RunSettings: GenerationRunSettings(Grid: Grid(Width: 256, Depth: 256), Seed: 123), OperationImplementationOverrides: 2)"));
        }

        private static GenerationRunSettings CreateRunSettings()
        {
            return new(
                new Grid(256, 256),
                new Seed(123UL));
        }

        private static OperationImplementationOverrideDescriptor CreateOverride(
            string stageRouteStepSymbol,
            string implementationSymbol)
        {
            return OperationImplementationOverrideDescriptor.Create(
                stageRouteStepSymbol,
                implementationSymbol);
        }
    }
}