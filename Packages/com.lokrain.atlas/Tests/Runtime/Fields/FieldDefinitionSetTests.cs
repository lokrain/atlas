#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Fields.Tests
{
    public sealed class FieldDefinitionSetTests
    {
        [Test]
        public void Constructor_WithValidFieldDefinitions_CreatesFieldDefinitionSetInCanonicalSymbolOrder()
        {
            FieldDefinition moistureField = CreateFieldDefinition(
                "moisture",
                "moisture_resource",
                "Moisture Field",
                "Moisture Resource");

            FieldDefinition heightField = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    moistureField,
                    heightField
                });

            Assert.That(fieldDefinitionSet.FieldDefinitions, Has.Count.EqualTo(2));
            Assert.That(fieldDefinitionSet.FieldDefinitions[0], Is.SameAs(heightField));
            Assert.That(fieldDefinitionSet.FieldDefinitions[1], Is.SameAs(moistureField));
        }

        [Test]
        public void Constructor_WithSameDefinitionsInDifferentOrder_ProducesSamePublicOrder()
        {
            FieldDefinition heightField = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinition moistureField = CreateFieldDefinition(
                "moisture",
                "moisture_resource",
                "Moisture Field",
                "Moisture Resource");

            FieldDefinitionSet first = new(
                new[]
                {
                    heightField,
                    moistureField
                });

            FieldDefinitionSet second = new(
                new[]
                {
                    moistureField,
                    heightField
                });

            Assert.That(first.FieldDefinitions, Is.EqualTo(second.FieldDefinitions));
        }

        [Test]
        public void Constructor_WithEmptyFieldDefinitions_CreatesEmptyFieldDefinitionSet()
        {
            FieldDefinitionSet fieldDefinitionSet = new(Array.Empty<FieldDefinition>());

            Assert.That(fieldDefinitionSet.FieldDefinitions, Is.Empty);
        }

        [Test]
        public void Constructor_WithNullFieldDefinitions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FieldDefinitionSet(null!));
        }

        [Test]
        public void Constructor_WithNullEntry_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new FieldDefinition?[]
                    {
                        CreateFieldDefinition(
                            "height",
                            "height_resource",
                            "Height Field",
                            "Height Resource"),
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateFieldSymbol_ThrowsArgumentException()
        {
            FieldDefinition first = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinition second = CreateFieldDefinition(
                "height",
                "moisture_resource",
                "Duplicate Height Field",
                "Moisture Resource");

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        first,
                        second
                    }));
        }

        [Test]
        public void Constructor_WithDuplicateResourceDefinitionSymbol_ThrowsArgumentException()
        {
            FieldDefinition first = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinition second = CreateFieldDefinition(
                "height_alias",
                "height_resource",
                "Height Alias Field",
                "Duplicate Height Resource");

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        first,
                        second
                    }));
        }

        [Test]
        public void Constructor_WithUnknownShape_ThrowsArgumentException()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource",
                FieldShape.Unknown,
                FieldValueKind.Single);

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        fieldDefinition
                    }));
        }

        [Test]
        public void Constructor_WithUnsupportedShape_ThrowsArgumentException()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource",
                (FieldShape)999,
                FieldValueKind.Single);

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        fieldDefinition
                    }));
        }

        [Test]
        public void Constructor_WithUnknownValueKind_ThrowsArgumentException()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource",
                FieldShape.Grid,
                FieldValueKind.Unknown);

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        fieldDefinition
                    }));
        }

        [Test]
        public void Constructor_WithUnsupportedValueKind_ThrowsArgumentException()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource",
                FieldShape.Grid,
                (FieldValueKind)999);

            Assert.Throws<ArgumentException>(
                () => new FieldDefinitionSet(
                    new[]
                    {
                        fieldDefinition
                    }));
        }

        [Test]
        public void Constructor_CopiesFieldDefinitions()
        {
            FieldDefinition heightField = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinition moistureField = CreateFieldDefinition(
                "moisture",
                "moisture_resource",
                "Moisture Field",
                "Moisture Resource");

            var fieldDefinitions = new List<FieldDefinition>
            {
                heightField
            };

            FieldDefinitionSet fieldDefinitionSet = new(fieldDefinitions);

            fieldDefinitions.Add(moistureField);

            Assert.That(fieldDefinitionSet.FieldDefinitions, Has.Count.EqualTo(1));
            Assert.That(fieldDefinitionSet.FieldDefinitions[0], Is.SameAs(heightField));
        }

        [Test]
        public void ContainsFieldDefinition_WithKnownSymbol_ReturnsTrue()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            Assert.That(fieldDefinitionSet.ContainsFieldDefinition(fieldDefinition.Symbol), Is.True);
        }

        [Test]
        public void ContainsFieldDefinition_WithUnknownSymbol_ReturnsFalse()
        {
            FieldDefinitionSet fieldDefinitionSet = new(Array.Empty<FieldDefinition>());

            Assert.That(
                fieldDefinitionSet.ContainsFieldDefinition(
                    Symbol.Create("lokrain.atlas.tests.field.unknown")),
                Is.False);
        }

        [Test]
        public void ContainsFieldDefinition_WithNullSymbol_ThrowsArgumentNullException()
        {
            FieldDefinitionSet fieldDefinitionSet = new(Array.Empty<FieldDefinition>());

            Assert.Throws<ArgumentNullException>(
                () => fieldDefinitionSet.ContainsFieldDefinition(null!));
        }

        [Test]
        public void GetFieldDefinition_WithKnownSymbol_ReturnsFieldDefinition()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            Assert.That(
                fieldDefinitionSet.GetFieldDefinition(fieldDefinition.Symbol),
                Is.SameAs(fieldDefinition));
        }

        [Test]
        public void GetFieldDefinition_WithUnknownSymbol_ThrowsKeyNotFoundException()
        {
            FieldDefinitionSet fieldDefinitionSet = new(Array.Empty<FieldDefinition>());

            Assert.Throws<KeyNotFoundException>(
                () => fieldDefinitionSet.GetFieldDefinition(
                    Symbol.Create("lokrain.atlas.tests.field.unknown")));
        }

        [Test]
        public void TryGetFieldDefinition_WithKnownSymbol_ReturnsTrueAndFieldDefinition()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            bool result = fieldDefinitionSet.TryGetFieldDefinition(
                fieldDefinition.Symbol,
                out FieldDefinition? resolvedFieldDefinition);

            Assert.That(result, Is.True);
            Assert.That(resolvedFieldDefinition, Is.SameAs(fieldDefinition));
        }

        [Test]
        public void TryGetFieldDefinition_WithUnknownSymbol_ReturnsFalseAndNull()
        {
            FieldDefinitionSet fieldDefinitionSet = new(Array.Empty<FieldDefinition>());

            bool result = fieldDefinitionSet.TryGetFieldDefinition(
                Symbol.Create("lokrain.atlas.tests.field.unknown"),
                out FieldDefinition? fieldDefinition);

            Assert.That(result, Is.False);
            Assert.That(fieldDefinition, Is.Null);
        }

        [Test]
        public void ContainsFieldDefinitionForResourceDefinition_WithKnownSymbol_ReturnsTrue()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            Assert.That(
                fieldDefinitionSet.ContainsFieldDefinitionForResourceDefinition(
                    fieldDefinition.ResourceDefinition.Symbol),
                Is.True);
        }

        [Test]
        public void GetFieldDefinitionForResourceDefinition_WithKnownSymbol_ReturnsFieldDefinition()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            Assert.That(
                fieldDefinitionSet.GetFieldDefinitionForResourceDefinition(
                    fieldDefinition.ResourceDefinition.Symbol),
                Is.SameAs(fieldDefinition));
        }

        [Test]
        public void TryGetFieldDefinitionForResourceDefinition_WithKnownSymbol_ReturnsTrueAndFieldDefinition()
        {
            FieldDefinition fieldDefinition = CreateFieldDefinition(
                "height",
                "height_resource",
                "Height Field",
                "Height Resource");

            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    fieldDefinition
                });

            bool result = fieldDefinitionSet.TryGetFieldDefinitionForResourceDefinition(
                fieldDefinition.ResourceDefinition.Symbol,
                out FieldDefinition? resolvedFieldDefinition);

            Assert.That(result, Is.True);
            Assert.That(resolvedFieldDefinition, Is.SameAs(fieldDefinition));
        }

        [Test]
        public void ToString_ReturnsFieldDefinitionSetSummary()
        {
            FieldDefinitionSet fieldDefinitionSet = new(
                new[]
                {
                    CreateFieldDefinition(
                        "height",
                        "height_resource",
                        "Height Field",
                        "Height Resource"),
                    CreateFieldDefinition(
                        "moisture",
                        "moisture_resource",
                        "Moisture Field",
                        "Moisture Resource")
                });

            string value = fieldDefinitionSet.ToString();

            Assert.That(value, Is.EqualTo("FieldDefinitionSet(FieldDefinitions: 2)"));
        }

        private static FieldDefinition CreateFieldDefinition(
            string fieldName,
            string resourceName,
            string fieldDisplayName,
            string resourceDisplayName,
            FieldShape shape = FieldShape.Grid,
            FieldValueKind valueKind = FieldValueKind.Single)
        {
            return new FieldDefinition(
                CreateResourceDefinition(resourceName, resourceDisplayName),
                Symbol.Create($"lokrain.atlas.tests.field.{fieldName}"),
                DisplayName.Create(fieldDisplayName),
                shape,
                valueKind);
        }

        private static ResourceDefinition CreateResourceDefinition(
            string resourceName,
            string resourceDisplayName)
        {
            return new ResourceDefinition(
                Symbol.Create($"lokrain.atlas.tests.resource.{resourceName}"),
                DisplayName.Create(resourceDisplayName),
                CreateSchema());
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }
    }
}
