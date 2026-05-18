#nullable enable

using System;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Fields;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using NUnit.Framework;

namespace Lokrain.Atlas.Execution.Tests
{
    public sealed class ResourceFieldBindingTests
    {
        [Test]
        public void Constructor_WithNullResourceDefinition_ThrowsArgumentNullException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition: null!,
                    fieldDefinition,
                    FieldPlanRole.RequiredInput,
                    FieldCapturePolicy.DoNotCapture),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("resourceDefinition"));
        }

        [Test]
        public void Constructor_WithNullFieldDefinition_ThrowsArgumentNullException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition,
                    fieldDefinition: null!,
                    FieldPlanRole.RequiredInput,
                    FieldCapturePolicy.DoNotCapture),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fieldDefinition"));
        }

        [Test]
        public void Constructor_WithUnknownPlanRole_ThrowsArgumentException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition,
                    fieldDefinition,
                    FieldPlanRole.Unknown,
                    FieldCapturePolicy.DoNotCapture),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("planRole"));
        }

        [Test]
        public void Constructor_WithUnsupportedPlanRole_ThrowsArgumentException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition,
                    fieldDefinition,
                    (FieldPlanRole)99,
                    FieldCapturePolicy.DoNotCapture),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("planRole"));
        }

        [Test]
        public void Constructor_WithUnknownCapturePolicy_ThrowsArgumentException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition,
                    fieldDefinition,
                    FieldPlanRole.RequiredInput,
                    FieldCapturePolicy.Unknown),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("capturePolicy"));
        }

        [Test]
        public void Constructor_WithUnsupportedCapturePolicy_ThrowsArgumentException()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    resourceDefinition,
                    fieldDefinition,
                    FieldPlanRole.RequiredInput,
                    (FieldCapturePolicy)99),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("capturePolicy"));
        }

        [Test]
        public void Constructor_WithFieldDefinitionResourceDifferentInstanceButSameSymbol_ThrowsArgumentException()
        {
            ResourceDefinition bindingResource = CreateResourceDefinition("atlas.resource.base_elevation");
            ResourceDefinition symbolEquivalentDifferentInstance = CreateResourceDefinition("atlas.resource.base_elevation");

            Assert.That(bindingResource == symbolEquivalentDifferentInstance, Is.True);
            Assert.That(ReferenceEquals(bindingResource, symbolEquivalentDifferentInstance), Is.False);

            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                symbolEquivalentDifferentInstance);

            Assert.That(
                () => new ResourceFieldBinding(
                    new FieldIndex(0),
                    bindingResource,
                    fieldDefinition,
                    FieldPlanRole.RequiredInput,
                    FieldCapturePolicy.DoNotCapture),
                Throws.TypeOf<ArgumentException>()
                    .With.Property(nameof(ArgumentException.ParamName)).EqualTo("fieldDefinition"));
        }

        [Test]
        public void Constructor_WithValidArguments_SetsProperties()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var binding = new ResourceFieldBinding(
                new FieldIndex(7),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInputAndProducedOutput,
                FieldCapturePolicy.Capture);

            Assert.That(binding.FieldIndex, Is.EqualTo(new FieldIndex(7)));
            Assert.That(binding.ResourceDefinition, Is.SameAs(resourceDefinition));
            Assert.That(binding.FieldDefinition, Is.SameAs(fieldDefinition));
            Assert.That(binding.PlanRole, Is.EqualTo(FieldPlanRole.RequiredInputAndProducedOutput));
            Assert.That(binding.CapturePolicy, Is.EqualTo(FieldCapturePolicy.Capture));
        }

        [Test]
        public void Equals_WithSameValuesAndReferences_ReturnsTrue()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var left = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            var right = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var binding = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(binding.Equals(null), Is.False);
            Assert.That(binding == null, Is.False);
            Assert.That(binding != null, Is.True);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var binding = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(binding.Equals("not a binding"), Is.False);
        }

        [Test]
        public void Operators_WithBothNull_ReturnEqual()
        {
            ResourceFieldBinding? left = null;
            ResourceFieldBinding? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void Equals_WithDifferentFieldIndex_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var left = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            var right = new ResourceFieldBinding(
                new FieldIndex(2),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void Equals_WithSymbolEquivalentButReferenceDifferentResourceDefinitions_ReturnsFalse()
        {
            ResourceDefinition resourceA = CreateResourceDefinition("atlas.resource.base_elevation");
            ResourceDefinition resourceB = CreateResourceDefinition("atlas.resource.base_elevation");

            Assert.That(resourceA == resourceB, Is.True);
            Assert.That(ReferenceEquals(resourceA, resourceB), Is.False);

            FieldDefinition fieldA = CreateFieldDefinitionForResource("atlas.field.base_elevation_a", resourceA);
            FieldDefinition fieldB = CreateFieldDefinitionForResource("atlas.field.base_elevation_b", resourceB);

            var left = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceA,
                fieldA,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            var right = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceB,
                fieldB,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithSymbolEquivalentButReferenceDifferentFieldDefinitions_ReturnsFalse()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");

            FieldDefinition fieldA = CreateFieldDefinitionForResource("atlas.field.base_elevation", resourceDefinition);
            FieldDefinition fieldB = CreateFieldDefinitionForResource("atlas.field.base_elevation", resourceDefinition);

            Assert.That(fieldA == fieldB, Is.True);
            Assert.That(ReferenceEquals(fieldA, fieldB), Is.False);

            var left = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldA,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            var right = new ResourceFieldBinding(
                new FieldIndex(1),
                resourceDefinition,
                fieldB,
                FieldPlanRole.RequiredInput,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(left.Equals(right), Is.False);
        }

        [TestCase(FieldPlanRole.RequiredInput, FieldPlanRole.ProducedOutput)]
        [TestCase(FieldPlanRole.RequiredInput, FieldPlanRole.RequiredInputAndProducedOutput)]
        [TestCase(FieldPlanRole.ProducedOutput, FieldPlanRole.RequiredInputAndProducedOutput)]
        public void Equals_WithDifferentPlanRole_ReturnsFalse(
            FieldPlanRole leftRole,
            FieldPlanRole rightRole)
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var left = new ResourceFieldBinding(
                new FieldIndex(2),
                resourceDefinition,
                fieldDefinition,
                leftRole,
                FieldCapturePolicy.DoNotCapture);

            var right = new ResourceFieldBinding(
                new FieldIndex(2),
                resourceDefinition,
                fieldDefinition,
                rightRole,
                FieldCapturePolicy.DoNotCapture);

            Assert.That(left.Equals(right), Is.False);
        }

        [TestCase(FieldCapturePolicy.DoNotCapture, FieldCapturePolicy.Capture)]
        public void Equals_WithDifferentCapturePolicy_ReturnsFalse(
            FieldCapturePolicy leftPolicy,
            FieldCapturePolicy rightPolicy)
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var left = new ResourceFieldBinding(
                new FieldIndex(2),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                leftPolicy);

            var right = new ResourceFieldBinding(
                new FieldIndex(2),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.RequiredInput,
                rightPolicy);

            Assert.That(left.Equals(right), Is.False);
        }

        [Test]
        public void ToString_ContainsHighSignalIdentityData()
        {
            ResourceDefinition resourceDefinition = CreateResourceDefinition("atlas.resource.base_elevation");
            FieldDefinition fieldDefinition = CreateFieldDefinitionForResource(
                "atlas.field.base_elevation",
                resourceDefinition);

            var binding = new ResourceFieldBinding(
                new FieldIndex(5),
                resourceDefinition,
                fieldDefinition,
                FieldPlanRole.ProducedOutput,
                FieldCapturePolicy.Capture);

            string text = binding.ToString();

            Assert.That(text, Does.Contain(nameof(ResourceFieldBinding)));
            Assert.That(text, Does.Contain(nameof(ResourceFieldBinding.FieldIndex)));
            Assert.That(text, Does.Contain(resourceDefinition.Symbol.ToString()));
            Assert.That(text, Does.Contain(fieldDefinition.Symbol.ToString()));
            Assert.That(text, Does.Contain(FieldPlanRole.ProducedOutput.ToString()));
            Assert.That(text, Does.Contain(FieldCapturePolicy.Capture.ToString()));
        }

        private static ResourceDefinition CreateResourceDefinition(string symbolText)
        {
            GenerationSchemaDefinition schema = new(
                Symbol.Create("atlas.schema.test"),
                DisplayName.Create("Test Schema"));

            return new ResourceDefinition(
                Symbol.Create(symbolText),
                DisplayName.Create(symbolText),
                schema);
        }

        private static FieldDefinition CreateFieldDefinitionForResource(
            string fieldSymbolText,
            ResourceDefinition resourceDefinition)
        {
            return new FieldDefinition(
                resourceDefinition,
                Symbol.Create(fieldSymbolText),
                DisplayName.Create(fieldSymbolText),
                FieldShape.Grid,
                FieldValueKind.Single);
        }
    }
}