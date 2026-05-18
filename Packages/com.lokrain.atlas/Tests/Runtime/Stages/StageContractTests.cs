#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Stages.Tests
{
    public sealed class StageContractTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesStageContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");
            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var stageContract = new StageContract(
                stageDefinition,
                new[]
                {
                    inputResource
                },
                new[]
                {
                    outputResource
                });

            Assert.That(stageContract.StageDefinition, Is.SameAs(stageDefinition));
            Assert.That(stageContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(stageContract.RequiredInputs[0], Is.SameAs(inputResource));
            Assert.That(stageContract.ProducedOutputs, Has.Count.EqualTo(1));
            Assert.That(stageContract.ProducedOutputs[0], Is.SameAs(outputResource));
        }

        [Test]
        public void Constructor_WithNullStageDefinition_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentNullException>(
                () => new StageContract(
                    null!,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithNullRequiredInputs_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentNullException>(
                () => new StageContract(
                    stageDefinition,
                    null!,
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithNullProducedOutputs_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            Assert.Throws<ArgumentNullException>(
                () => new StageContract(
                    stageDefinition,
                    new[]
                    {
                        inputResource
                    },
                    null!));
        }

        [Test]
        public void Constructor_WithNullRequiredInputEntry_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    new ResourceDefinition?[]
                    {
                        null
                    }!,
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithNullProducedOutputEntry_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    Array.Empty<ResourceDefinition>(),
                    new ResourceDefinition?[]
                    {
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateRequiredInputResource_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);

            ResourceDefinition firstInputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            ResourceDefinition duplicateInputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Duplicate Input Resource");

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    new[]
                    {
                        firstInputResource,
                        duplicateInputResource
                    },
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithDuplicateProducedOutputResource_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);

            ResourceDefinition firstOutputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            ResourceDefinition duplicateOutputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Duplicate Output Resource");

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        firstOutputResource,
                        duplicateOutputResource
                    }));
        }

        [Test]
        public void Constructor_WithRequiredInputFromDifferentGenerationSchema_ThrowsArgumentException()
        {
            GenerationSchemaDefinition stageSchema = CreateSchema();
            GenerationSchemaDefinition resourceSchema = CreateAlternativeSchema();
            StageDefinition stageDefinition = CreateStage(stageSchema);

            ResourceDefinition inputResource = CreateResource(
                resourceSchema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            ResourceDefinition outputResource = CreateResource(
                stageSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    new[]
                    {
                        inputResource
                    },
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithProducedOutputFromDifferentGenerationSchema_ThrowsArgumentException()
        {
            GenerationSchemaDefinition stageSchema = CreateSchema();
            GenerationSchemaDefinition resourceSchema = CreateAlternativeSchema();
            StageDefinition stageDefinition = CreateStage(stageSchema);

            ResourceDefinition outputResource = CreateResource(
                resourceSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithNoRequiredInputsAndNoProducedOutputs_ThrowsArgumentException()
        {
            StageDefinition stageDefinition = CreateStage(CreateSchema());

            Assert.Throws<ArgumentException>(
                () => new StageContract(
                    stageDefinition,
                    Array.Empty<ResourceDefinition>(),
                    Array.Empty<ResourceDefinition>()));
        }

        [Test]
        public void Constructor_WithOnlyRequiredInputs_CreatesStageContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            var stageContract = new StageContract(
                stageDefinition,
                new[]
                {
                    inputResource
                },
                Array.Empty<ResourceDefinition>());

            Assert.That(stageContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(stageContract.ProducedOutputs, Is.Empty);
        }

        [Test]
        public void Constructor_WithOnlyProducedOutputs_CreatesStageContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var stageContract = new StageContract(
                stageDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    outputResource
                });

            Assert.That(stageContract.RequiredInputs, Is.Empty);
            Assert.That(stageContract.ProducedOutputs, Has.Count.EqualTo(1));
        }

        [Test]
        public void Constructor_WithSameResourceAsRequiredInputAndProducedOutput_CreatesStageContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(schema);
            ResourceDefinition resource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.mask",
                "Mask Resource");

            var stageContract = new StageContract(
                stageDefinition,
                new[]
                {
                    resource
                },
                new[]
                {
                    resource
                });

            Assert.That(stageContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(stageContract.ProducedOutputs, Has.Count.EqualTo(1));
            Assert.That(stageContract.RequiredInputs[0], Is.SameAs(resource));
            Assert.That(stageContract.ProducedOutputs[0], Is.SameAs(resource));
        }

        [Test]
        public void Constructor_WithResourceUsingEquivalentGenerationSchemaSymbol_CreatesStageContract()
        {
            GenerationSchemaDefinition stageSchema = CreateSchema();
            GenerationSchemaDefinition equivalentResourceSchema = CreateSchema();
            StageDefinition stageDefinition = CreateStage(stageSchema);

            ResourceDefinition outputResource = CreateResource(
                equivalentResourceSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var stageContract = new StageContract(
                stageDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    outputResource
                });

            Assert.That(stageContract.ProducedOutputs[0], Is.SameAs(outputResource));
        }

        [Test]
        public void Equals_WithSameStageDefinitionSymbol_ReturnsTrue()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageContract left = new(
                CreateStage(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.left_output",
                        "Left Output Resource")
                });

            StageContract right = new(
                CreateStage(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.right_output",
                        "Right Output Resource")
                });

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentStageDefinitionSymbol_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageContract left = new(
                CreateStage(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            StageDefinition alternativeStage = new(
                schema,
                StageKind.Create("lokrain.atlas.tests.stage_kind.alternative"),
                Symbol.Create("lokrain.atlas.tests.stage.alternative"),
                DisplayName.Create("Alternative Stage"));

            StageContract right = new(
                alternativeStage,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageContract stageContract = new(
                CreateStage(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            Assert.That(stageContract.Equals("StageContract"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            StageContract? left = null;
            StageContract? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageContract? left = new(
                CreateStage(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            StageContract? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsStageContractSummary()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            StageContract stageContract = new(
                CreateStage(schema),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.input",
                        "Input Resource")
                },
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            string value = stageContract.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "StageContract(StageDefinition: lokrain.atlas.tests.stage.continents, RequiredInputs: 1, ProducedOutputs: 1)"));
        }

        private static GenerationSchemaDefinition CreateSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.world"),
                DisplayName.Create("World Test Schema"));
        }

        private static GenerationSchemaDefinition CreateAlternativeSchema()
        {
            return new GenerationSchemaDefinition(
                Symbol.Create("lokrain.atlas.tests.schema.alternative"),
                DisplayName.Create("Alternative Test Schema"));
        }

        private static StageDefinition CreateStage(GenerationSchemaDefinition schema)
        {
            return new StageDefinition(
                schema,
                StageKind.Create("lokrain.atlas.tests.stage_kind.landmass"),
                Symbol.Create("lokrain.atlas.tests.stage.continents"),
                DisplayName.Create("Continents Stage"));
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbol,
            string displayName)
        {
            return new ResourceDefinition(
                Symbol.Create(symbol),
                DisplayName.Create(displayName),
                schema);
        }
    }
}