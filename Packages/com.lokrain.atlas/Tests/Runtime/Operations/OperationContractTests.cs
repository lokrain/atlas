#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Operations.Tests
{
    public sealed class OperationContractTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesOperationContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var operationContract = new OperationContract(
                operationDefinition,
                new[]
                {
                    inputResource
                },
                new[]
                {
                    outputResource
                });

            Assert.That(operationContract.OperationDefinition, Is.SameAs(operationDefinition));
            Assert.That(operationContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.RequiredInputs[0], Is.SameAs(inputResource));
            Assert.That(operationContract.ProducedOutputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.ProducedOutputs[0], Is.SameAs(outputResource));
        }

        [Test]
        public void Constructor_WithNullOperationDefinition_ThrowsArgumentNullException()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentNullException>(
                () => new OperationContract(
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
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentNullException>(
                () => new OperationContract(
                    operationDefinition,
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
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            Assert.Throws<ArgumentNullException>(
                () => new OperationContract(
                    operationDefinition,
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
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
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
            OperationDefinition operationDefinition = CreateOperation(schema);

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
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
            OperationDefinition operationDefinition = CreateOperation(schema);

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
                () => new OperationContract(
                    operationDefinition,
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
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition firstOutputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            ResourceDefinition duplicateOutputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Duplicate Output Resource");

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
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
            GenerationSchemaDefinition operationSchema = CreateSchema();
            GenerationSchemaDefinition resourceSchema = CreateAlternativeSchema();
            OperationDefinition operationDefinition = CreateOperation(operationSchema);

            ResourceDefinition inputResource = CreateResource(
                resourceSchema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            ResourceDefinition outputResource = CreateResource(
                operationSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
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
            GenerationSchemaDefinition operationSchema = CreateSchema();
            GenerationSchemaDefinition resourceSchema = CreateAlternativeSchema();
            OperationDefinition operationDefinition = CreateOperation(operationSchema);

            ResourceDefinition outputResource = CreateResource(
                resourceSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
                    Array.Empty<ResourceDefinition>(),
                    new[]
                    {
                        outputResource
                    }));
        }

        [Test]
        public void Constructor_WithNoRequiredInputsAndNoProducedOutputs_ThrowsArgumentException()
        {
            OperationDefinition operationDefinition = CreateOperation(CreateSchema());

            Assert.Throws<ArgumentException>(
                () => new OperationContract(
                    operationDefinition,
                    Array.Empty<ResourceDefinition>(),
                    Array.Empty<ResourceDefinition>()));
        }

        [Test]
        public void Constructor_WithOnlyRequiredInputs_CreatesOperationContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition inputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.input",
                "Input Resource");

            var operationContract = new OperationContract(
                operationDefinition,
                new[]
                {
                    inputResource
                },
                Array.Empty<ResourceDefinition>());

            Assert.That(operationContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.RequiredInputs[0], Is.SameAs(inputResource));
            Assert.That(operationContract.ProducedOutputs, Is.Empty);
        }

        [Test]
        public void Constructor_WithOnlyProducedOutputs_CreatesOperationContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition outputResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var operationContract = new OperationContract(
                operationDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    outputResource
                });

            Assert.That(operationContract.RequiredInputs, Is.Empty);
            Assert.That(operationContract.ProducedOutputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.ProducedOutputs[0], Is.SameAs(outputResource));
        }

        [Test]
        public void Constructor_WithSameResourceAsRequiredInputAndProducedOutput_CreatesOperationContract()
        {
            GenerationSchemaDefinition schema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(schema);

            ResourceDefinition resource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.mask",
                "Mask Resource");

            var operationContract = new OperationContract(
                operationDefinition,
                new[]
                {
                    resource
                },
                new[]
                {
                    resource
                });

            Assert.That(operationContract.RequiredInputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.ProducedOutputs, Has.Count.EqualTo(1));
            Assert.That(operationContract.RequiredInputs[0], Is.SameAs(resource));
            Assert.That(operationContract.ProducedOutputs[0], Is.SameAs(resource));
        }

        [Test]
        public void Constructor_WithResourceUsingEquivalentGenerationSchemaSymbol_CreatesOperationContract()
        {
            GenerationSchemaDefinition operationSchema = CreateSchema();
            GenerationSchemaDefinition equivalentResourceSchema = CreateSchema();
            OperationDefinition operationDefinition = CreateOperation(operationSchema);

            ResourceDefinition outputResource = CreateResource(
                equivalentResourceSchema,
                "lokrain.atlas.tests.resource.output",
                "Output Resource");

            var operationContract = new OperationContract(
                operationDefinition,
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    outputResource
                });

            Assert.That(operationContract.ProducedOutputs[0], Is.SameAs(outputResource));
        }

        [Test]
        public void Equals_WithSameOperationDefinitionSymbol_ReturnsTrue()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            OperationContract left = new(
                CreateOperation(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.left_output",
                        "Left Output Resource")
                });

            OperationContract right = new(
                CreateOperation(schema),
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
        public void Equals_WithDifferentOperationDefinitionSymbol_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            OperationContract left = new(
                CreateOperation(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            OperationDefinition alternativeOperation = new(
                schema,
                OperationKind.Create("lokrain.atlas.tests.operation_kind.alternative"),
                Symbol.Create("lokrain.atlas.tests.operation.alternative"),
                DisplayName.Create("Alternative Operation"));

            OperationContract right = new(
                alternativeOperation,
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

            OperationContract operationContract = new(
                CreateOperation(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            Assert.That(operationContract.Equals("OperationContract"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            OperationContract? left = null;
            OperationContract? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            OperationContract? left = new(
                CreateOperation(schema),
                Array.Empty<ResourceDefinition>(),
                new[]
                {
                    CreateResource(
                        schema,
                        "lokrain.atlas.tests.resource.output",
                        "Output Resource")
                });

            OperationContract? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsOperationContractSummary()
        {
            GenerationSchemaDefinition schema = CreateSchema();

            OperationContract operationContract = new(
                CreateOperation(schema),
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

            string value = operationContract.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "OperationContract(OperationDefinition: lokrain.atlas.tests.operation.generate_height, RequiredInputs: 1, ProducedOutputs: 1)"));
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

        private static OperationDefinition CreateOperation(GenerationSchemaDefinition schema)
        {
            return new OperationDefinition(
                schema,
                OperationKind.Create("lokrain.atlas.tests.operation_kind.height"),
                Symbol.Create("lokrain.atlas.tests.operation.generate_height"),
                DisplayName.Create("Generate Height Operation"));
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