#nullable enable

using System;
using System.Collections.Generic;
using Lokrain.Atlas.Core;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Resources;
using Lokrain.Atlas.Schemas;
using Lokrain.Atlas.Stages;
using NUnit.Framework;

namespace Lokrain.Atlas.Recipes.Tests
{
    public sealed class GenerationRecipeDefinitionTests
    {
        [Test]
        public void Constructor_WithValidArguments_CreatesGenerationRecipeDefinition()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");
            Symbol symbol = Symbol.Create("lokrain.atlas.tests.recipe.landmass");
            DisplayName displayName = DisplayName.Create("Landmass Recipe");

            var recipe = new GenerationRecipeDefinition(
                symbol,
                displayName,
                graph.Schema,
                new[]
                {
                    graph.StageRouteChoice
                },
                new[]
                {
                    graph.StageRouteStepImplementationChoice
                });

            Assert.That(recipe.Symbol, Is.SameAs(symbol));
            Assert.That(recipe.DisplayName, Is.SameAs(displayName));
            Assert.That(recipe.GenerationSchemaDefinition, Is.SameAs(graph.Schema));
            Assert.That(recipe.StageRouteChoices, Has.Count.EqualTo(1));
            Assert.That(recipe.StageRouteChoices[0], Is.SameAs(graph.StageRouteChoice));
            Assert.That(recipe.StageRouteStepImplementationChoices, Has.Count.EqualTo(1));
            Assert.That(
                recipe.StageRouteStepImplementationChoices[0],
                Is.SameAs(graph.StageRouteStepImplementationChoice));
        }

        [Test]
        public void Constructor_WithNullSymbol_ThrowsArgumentNullException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRecipeDefinition(
                    null!,
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullDisplayName_ThrowsArgumentNullException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    null!,
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullGenerationSchemaDefinition_ThrowsArgumentNullException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    null!,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullStageRouteChoices_ThrowsArgumentNullException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    null!,
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepImplementationChoices_ThrowsArgumentNullException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentNullException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    null!));
        }

        [Test]
        public void Constructor_WithEmptyStageRouteChoices_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    Array.Empty<StageRouteChoice>(),
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullStageRouteChoice_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new StageRouteChoice?[]
                    {
                        graph.StageRouteChoice,
                        null
                    }!,
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithNullStageRouteStepImplementationChoice_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new StageRouteStepImplementationChoice?[]
                    {
                        graph.StageRouteStepImplementationChoice,
                        null
                    }!));
        }

        [Test]
        public void Constructor_WithDuplicateStageDefinitionSymbol_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            RecipeGraph left = CreateSingleStageGraph(
                "landmass",
                schema,
                stageSymbolValue: "lokrain.atlas.tests.stage.duplicate");

            RecipeGraph right = CreateSingleStageGraph(
                "climate",
                schema,
                stageSymbolValue: "lokrain.atlas.tests.stage.duplicate");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.duplicate_stage"),
                    DisplayName.Create("Duplicate Stage Recipe"),
                    schema,
                    new[]
                    {
                        left.StageRouteChoice,
                        right.StageRouteChoice
                    },
                    new[]
                    {
                        left.StageRouteStepImplementationChoice,
                        right.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithDuplicateStageRouteDefinitionSymbol_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            RecipeGraph left = CreateSingleStageGraph(
                "landmass",
                schema,
                stageRouteSymbolValue: "lokrain.atlas.tests.stage_route.duplicate");

            RecipeGraph right = CreateSingleStageGraph(
                "climate",
                schema,
                stageRouteSymbolValue: "lokrain.atlas.tests.stage_route.duplicate");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.duplicate_route"),
                    DisplayName.Create("Duplicate Route Recipe"),
                    schema,
                    new[]
                    {
                        left.StageRouteChoice,
                        right.StageRouteChoice
                    },
                    new[]
                    {
                        left.StageRouteStepImplementationChoice,
                        right.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithDuplicateSelectedRouteStepSymbol_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            RecipeGraph left = CreateSingleStageGraph(
                "landmass",
                schema,
                stageRouteStepSymbolValue: "lokrain.atlas.tests.route_step.duplicate");

            RecipeGraph right = CreateSingleStageGraph(
                "climate",
                schema,
                stageRouteStepSymbolValue: "lokrain.atlas.tests.route_step.duplicate");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.duplicate_route_step"),
                    DisplayName.Create("Duplicate Route Step Recipe"),
                    schema,
                    new[]
                    {
                        left.StageRouteChoice,
                        right.StageRouteChoice
                    },
                    new[]
                    {
                        left.StageRouteStepImplementationChoice,
                        right.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithStageRouteChoiceForDifferentSchema_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");
            GenerationSchemaDefinition alternativeSchema = CreateSchema("alternative");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    alternativeSchema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithStageRouteStepImplementationChoiceForDifferentSchema_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");
            RecipeGraph graph = CreateSingleStageGraph("landmass", schema);
            RecipeGraph alternativeGraph = CreateSingleStageGraph("climate", CreateSchema("alternative"));

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        alternativeGraph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithStageRouteStepImplementationChoiceForUnselectedRouteStep_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");
            RecipeGraph graph = CreateSingleStageGraph("landmass", schema);
            RecipeGraph unselectedGraph = CreateSingleStageGraph("climate", schema);

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice,
                        unselectedGraph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithDuplicateStageRouteStepImplementationChoice_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice,
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithoutImplementationChoiceForSelectedRouteStep_ThrowsArgumentException()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    graph.Schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    Array.Empty<StageRouteStepImplementationChoice>()));
        }

        [Test]
        public void Constructor_WithRouteStepInputNotAvailableFromStageInputOrPreviousStep_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            ResourceDefinition missingInput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.missing_input",
                "Missing Input");

            ResourceDefinition stageOutput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.stage_output",
                "Stage Output");

            RecipeGraph graph = CreateSingleStageGraph(
                "landmass",
                schema,
                stageProducedOutput: stageOutput,
                operationRequiredInput: missingInput,
                operationProducedOutput: stageOutput);

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithRouteNotProducingRequiredStageOutput_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            ResourceDefinition stageOutput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.stage_output",
                "Stage Output");

            ResourceDefinition operationOutput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.operation_output",
                "Operation Output");

            RecipeGraph graph = CreateSingleStageGraph(
                "landmass",
                schema,
                stageProducedOutput: stageOutput,
                operationProducedOutput: operationOutput);

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithUnsatisfiedStageDependency_ThrowsArgumentException()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            ResourceDefinition requiredInput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.required_input",
                "Required Input");

            ResourceDefinition stageOutput = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.stage_output",
                "Stage Output");

            RecipeGraph graph = CreateSingleStageGraph(
                "landmass",
                schema,
                stageRequiredInput: requiredInput,
                stageProducedOutput: stageOutput,
                operationRequiredInput: requiredInput,
                operationProducedOutput: stageOutput);

            Assert.Throws<ArgumentException>(
                () => new GenerationRecipeDefinition(
                    Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                    DisplayName.Create("Landmass Recipe"),
                    schema,
                    new[]
                    {
                        graph.StageRouteChoice
                    },
                    new[]
                    {
                        graph.StageRouteStepImplementationChoice
                    }));
        }

        [Test]
        public void Constructor_WithStageDependencySatisfiedByAnotherSelectedStage_CreatesGenerationRecipeDefinition()
        {
            GenerationSchemaDefinition schema = CreateSchema("world");

            ResourceDefinition sharedResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.shared",
                "Shared Resource");

            ResourceDefinition finalResource = CreateResource(
                schema,
                "lokrain.atlas.tests.resource.final",
                "Final Resource");

            RecipeGraph producer = CreateSingleStageGraph(
                "producer",
                schema,
                stageProducedOutput: sharedResource,
                operationProducedOutput: sharedResource);

            RecipeGraph consumer = CreateSingleStageGraph(
                "consumer",
                schema,
                stageRequiredInput: sharedResource,
                stageProducedOutput: finalResource,
                operationRequiredInput: sharedResource,
                operationProducedOutput: finalResource);

            var recipe = new GenerationRecipeDefinition(
                Symbol.Create("lokrain.atlas.tests.recipe.dependency"),
                DisplayName.Create("Dependency Recipe"),
                schema,
                new[]
                {
                    consumer.StageRouteChoice,
                    producer.StageRouteChoice
                },
                new[]
                {
                    consumer.StageRouteStepImplementationChoice,
                    producer.StageRouteStepImplementationChoice
                });

            Assert.That(recipe.StageRouteChoices, Has.Count.EqualTo(2));
            Assert.That(recipe.StageRouteChoices[0], Is.SameAs(consumer.StageRouteChoice));
            Assert.That(recipe.StageRouteChoices[1], Is.SameAs(producer.StageRouteChoice));
        }

        [Test]
        public void Constructor_WithMutableSourceCollections_StoresSnapshot()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            var stageRouteChoices = new List<StageRouteChoice>
            {
                graph.StageRouteChoice
            };

            var implementationChoices = new List<StageRouteStepImplementationChoice>
            {
                graph.StageRouteStepImplementationChoice
            };

            var recipe = new GenerationRecipeDefinition(
                Symbol.Create("lokrain.atlas.tests.recipe.landmass"),
                DisplayName.Create("Landmass Recipe"),
                graph.Schema,
                stageRouteChoices,
                implementationChoices);

            stageRouteChoices.Clear();
            implementationChoices.Clear();

            Assert.That(recipe.StageRouteChoices, Has.Count.EqualTo(1));
            Assert.That(recipe.StageRouteChoices[0], Is.SameAs(graph.StageRouteChoice));
            Assert.That(recipe.StageRouteStepImplementationChoices, Has.Count.EqualTo(1));
            Assert.That(
                recipe.StageRouteStepImplementationChoices[0],
                Is.SameAs(graph.StageRouteStepImplementationChoice));
        }

        [Test]
        public void Equals_WithSameSymbol_ReturnsTrue()
        {
            RecipeGraph leftGraph = CreateSingleStageGraph("landmass");
            RecipeGraph rightGraph = CreateSingleStageGraph("climate");

            GenerationRecipeDefinition left = CreateRecipe(
                leftGraph,
                "lokrain.atlas.tests.recipe.shared",
                "Landmass Recipe");

            GenerationRecipeDefinition right = CreateRecipe(
                rightGraph,
                "lokrain.atlas.tests.recipe.shared",
                "Climate Recipe");

            Assert.That(left.Equals(right), Is.True);
            Assert.That(left.Equals((object)right), Is.True);
            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        }

        [Test]
        public void Equals_WithDifferentSymbol_ReturnsFalse()
        {
            RecipeGraph graph = CreateSingleStageGraph("landmass");

            GenerationRecipeDefinition left = CreateRecipe(
                graph,
                "lokrain.atlas.tests.recipe.landmass",
                "Landmass Recipe");

            GenerationRecipeDefinition right = CreateRecipe(
                graph,
                "lokrain.atlas.tests.recipe.alternative",
                "Alternative Recipe");

            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.Equals((object)right), Is.False);
            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void Equals_WithNull_ReturnsFalse()
        {
            GenerationRecipeDefinition recipe = CreateRecipe(
                CreateSingleStageGraph("landmass"),
                "lokrain.atlas.tests.recipe.landmass",
                "Landmass Recipe");

            Assert.That(recipe.Equals(null), Is.False);
        }

        [Test]
        public void Equals_WithDifferentObjectType_ReturnsFalse()
        {
            GenerationRecipeDefinition recipe = CreateRecipe(
                CreateSingleStageGraph("landmass"),
                "lokrain.atlas.tests.recipe.landmass",
                "Landmass Recipe");

            Assert.That(recipe.Equals("GenerationRecipeDefinition"), Is.False);
        }

        [Test]
        public void EqualityOperator_WithBothNull_ReturnsTrue()
        {
            GenerationRecipeDefinition? left = null;
            GenerationRecipeDefinition? right = null;

            Assert.That(left == right, Is.True);
            Assert.That(left != right, Is.False);
        }

        [Test]
        public void EqualityOperator_WithOneNull_ReturnsFalse()
        {
            GenerationRecipeDefinition? left = CreateRecipe(
                CreateSingleStageGraph("landmass"),
                "lokrain.atlas.tests.recipe.landmass",
                "Landmass Recipe");

            GenerationRecipeDefinition? right = null;

            Assert.That(left == right, Is.False);
            Assert.That(left != right, Is.True);
        }

        [Test]
        public void ToString_ReturnsGenerationRecipeDefinitionSummary()
        {
            GenerationRecipeDefinition recipe = CreateRecipe(
                CreateSingleStageGraph("landmass"),
                "lokrain.atlas.tests.recipe.landmass",
                "Landmass Recipe");

            string value = recipe.ToString();

            Assert.That(
                value,
                Is.EqualTo(
                    "GenerationRecipeDefinition(Symbol: lokrain.atlas.tests.recipe.landmass, DisplayName: Landmass Recipe, GenerationSchemaDefinition: lokrain.atlas.tests.schema.world, StageRouteChoices: 1)"));
        }

        private static GenerationRecipeDefinition CreateRecipe(
            RecipeGraph graph,
            string symbolValue,
            string displayNameValue)
        {
            return new GenerationRecipeDefinition(
                Symbol.Create(symbolValue),
                DisplayName.Create(displayNameValue),
                graph.Schema,
                new[]
                {
                    graph.StageRouteChoice
                },
                new[]
                {
                    graph.StageRouteStepImplementationChoice
                });
        }

        private static RecipeGraph CreateSingleStageGraph(
            string suffix,
            GenerationSchemaDefinition? schema = null,
            ResourceDefinition? stageRequiredInput = null,
            ResourceDefinition? stageProducedOutput = null,
            ResourceDefinition? operationRequiredInput = null,
            ResourceDefinition? operationProducedOutput = null,
            string? stageSymbolValue = null,
            string? stageRouteSymbolValue = null,
            string? stageRouteStepSymbolValue = null)
        {
            GenerationSchemaDefinition resolvedSchema = schema ?? CreateSchema("world");

            ResourceDefinition resolvedStageProducedOutput =
                stageProducedOutput
                ?? CreateResource(
                    resolvedSchema,
                    $"lokrain.atlas.tests.resource.{suffix}_output",
                    $"{suffix} Output");

            ResourceDefinition resolvedOperationProducedOutput =
                operationProducedOutput ?? resolvedStageProducedOutput;

            StageDefinition stageDefinition = CreateStage(
                resolvedSchema,
                stageSymbolValue ?? $"lokrain.atlas.tests.stage.{suffix}",
                $"{suffix} Stage");

            OperationDefinition operationDefinition = CreateOperation(
                resolvedSchema,
                $"lokrain.atlas.tests.operation.{suffix}",
                $"{suffix} Operation");

            StageRouteStepDefinition stageRouteStepDefinition =
                new(
                    Symbol.Create(
                        stageRouteStepSymbolValue
                        ?? $"lokrain.atlas.tests.route_step.{suffix}"),
                    DisplayName.Create($"{suffix} Route Step"),
                    operationDefinition.Symbol);

            StageRouteDefinition stageRouteDefinition =
                new(
                    stageDefinition,
                    Symbol.Create(
                        stageRouteSymbolValue
                        ?? $"lokrain.atlas.tests.stage_route.{suffix}"),
                    DisplayName.Create($"{suffix} Stage Route"),
                    new[]
                    {
                        stageRouteStepDefinition
                    });

            StageContract stageContract =
                new(
                    stageDefinition,
                    ToResourceArray(stageRequiredInput),
                    new[]
                    {
                        resolvedStageProducedOutput
                    });

            OperationContract operationContract =
                new(
                    operationDefinition,
                    ToResourceArray(operationRequiredInput),
                    new[]
                    {
                        resolvedOperationProducedOutput
                    });

            OperationImplementationDefinition operationImplementationDefinition =
                new(
                    operationDefinition,
                    Symbol.Create($"lokrain.atlas.tests.implementation.{suffix}.default"),
                    DisplayName.Create($"{suffix} Default Implementation"));

            StageRouteChoice stageRouteChoice =
                new(
                    stageDefinition,
                    stageRouteDefinition,
                    stageContract);

            StageRouteStepImplementationChoice stageRouteStepImplementationChoice =
                new(
                    stageRouteStepDefinition,
                    operationDefinition,
                    operationContract,
                    operationImplementationDefinition);

            return new RecipeGraph(
                resolvedSchema,
                stageRouteChoice,
                stageRouteStepImplementationChoice);
        }

        private static ResourceDefinition[] ToResourceArray(ResourceDefinition? resourceDefinition)
        {
            if (resourceDefinition is null)
            {
                return Array.Empty<ResourceDefinition>();
            }

            return new[]
            {
                resourceDefinition
            };
        }

        private static GenerationSchemaDefinition CreateSchema(string suffix)
        {
            return new GenerationSchemaDefinition(
                Symbol.Create($"lokrain.atlas.tests.schema.{suffix}"),
                DisplayName.Create($"{suffix} Test Schema"));
        }

        private static StageDefinition CreateStage(
            GenerationSchemaDefinition schema,
            string symbolValue,
            string displayNameValue)
        {
            return new StageDefinition(
                schema,
                StageKind.Create($"{symbolValue}.kind"),
                Symbol.Create(symbolValue),
                DisplayName.Create(displayNameValue));
        }

        private static OperationDefinition CreateOperation(
            GenerationSchemaDefinition schema,
            string symbolValue,
            string displayNameValue)
        {
            return new OperationDefinition(
                schema,
                OperationKind.Create($"{symbolValue}.kind"),
                Symbol.Create(symbolValue),
                DisplayName.Create(displayNameValue));
        }

        private static ResourceDefinition CreateResource(
            GenerationSchemaDefinition schema,
            string symbolValue,
            string displayNameValue)
        {
            return new ResourceDefinition(
                Symbol.Create(symbolValue),
                DisplayName.Create(displayNameValue),
                schema);
        }

        private sealed class RecipeGraph
        {
            public RecipeGraph(
                GenerationSchemaDefinition schema,
                StageRouteChoice stageRouteChoice,
                StageRouteStepImplementationChoice stageRouteStepImplementationChoice)
            {
                Schema = schema;
                StageRouteChoice = stageRouteChoice;
                StageRouteStepImplementationChoice = stageRouteStepImplementationChoice;
            }

            public GenerationSchemaDefinition Schema { get; }

            public StageRouteChoice StageRouteChoice { get; }

            public StageRouteStepImplementationChoice StageRouteStepImplementationChoice { get; }
        }
    }
}