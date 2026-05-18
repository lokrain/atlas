#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Generation.Landmass.Operations;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Resources;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassOperationContractsTests
    {
        [Test]
        public void All_ReturnsOperationContractsInDeclaredOrder()
        {
            Assert.That(LandmassOperationContracts.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassOperationContracts.All[0],
                Is.SameAs(LandmassOperationContracts.EvaluateContinentSuitability));

            Assert.That(
                LandmassOperationContracts.All[1],
                Is.SameAs(LandmassOperationContracts.FormContinentCandidate));

            Assert.That(
                LandmassOperationContracts.All[2],
                Is.SameAs(LandmassOperationContracts.ExtractMainContinent));

            Assert.That(
                LandmassOperationContracts.All[3],
                Is.SameAs(LandmassOperationContracts.CompleteContinentArea));

            Assert.That(
                LandmassOperationContracts.All[4],
                Is.SameAs(LandmassOperationContracts.ComposeBaseElevation));
        }

        [Test]
        public void Definitions_UseExpectedOperationDefinitionsInputsAndOutputs()
        {
            AssertOperationContract(
                LandmassOperationContracts.EvaluateContinentSuitability,
                LandmassOperationDefinitions.EvaluateContinentSuitability,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    LandmassResourceDefinitions.ContinentSuitability
                });

            AssertOperationContract(
                LandmassOperationContracts.FormContinentCandidate,
                LandmassOperationDefinitions.FormContinentCandidate,
                new[]
                {
                    LandmassResourceDefinitions.ContinentSuitability
                },
                new[]
                {
                    LandmassResourceDefinitions.ContinentCandidate
                });

            AssertOperationContract(
                LandmassOperationContracts.ExtractMainContinent,
                LandmassOperationDefinitions.ExtractMainContinent,
                new[]
                {
                    LandmassResourceDefinitions.ContinentCandidate
                },
                new[]
                {
                    LandmassResourceDefinitions.MainContinent
                });

            AssertOperationContract(
                LandmassOperationContracts.CompleteContinentArea,
                LandmassOperationDefinitions.CompleteContinentArea,
                new[]
                {
                    LandmassResourceDefinitions.MainContinent
                },
                new[]
                {
                    LandmassResourceDefinitions.ContinentalLandmassArea
                });

            AssertOperationContract(
                LandmassOperationContracts.ComposeBaseElevation,
                LandmassOperationDefinitions.ComposeBaseElevation,
                new[]
                {
                    LandmassResourceDefinitions.ContinentalLandmassArea
                },
                new[]
                {
                    LandmassResourceDefinitions.BaseElevation
                });
        }

        [Test]
        public void All_DoesNotContainDuplicateOperationDefinitions()
        {
            var operationDefinitions = new HashSet<OperationDefinition>();

            foreach (OperationContract operationContract in LandmassOperationContracts.All)
            {
                Assert.That(
                    operationDefinitions.Add(operationContract.OperationDefinition),
                    Is.True,
                    $"Duplicate operation contract for operation definition: {operationContract.OperationDefinition.Symbol}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassOperationContracts.All,
                Is.InstanceOf<ICollection<OperationContract>>());

            ICollection<OperationContract> operationContracts =
                (ICollection<OperationContract>)LandmassOperationContracts.All;

            Assert.That(operationContracts.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => operationContracts.Add(LandmassOperationContracts.EvaluateContinentSuitability));

            Assert.Throws<NotSupportedException>(
                operationContracts.Clear);
        }

        private static void AssertOperationContract(
            OperationContract operationContract,
            OperationDefinition expectedOperationDefinition,
            IReadOnlyList<ResourceDefinition> expectedRequiredInputs,
            IReadOnlyList<ResourceDefinition> expectedProducedOutputs)
        {
            Assert.That(operationContract.OperationDefinition, Is.SameAs(expectedOperationDefinition));

            Assert.That(operationContract.RequiredInputs, Has.Count.EqualTo(expectedRequiredInputs.Count));

            for (int index = 0; index < expectedRequiredInputs.Count; index++)
            {
                Assert.That(
                    operationContract.RequiredInputs[index],
                    Is.SameAs(expectedRequiredInputs[index]));
            }

            Assert.That(operationContract.ProducedOutputs, Has.Count.EqualTo(expectedProducedOutputs.Count));

            for (int index = 0; index < expectedProducedOutputs.Count; index++)
            {
                Assert.That(
                    operationContract.ProducedOutputs[index],
                    Is.SameAs(expectedProducedOutputs[index]));
            }
        }
    }
}