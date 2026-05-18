#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassStageKindsTests
    {
        [Test]
        public void All_ReturnsStageKindsInDeclaredOrder()
        {
            Assert.That(LandmassStageKinds.All, Has.Count.EqualTo(1));

            Assert.That(
                LandmassStageKinds.All[0],
                Is.SameAs(LandmassStageKinds.ContinentalLandmass));
        }

        [Test]
        public void ContinentalLandmass_UsesExpectedSymbol()
        {
            StageKind stageKind = LandmassStageKinds.ContinentalLandmass;

            Assert.That(
                stageKind.Symbol.Value,
                Is.EqualTo("lokrain.atlas.landmass.stage_kind.continental_landmass"));
        }

        [Test]
        public void All_DoesNotContainDuplicateStageKindSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (StageKind stageKind in LandmassStageKinds.All)
            {
                Assert.That(
                    symbols.Add(stageKind.Symbol.Value),
                    Is.True,
                    $"Duplicate stage kind symbol: {stageKind.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassStageKinds.All,
                Is.InstanceOf<ICollection<StageKind>>());

            ICollection<StageKind> stageKinds =
                (ICollection<StageKind>)LandmassStageKinds.All;

            Assert.That(stageKinds.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => stageKinds.Add(LandmassStageKinds.ContinentalLandmass));

            Assert.Throws<NotSupportedException>(
                stageKinds.Clear);
        }
    }
}