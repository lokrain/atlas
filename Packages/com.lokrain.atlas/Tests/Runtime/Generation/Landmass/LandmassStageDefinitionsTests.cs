#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassStageDefinitionsTests
    {
        [Test]
        public void All_ReturnsStageDefinitionsInDeclaredOrder()
        {
            Assert.That(LandmassStageDefinitions.All, Has.Count.EqualTo(1));

            Assert.That(
                LandmassStageDefinitions.All[0],
                Is.SameAs(LandmassStageDefinitions.ContinentalLandmass));
        }

        [Test]
        public void ContinentalLandmass_UsesExpectedSchemaKindSymbolAndDisplayName()
        {
            StageDefinition stageDefinition = LandmassStageDefinitions.ContinentalLandmass;

            Assert.That(stageDefinition.GenerationSchema, Is.SameAs(BuiltInGenerationSchemas.World));
            Assert.That(stageDefinition.StageKind, Is.SameAs(LandmassStageKinds.ContinentalLandmass));
            Assert.That(stageDefinition.Symbol.Value, Is.EqualTo("lokrain.atlas.landmass.stage.continental_landmass"));
            Assert.That(stageDefinition.DisplayName.Value, Is.EqualTo("Continental Landmass"));
        }

        [Test]
        public void All_DoesNotContainDuplicateStageSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (StageDefinition stageDefinition in LandmassStageDefinitions.All)
            {
                Assert.That(
                    symbols.Add(stageDefinition.Symbol.Value),
                    Is.True,
                    $"Duplicate stage symbol: {stageDefinition.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassStageDefinitions.All,
                Is.InstanceOf<ICollection<StageDefinition>>());

            ICollection<StageDefinition> stageDefinitions =
                (ICollection<StageDefinition>)LandmassStageDefinitions.All;

            Assert.That(stageDefinitions.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stageDefinitions.Add(LandmassStageDefinitions.ContinentalLandmass));

            Assert.Throws<NotSupportedException>(
                stageDefinitions.Clear);
        }
    }
}