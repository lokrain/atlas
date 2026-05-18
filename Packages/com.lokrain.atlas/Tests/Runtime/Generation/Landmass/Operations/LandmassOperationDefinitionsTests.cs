#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassOperationDefinitionsTests
    {
        [Test]
        public void All_ReturnsOperationDefinitionsInDeclaredOrder()
        {
            Assert.That(LandmassOperationDefinitions.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassOperationDefinitions.All[0],
                Is.SameAs(LandmassOperationDefinitions.EvaluateContinentSuitability));

            Assert.That(
                LandmassOperationDefinitions.All[1],
                Is.SameAs(LandmassOperationDefinitions.FormContinentCandidate));

            Assert.That(
                LandmassOperationDefinitions.All[2],
                Is.SameAs(LandmassOperationDefinitions.ExtractMainContinent));

            Assert.That(
                LandmassOperationDefinitions.All[3],
                Is.SameAs(LandmassOperationDefinitions.CompleteContinentArea));

            Assert.That(
                LandmassOperationDefinitions.All[4],
                Is.SameAs(LandmassOperationDefinitions.ComposeBaseElevation));
        }

        [Test]
        public void Definitions_UseExpectedSchemasKindsSymbolsAndDisplayNames()
        {
            AssertOperationDefinition(
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                LandmassOperationKinds.ContinentSuitabilityEvaluation,
                "lokrain.atlas.landmass.operation.evaluate_continent_suitability",
                "Evaluate Continent Suitability");

            AssertOperationDefinition(
                LandmassOperationDefinitions.FormContinentCandidate,
                LandmassOperationKinds.ContinentCandidateFormation,
                "lokrain.atlas.landmass.operation.form_continent_candidate",
                "Form Continent Candidate");

            AssertOperationDefinition(
                LandmassOperationDefinitions.ExtractMainContinent,
                LandmassOperationKinds.MainContinentExtraction,
                "lokrain.atlas.landmass.operation.extract_main_continent",
                "Extract Main Continent");

            AssertOperationDefinition(
                LandmassOperationDefinitions.CompleteContinentArea,
                LandmassOperationKinds.ContinentAreaCompletion,
                "lokrain.atlas.landmass.operation.complete_continent_area",
                "Complete Continent Area");

            AssertOperationDefinition(
                LandmassOperationDefinitions.ComposeBaseElevation,
                LandmassOperationKinds.BaseElevationComposition,
                "lokrain.atlas.landmass.operation.compose_base_elevation",
                "Compose Base Elevation");
        }

        [Test]
        public void All_DoesNotContainDuplicateOperationSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (OperationDefinition operationDefinition in LandmassOperationDefinitions.All)
            {
                Assert.That(
                    symbols.Add(operationDefinition.Symbol.Value),
                    Is.True,
                    $"Duplicate operation symbol: {operationDefinition.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassOperationDefinitions.All,
                Is.InstanceOf<ICollection<OperationDefinition>>());

            ICollection<OperationDefinition> operationDefinitions =
                (ICollection<OperationDefinition>)LandmassOperationDefinitions.All;

            Assert.That(operationDefinitions.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => operationDefinitions.Add(LandmassOperationDefinitions.EvaluateContinentSuitability));

            Assert.Throws<NotSupportedException>(
                operationDefinitions.Clear);
        }

        private static void AssertOperationDefinition(
            OperationDefinition operationDefinition,
            OperationKind expectedOperationKind,
            string expectedSymbolValue,
            string expectedDisplayNameValue)
        {
            Assert.That(operationDefinition.GenerationSchema, Is.SameAs(BuiltInGenerationSchemas.World));
            Assert.That(operationDefinition.OperationKind, Is.SameAs(expectedOperationKind));
            Assert.That(operationDefinition.Symbol.Value, Is.EqualTo(expectedSymbolValue));
            Assert.That(operationDefinition.DisplayName.Value, Is.EqualTo(expectedDisplayNameValue));
        }
    }
}