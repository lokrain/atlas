#nullable enable

using Lokrain.Atlas.Fields;
using NUnit.Framework;

namespace Lokrain.Atlas.Generation.Landmass.Tests
{
    public sealed class LandmassFieldDefinitionSetTests
    {
        [Test]
        public void Default_ReturnsFieldDefinitionSet()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            Assert.That(fieldDefinitionSet, Is.Not.Null);
        }

        [Test]
        public void Default_ContainsAllLandmassFieldDefinitionsInCanonicalSymbolOrder()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            Assert.That(
                fieldDefinitionSet.FieldDefinitions,
                Is.EqualTo(new[]
                {
                    LandmassFieldDefinitions.BaseElevation,
                    LandmassFieldDefinitions.ContinentCandidate,
                    LandmassFieldDefinitions.ContinentSuitability,
                    LandmassFieldDefinitions.ContinentalLandmassArea,
                    LandmassFieldDefinitions.MainContinent
                }));
        }

        [Test]
        public void Default_ContainsFieldsForAllLandmassFieldDefinitions()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(
                    fieldDefinitionSet.ContainsFieldDefinition(fieldDefinition.Symbol),
                    Is.True);

                Assert.That(
                    fieldDefinitionSet.GetFieldDefinition(fieldDefinition.Symbol),
                    Is.SameAs(fieldDefinition));
            }
        }

        [Test]
        public void Default_ContainsFieldsForAllLandmassResourceDefinitions()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(
                    fieldDefinitionSet.ContainsFieldDefinitionForResourceDefinition(
                        fieldDefinition.ResourceDefinition.Symbol),
                    Is.True);

                Assert.That(
                    fieldDefinitionSet.GetFieldDefinitionForResourceDefinition(
                        fieldDefinition.ResourceDefinition.Symbol),
                    Is.SameAs(fieldDefinition));
            }
        }

        [Test]
        public void Default_ToString_ReturnsExpectedSummary()
        {
            string value = LandmassFieldDefinitionSet.Default.ToString();

            Assert.That(value, Is.EqualTo("FieldDefinitionSet(FieldDefinitions: 5)"));
        }
    }
}
