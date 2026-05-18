#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassStageContractsTests
    {
        [Test]
        public void All_ReturnsStageContractsInDeclaredOrder()
        {
            Assert.That(LandmassStageContracts.All, Has.Count.EqualTo(1));

            Assert.That(
                LandmassStageContracts.All[0],
                Is.SameAs(LandmassStageContracts.ContinentalLandmass));
        }

        [Test]
        public void ContinentalLandmass_UsesExpectedStageDefinition()
        {
            StageContract stageContract = LandmassStageContracts.ContinentalLandmass;

            Assert.That(
                stageContract.StageDefinition,
                Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));
        }

        [Test]
        public void ContinentalLandmass_HasNoRequiredInputs()
        {
            StageContract stageContract = LandmassStageContracts.ContinentalLandmass;

            Assert.That(stageContract.RequiredInputs, Is.Empty);
        }

        [Test]
        public void ContinentalLandmass_ProducesExpectedResourcesInDeclaredOrder()
        {
            StageContract stageContract = LandmassStageContracts.ContinentalLandmass;

            Assert.That(stageContract.ProducedOutputs, Has.Count.EqualTo(2));

            Assert.That(
                stageContract.ProducedOutputs[0],
                Is.SameAs(LandmassResourceDefinitions.ContinentalLandmassArea));

            Assert.That(
                stageContract.ProducedOutputs[1],
                Is.SameAs(LandmassResourceDefinitions.BaseElevation));
        }

        [Test]
        public void ContinentalLandmass_ProducedOutputsUseSameGenerationSchemaAsStage()
        {
            StageContract stageContract = LandmassStageContracts.ContinentalLandmass;

            foreach (ResourceDefinition producedOutput in stageContract.ProducedOutputs)
            {
                Assert.That(
                    producedOutput.GenerationSchema,
                    Is.SameAs(stageContract.StageDefinition.GenerationSchema));
            }
        }

        [Test]
        public void All_DoesNotContainDuplicateStageDefinitions()
        {
            var stageDefinitions = new HashSet<StageDefinition>();

            foreach (StageContract stageContract in LandmassStageContracts.All)
            {
                Assert.That(
                    stageDefinitions.Add(stageContract.StageDefinition),
                    Is.True,
                    $"Duplicate stage contract for stage definition: {stageContract.StageDefinition.Symbol}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassStageContracts.All,
                Is.InstanceOf<ICollection<StageContract>>());

            ICollection<StageContract> stageContracts =
                (ICollection<StageContract>)LandmassStageContracts.All;

            Assert.That(stageContracts.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stageContracts.Add(LandmassStageContracts.ContinentalLandmass));

            Assert.Throws<NotSupportedException>(
                stageContracts.Clear);
        }
    }
}