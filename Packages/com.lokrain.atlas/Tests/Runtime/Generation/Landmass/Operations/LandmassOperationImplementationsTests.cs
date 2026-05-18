#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Operations;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassOperationImplementationsTests
    {
        [Test]
        public void All_ReturnsOperationImplementationsInDeclaredOrder()
        {
            Assert.That(LandmassOperationImplementations.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassOperationImplementations.All[0],
                Is.SameAs(LandmassOperationImplementations.EvaluateContinentSuitabilityDefault));

            Assert.That(
                LandmassOperationImplementations.All[1],
                Is.SameAs(LandmassOperationImplementations.FormContinentCandidateDefault));

            Assert.That(
                LandmassOperationImplementations.All[2],
                Is.SameAs(LandmassOperationImplementations.ExtractMainContinentDefault));

            Assert.That(
                LandmassOperationImplementations.All[3],
                Is.SameAs(LandmassOperationImplementations.CompleteContinentAreaDefault));

            Assert.That(
                LandmassOperationImplementations.All[4],
                Is.SameAs(LandmassOperationImplementations.ComposeBaseElevationDefault));
        }

        [Test]
        public void Definitions_UseExpectedOperationDefinitionsSymbolsAndDisplayNames()
        {
            AssertOperationImplementation(
                LandmassOperationImplementations.EvaluateContinentSuitabilityDefault,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                "lokrain.atlas.landmass.implementation.evaluate_continent_suitability.default",
                "Default Evaluate Continent Suitability");

            AssertOperationImplementation(
                LandmassOperationImplementations.FormContinentCandidateDefault,
                LandmassOperationDefinitions.FormContinentCandidate,
                "lokrain.atlas.landmass.implementation.form_continent_candidate.default",
                "Default Form Continent Candidate");

            AssertOperationImplementation(
                LandmassOperationImplementations.ExtractMainContinentDefault,
                LandmassOperationDefinitions.ExtractMainContinent,
                "lokrain.atlas.landmass.implementation.extract_main_continent.default",
                "Default Extract Main Continent");

            AssertOperationImplementation(
                LandmassOperationImplementations.CompleteContinentAreaDefault,
                LandmassOperationDefinitions.CompleteContinentArea,
                "lokrain.atlas.landmass.implementation.complete_continent_area.default",
                "Default Complete Continent Area");

            AssertOperationImplementation(
                LandmassOperationImplementations.ComposeBaseElevationDefault,
                LandmassOperationDefinitions.ComposeBaseElevation,
                "lokrain.atlas.landmass.implementation.compose_base_elevation.default",
                "Default Compose Base Elevation");
        }

        [Test]
        public void All_DoesNotContainDuplicateOperationImplementationSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (OperationImplementationDefinition operationImplementation in LandmassOperationImplementations.All)
            {
                Assert.That(
                    symbols.Add(operationImplementation.Symbol.Value),
                    Is.True,
                    $"Duplicate operation implementation symbol: {operationImplementation.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassOperationImplementations.All,
                Is.InstanceOf<ICollection<OperationImplementationDefinition>>());

            ICollection<OperationImplementationDefinition> operationImplementations =
                (ICollection<OperationImplementationDefinition>)LandmassOperationImplementations.All;

            Assert.That(operationImplementations.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => operationImplementations.Add(
                    LandmassOperationImplementations.EvaluateContinentSuitabilityDefault));

            Assert.Throws<NotSupportedException>(
                operationImplementations.Clear);
        }

        private static void AssertOperationImplementation(
            OperationImplementationDefinition operationImplementation,
            OperationDefinition expectedOperationDefinition,
            string expectedSymbolValue,
            string expectedDisplayNameValue)
        {
            Assert.That(operationImplementation.OperationDefinition, Is.SameAs(expectedOperationDefinition));
            Assert.That(operationImplementation.Symbol.Value, Is.EqualTo(expectedSymbolValue));
            Assert.That(operationImplementation.DisplayName.Value, Is.EqualTo(expectedDisplayNameValue));
        }
    }
}