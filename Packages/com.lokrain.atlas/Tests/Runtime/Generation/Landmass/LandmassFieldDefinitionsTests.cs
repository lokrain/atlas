#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Fields;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassFieldDefinitionsTests
    {
        [Test]
        public void All_ReturnsExpectedFieldDefinitionsInDeclaredOrder()
        {
            Assert.That(
                LandmassFieldDefinitions.All,
                Is.EqualTo(new[]
                {
                    LandmassFieldDefinitions.ContinentSuitability,
                    LandmassFieldDefinitions.ContinentCandidate,
                    LandmassFieldDefinitions.MainContinent,
                    LandmassFieldDefinitions.ContinentalLandmassArea,
                    LandmassFieldDefinitions.BaseElevation
                }));
        }

        [Test]
        public void All_ReturnsFiveFieldDefinitions()
        {
            Assert.That(LandmassFieldDefinitions.All, Has.Count.EqualTo(5));
        }

        [Test]
        public void All_IsReadOnly()
        {
            var fieldDefinitions = (ICollection<FieldDefinition>)LandmassFieldDefinitions.All;

            Assert.That(fieldDefinitions.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(
                () => fieldDefinitions.Add(LandmassFieldDefinitions.BaseElevation));
        }

        [Test]
        public void ContinentSuitability_HasExpectedMetadata()
        {
            AssertFieldDefinition(
                LandmassFieldDefinitions.ContinentSuitability,
                LandmassResourceDefinitions.ContinentSuitability,
                "lokrain.atlas.landmass.field.continent_suitability",
                "Continent Suitability Field",
                FieldShape.Grid,
                FieldValueKind.Single);
        }

        [Test]
        public void ContinentCandidate_HasExpectedMetadata()
        {
            AssertFieldDefinition(
                LandmassFieldDefinitions.ContinentCandidate,
                LandmassResourceDefinitions.ContinentCandidate,
                "lokrain.atlas.landmass.field.continent_candidate",
                "Continent Candidate Field",
                FieldShape.Grid,
                FieldValueKind.Boolean);
        }

        [Test]
        public void MainContinent_HasExpectedMetadata()
        {
            AssertFieldDefinition(
                LandmassFieldDefinitions.MainContinent,
                LandmassResourceDefinitions.MainContinent,
                "lokrain.atlas.landmass.field.main_continent",
                "Main Continent Field",
                FieldShape.Grid,
                FieldValueKind.Boolean);
        }

        [Test]
        public void ContinentalLandmassArea_HasExpectedMetadata()
        {
            AssertFieldDefinition(
                LandmassFieldDefinitions.ContinentalLandmassArea,
                LandmassResourceDefinitions.ContinentalLandmassArea,
                "lokrain.atlas.landmass.field.continental_landmass_area",
                "Continental Landmass Area Field",
                FieldShape.Grid,
                FieldValueKind.Boolean);
        }

        [Test]
        public void BaseElevation_HasExpectedMetadata()
        {
            AssertFieldDefinition(
                LandmassFieldDefinitions.BaseElevation,
                LandmassResourceDefinitions.BaseElevation,
                "lokrain.atlas.landmass.field.base_elevation",
                "Base Elevation Field",
                FieldShape.Grid,
                FieldValueKind.Single);
        }

        [Test]
        public void All_ContainsUniqueFieldSymbols()
        {
            var symbols = new HashSet<string>();

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(symbols.Add(fieldDefinition.Symbol.Value), Is.True);
            }
        }

        [Test]
        public void All_ContainsUniqueResourceDefinitions()
        {
            var resourceSymbols = new HashSet<string>();

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(resourceSymbols.Add(fieldDefinition.ResourceDefinition.Symbol.Value), Is.True);
            }
        }

        private static void AssertFieldDefinition(
            FieldDefinition fieldDefinition,
            Resources.ResourceDefinition expectedResourceDefinition,
            string expectedSymbol,
            string expectedDisplayName,
            FieldShape expectedShape,
            FieldValueKind expectedValueKind)
        {
            Assert.That(fieldDefinition.ResourceDefinition, Is.SameAs(expectedResourceDefinition));
            Assert.That(fieldDefinition.Symbol.Value, Is.EqualTo(expectedSymbol));
            Assert.That(fieldDefinition.DisplayName.Value, Is.EqualTo(expectedDisplayName));
            Assert.That(fieldDefinition.Shape, Is.EqualTo(expectedShape));
            Assert.That(fieldDefinition.ValueKind, Is.EqualTo(expectedValueKind));
        }
    }
}
