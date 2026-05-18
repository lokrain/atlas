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
        public void Default_ContainsFieldsForAllLandmassFieldDefinitionSymbols()
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

                Assert.That(
                    fieldDefinitionSet.TryGetFieldDefinition(
                        fieldDefinition.Symbol,
                        out FieldDefinition? resolvedFieldDefinition),
                    Is.True);

                Assert.That(resolvedFieldDefinition, Is.SameAs(fieldDefinition));
            }
        }

        [Test]
        public void Default_ContainsFieldsForAllLandmassResourceDefinitionSymbols()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(
                    fieldDefinitionSet.ContainsFieldDefinitionForResourceDefinitionSymbol(
                        fieldDefinition.ResourceDefinition.Symbol),
                    Is.True);

                Assert.That(
                    fieldDefinitionSet.GetFieldDefinitionForResourceDefinitionSymbol(
                        fieldDefinition.ResourceDefinition.Symbol),
                    Is.SameAs(fieldDefinition));

                Assert.That(
                    fieldDefinitionSet.TryGetFieldDefinitionForResourceDefinitionSymbol(
                        fieldDefinition.ResourceDefinition.Symbol,
                        out FieldDefinition? resolvedFieldDefinition),
                    Is.True);

                Assert.That(resolvedFieldDefinition, Is.SameAs(fieldDefinition));
            }
        }

        [Test]
        public void Default_ContainsFieldsForAllExactLandmassResourceDefinitions()
        {
            FieldDefinitionSet fieldDefinitionSet = LandmassFieldDefinitionSet.Default;

            foreach (FieldDefinition fieldDefinition in LandmassFieldDefinitions.All)
            {
                Assert.That(
                    fieldDefinitionSet.ContainsFieldDefinitionForResourceDefinition(
                        fieldDefinition.ResourceDefinition),
                    Is.True);

                Assert.That(
                    fieldDefinitionSet.GetFieldDefinitionForResourceDefinition(
                        fieldDefinition.ResourceDefinition),
                    Is.SameAs(fieldDefinition));

                Assert.That(
                    fieldDefinitionSet.TryGetFieldDefinitionForResourceDefinition(
                        fieldDefinition.ResourceDefinition,
                        out FieldDefinition? resolvedFieldDefinition),
                    Is.True);

                Assert.That(resolvedFieldDefinition, Is.SameAs(fieldDefinition));
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