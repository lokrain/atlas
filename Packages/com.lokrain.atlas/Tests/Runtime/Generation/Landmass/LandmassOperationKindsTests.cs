#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Operations;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassOperationKindsTests
    {
        [Test]
        public void All_ReturnsOperationKindsInDeclaredOrder()
        {
            Assert.That(LandmassOperationKinds.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassOperationKinds.All[0],
                Is.SameAs(LandmassOperationKinds.ContinentSuitabilityEvaluation));

            Assert.That(
                LandmassOperationKinds.All[1],
                Is.SameAs(LandmassOperationKinds.ContinentCandidateFormation));

            Assert.That(
                LandmassOperationKinds.All[2],
                Is.SameAs(LandmassOperationKinds.MainContinentExtraction));

            Assert.That(
                LandmassOperationKinds.All[3],
                Is.SameAs(LandmassOperationKinds.ContinentAreaCompletion));

            Assert.That(
                LandmassOperationKinds.All[4],
                Is.SameAs(LandmassOperationKinds.BaseElevationComposition));
        }

        [Test]
        public void Definitions_UseExpectedSymbols()
        {
            AssertOperationKind(
                LandmassOperationKinds.ContinentSuitabilityEvaluation,
                "lokrain.atlas.landmass.operation_kind.continent_suitability_evaluation");

            AssertOperationKind(
                LandmassOperationKinds.ContinentCandidateFormation,
                "lokrain.atlas.landmass.operation_kind.continent_candidate_formation");

            AssertOperationKind(
                LandmassOperationKinds.MainContinentExtraction,
                "lokrain.atlas.landmass.operation_kind.main_continent_extraction");

            AssertOperationKind(
                LandmassOperationKinds.ContinentAreaCompletion,
                "lokrain.atlas.landmass.operation_kind.continent_area_completion");

            AssertOperationKind(
                LandmassOperationKinds.BaseElevationComposition,
                "lokrain.atlas.landmass.operation_kind.base_elevation_composition");
        }

        [Test]
        public void All_DoesNotContainDuplicateOperationKindSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (OperationKind operationKind in LandmassOperationKinds.All)
            {
                Assert.That(
                    symbols.Add(operationKind.Symbol.Value),
                    Is.True,
                    $"Duplicate operation kind symbol: {operationKind.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassOperationKinds.All,
                Is.InstanceOf<ICollection<OperationKind>>());

            ICollection<OperationKind> operationKinds =
                (ICollection<OperationKind>)LandmassOperationKinds.All;

            Assert.That(operationKinds.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => operationKinds.Add(LandmassOperationKinds.ContinentSuitabilityEvaluation));

            Assert.Throws<NotSupportedException>(
                operationKinds.Clear);
        }

        private static void AssertOperationKind(
            OperationKind operationKind,
            string expectedSymbolValue)
        {
            Assert.That(operationKind.Symbol.Value, Is.EqualTo(expectedSymbolValue));
        }
    }
}