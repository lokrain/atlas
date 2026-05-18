#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassResourceDefinitionsTests
    {
        [Test]
        public void All_ReturnsResourceDefinitionsInDeclaredOrder()
        {
            Assert.That(LandmassResourceDefinitions.All, Has.Count.EqualTo(5));

            Assert.That(
                LandmassResourceDefinitions.All[0],
                Is.SameAs(LandmassResourceDefinitions.ContinentSuitability));

            Assert.That(
                LandmassResourceDefinitions.All[1],
                Is.SameAs(LandmassResourceDefinitions.ContinentCandidate));

            Assert.That(
                LandmassResourceDefinitions.All[2],
                Is.SameAs(LandmassResourceDefinitions.MainContinent));

            Assert.That(
                LandmassResourceDefinitions.All[3],
                Is.SameAs(LandmassResourceDefinitions.ContinentalLandmassArea));

            Assert.That(
                LandmassResourceDefinitions.All[4],
                Is.SameAs(LandmassResourceDefinitions.BaseElevation));
        }

        [Test]
        public void Definitions_UseExpectedSymbolsDisplayNamesAndGenerationSchema()
        {
            AssertResourceDefinition(
                LandmassResourceDefinitions.ContinentSuitability,
                "lokrain.atlas.landmass.resource.continent_suitability",
                "Continent Suitability");

            AssertResourceDefinition(
                LandmassResourceDefinitions.ContinentCandidate,
                "lokrain.atlas.landmass.resource.continent_candidate",
                "Continent Candidate");

            AssertResourceDefinition(
                LandmassResourceDefinitions.MainContinent,
                "lokrain.atlas.landmass.resource.main_continent",
                "Main Continent");

            AssertResourceDefinition(
                LandmassResourceDefinitions.ContinentalLandmassArea,
                "lokrain.atlas.landmass.resource.continental_landmass_area",
                "Continental Landmass Area");

            AssertResourceDefinition(
                LandmassResourceDefinitions.BaseElevation,
                "lokrain.atlas.landmass.resource.base_elevation",
                "Base Elevation");
        }

        [Test]
        public void All_DoesNotContainDuplicateResourceSymbols()
        {
            var symbols = new HashSet<string>(StringComparer.Ordinal);

            foreach (ResourceDefinition resourceDefinition in LandmassResourceDefinitions.All)
            {
                Assert.That(
                    symbols.Add(resourceDefinition.Symbol.Value),
                    Is.True,
                    $"Duplicate resource symbol: {resourceDefinition.Symbol.Value}");
            }
        }

        [Test]
        public void All_AsMutableCollection_IsReadOnlyAndRejectsMutation()
        {
            Assert.That(
                LandmassResourceDefinitions.All,
                Is.InstanceOf<ICollection<ResourceDefinition>>());

            ICollection<ResourceDefinition> resourceDefinitions =
                (ICollection<ResourceDefinition>)LandmassResourceDefinitions.All;

            Assert.That(resourceDefinitions.IsReadOnly, Is.True);

            Assert.Throws<NotSupportedException>(
                () => resourceDefinitions.Add(LandmassResourceDefinitions.BaseElevation));

            Assert.Throws<NotSupportedException>(
                () => resourceDefinitions.Clear());
        }

        private static void AssertResourceDefinition(
            ResourceDefinition resourceDefinition,
            string expectedSymbolValue,
            string expectedDisplayNameValue)
        {
            Assert.That(resourceDefinition.Symbol.Value, Is.EqualTo(expectedSymbolValue));
            Assert.That(resourceDefinition.DisplayName.Value, Is.EqualTo(expectedDisplayNameValue));
            Assert.That(resourceDefinition.GenerationSchema, Is.SameAs(BuiltInGenerationSchemas.World));
        }
    }
}