// Packages/com.lokrain.atlas/Runtime/Pipelines/AtlasPipelineValidationPolicy.cs
//
// Package: com.lokrain.atlas
// Namespace: Lokrain.Atlas.Pipelines
//
// Purpose
// - Define route/preset policy for Atlas pipeline validation.
// - Keep generic pipeline metadata permissive while allowing concrete presets to reject invalid structures.
// - Express required, allowed, and forbidden stage/operation contracts.
// - Express duplicate and ordering rules without baking them into AtlasPipelineDefinition or AtlasCompiledPlan.
//
// Design notes
// - This is policy metadata, not execution metadata.
// - This type does not allocate workspace memory.
// - This type does not schedule jobs.
// - This type does not resolve Field storage.
// - Generic Atlas pipelines may repeat stages and operations.
// - Concrete route/preset policies may reject repeats deliberately.
// - default(AtlasPipelineValidationPolicy) is valid and open: it imposes no extra route rules.

using System;
using System.Globalization;
using Lokrain.Atlas.Operations;
using Lokrain.Atlas.Stages;
using Unity.Collections;

namespace Lokrain.Atlas.Pipelines
{
    /// <summary>
    /// Route/preset validation policy for Atlas pipelines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="AtlasPipelineValidationPolicy"/> defines what a concrete pipeline preset accepts.
    /// The base pipeline type stays permissive and ordered. This policy decides whether a concrete
    /// route rejects repeated stages, requires specific stages, forbids operations, or enforces
    /// declared ordering.
    /// </para>
    ///
    /// <para>
    /// Stage and operation lists use durable ids. Debug names are diagnostic context only and are
    /// not used by this policy for identity.
    /// </para>
    ///
    /// <para>
    /// The default value is valid and imposes no additional constraints. This is intentional:
    /// permissive metadata belongs to the generic pipeline layer; restrictions belong to explicit
    /// route/preset policy.
    /// </para>
    /// </remarks>
    public sealed class AtlasPipelineValidationPolicy
    {
        private static readonly AtlasStageId[] NoStages = Array.Empty<AtlasStageId>();
        private static readonly AtlasOperationId[] NoOperations = Array.Empty<AtlasOperationId>();

        /// <summary>
        /// Open policy with no route-specific constraints.
        /// </summary>
        public static readonly AtlasPipelineValidationPolicy Open = new(
            default,
            AtlasPipelineValidationPolicyFlags.None,
            NoStages,
            NoStages,
            NoStages,
            NoOperations,
            NoOperations,
            NoOperations);

        /// <summary>
        /// Conservative policy suitable as a starting point for production presets.
        /// </summary>
        /// <remarks>
        /// This policy rejects repeated durable stage identities, rejects repeated operation
        /// identities inside each stage, and enforces forbidden lists when supplied. It does not
        /// require a fixed stage set because that must be supplied by a concrete route/preset.
        /// </remarks>
        public static readonly AtlasPipelineValidationPolicy Conservative = new(
            new FixedString64Bytes("Conservative"),
            AtlasPipelineValidationPolicyFlags.RejectRepeatedStageIdentity |
            AtlasPipelineValidationPolicyFlags.RejectRepeatedOperationIdentityWithinStage |
            AtlasPipelineValidationPolicyFlags.EnforceForbiddenStages |
            AtlasPipelineValidationPolicyFlags.EnforceForbiddenOperations,
            NoStages,
            NoStages,
            NoStages,
            NoOperations,
            NoOperations,
            NoOperations);

        /// <summary>
        /// Diagnostic policy name.
        /// </summary>
        public readonly FixedString64Bytes Name;

        /// <summary>
        /// Policy flags.
        /// </summary>
        public readonly AtlasPipelineValidationPolicyFlags Flags;

        private readonly AtlasStageId[] _requiredStages;
        private readonly AtlasStageId[] _allowedStages;
        private readonly AtlasStageId[] _forbiddenStages;
        private readonly AtlasOperationId[] _requiredOperations;
        private readonly AtlasOperationId[] _allowedOperations;
        private readonly AtlasOperationId[] _forbiddenOperations;

        private AtlasPipelineValidationPolicy(
            FixedString64Bytes name,
            AtlasPipelineValidationPolicyFlags flags,
            AtlasStageId[] requiredStages,
            AtlasStageId[] allowedStages,
            AtlasStageId[] forbiddenStages,
            AtlasOperationId[] requiredOperations,
            AtlasOperationId[] allowedOperations,
            AtlasOperationId[] forbiddenOperations)
        {
            Name = name;
            Flags = flags;

            _requiredStages = CopyAndValidateStageIds(requiredStages, nameof(requiredStages));
            _allowedStages = CopyAndValidateStageIds(allowedStages, nameof(allowedStages));
            _forbiddenStages = CopyAndValidateStageIds(forbiddenStages, nameof(forbiddenStages));
            _requiredOperations = CopyAndValidateOperationIds(requiredOperations, nameof(requiredOperations));
            _allowedOperations = CopyAndValidateOperationIds(allowedOperations, nameof(allowedOperations));
            _forbiddenOperations = CopyAndValidateOperationIds(forbiddenOperations, nameof(forbiddenOperations));

            ValidateNoDuplicateStageIds(_requiredStages, nameof(requiredStages));
            ValidateNoDuplicateStageIds(_allowedStages, nameof(allowedStages));
            ValidateNoDuplicateStageIds(_forbiddenStages, nameof(forbiddenStages));

            ValidateNoDuplicateOperationIds(_requiredOperations, nameof(requiredOperations));
            ValidateNoDuplicateOperationIds(_allowedOperations, nameof(allowedOperations));
            ValidateNoDuplicateOperationIds(_forbiddenOperations, nameof(forbiddenOperations));
        }

        /// <summary>
        /// Gets whether this policy imposes no route-specific constraints.
        /// </summary>
        public bool IsOpen => Flags == AtlasPipelineValidationPolicyFlags.None &&
                              _requiredStages.Length == 0 &&
                              _allowedStages.Length == 0 &&
                              _forbiddenStages.Length == 0 &&
                              _requiredOperations.Length == 0 &&
                              _allowedOperations.Length == 0 &&
                              _forbiddenOperations.Length == 0;

        /// <summary>
        /// Gets the number of required stage ids.
        /// </summary>
        public int RequiredStageCount => _requiredStages.Length;

        /// <summary>
        /// Gets the number of allowed stage ids.
        /// </summary>
        public int AllowedStageCount => _allowedStages.Length;

        /// <summary>
        /// Gets the number of forbidden stage ids.
        /// </summary>
        public int ForbiddenStageCount => _forbiddenStages.Length;

        /// <summary>
        /// Gets the number of required operation ids.
        /// </summary>
        public int RequiredOperationCount => _requiredOperations.Length;

        /// <summary>
        /// Gets the number of allowed operation ids.
        /// </summary>
        public int AllowedOperationCount => _allowedOperations.Length;

        /// <summary>
        /// Gets the number of forbidden operation ids.
        /// </summary>
        public int ForbiddenOperationCount => _forbiddenOperations.Length;

        /// <summary>
        /// Creates a pipeline validation policy.
        /// </summary>
        /// <param name="name">Diagnostic policy name.</param>
        /// <param name="flags">Policy flags.</param>
        /// <param name="requiredStages">Stage ids that must appear.</param>
        /// <param name="allowedStages">Stage ids accepted when allowed-stage enforcement is enabled.</param>
        /// <param name="forbiddenStages">Stage ids rejected when forbidden-stage enforcement is enabled.</param>
        /// <param name="requiredOperations">Operation ids that must appear.</param>
        /// <param name="allowedOperations">Operation ids accepted when allowed-operation enforcement is enabled.</param>
        /// <param name="forbiddenOperations">Operation ids rejected when forbidden-operation enforcement is enabled.</param>
        /// <returns>A validated immutable policy.</returns>
        public static AtlasPipelineValidationPolicy Create(
            FixedString64Bytes name = default,
            AtlasPipelineValidationPolicyFlags flags = AtlasPipelineValidationPolicyFlags.None,
            AtlasStageId[] requiredStages = null,
            AtlasStageId[] allowedStages = null,
            AtlasStageId[] forbiddenStages = null,
            AtlasOperationId[] requiredOperations = null,
            AtlasOperationId[] allowedOperations = null,
            AtlasOperationId[] forbiddenOperations = null)
        {
            return new AtlasPipelineValidationPolicy(
                name,
                flags,
                requiredStages ?? NoStages,
                allowedStages ?? NoStages,
                forbiddenStages ?? NoStages,
                requiredOperations ?? NoOperations,
                allowedOperations ?? NoOperations,
                forbiddenOperations ?? NoOperations);
        }

        /// <summary>
        /// Gets a required stage id by policy index.
        /// </summary>
        /// <param name="index">Policy-local stage index.</param>
        /// <returns>The required stage id.</returns>
        public AtlasStageId GetRequiredStage(int index)
        {
            return _requiredStages[index];
        }

        /// <summary>
        /// Gets an allowed stage id by policy index.
        /// </summary>
        /// <param name="index">Policy-local stage index.</param>
        /// <returns>The allowed stage id.</returns>
        public AtlasStageId GetAllowedStage(int index)
        {
            return _allowedStages[index];
        }

        /// <summary>
        /// Gets a forbidden stage id by policy index.
        /// </summary>
        /// <param name="index">Policy-local stage index.</param>
        /// <returns>The forbidden stage id.</returns>
        public AtlasStageId GetForbiddenStage(int index)
        {
            return _forbiddenStages[index];
        }

        /// <summary>
        /// Gets a required operation id by policy index.
        /// </summary>
        /// <param name="index">Policy-local operation index.</param>
        /// <returns>The required operation id.</returns>
        public AtlasOperationId GetRequiredOperation(int index)
        {
            return _requiredOperations[index];
        }

        /// <summary>
        /// Gets an allowed operation id by policy index.
        /// </summary>
        /// <param name="index">Policy-local operation index.</param>
        /// <returns>The allowed operation id.</returns>
        public AtlasOperationId GetAllowedOperation(int index)
        {
            return _allowedOperations[index];
        }

        /// <summary>
        /// Gets a forbidden operation id by policy index.
        /// </summary>
        /// <param name="index">Policy-local operation index.</param>
        /// <returns>The forbidden operation id.</returns>
        public AtlasOperationId GetForbiddenOperation(int index)
        {
            return _forbiddenOperations[index];
        }

        /// <summary>
        /// Returns whether the policy requires the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stage id to test.</param>
        /// <returns><c>true</c> when the id appears in the required stage list.</returns>
        public bool RequiresStage(AtlasStageId stageId)
        {
            return ContainsStageId(_requiredStages, stageId);
        }

        /// <summary>
        /// Returns whether the policy allows the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stage id to test.</param>
        /// <returns>
        /// <c>true</c> when allowed-stage enforcement is disabled, the allowed list is empty, or the id is listed.
        /// </returns>
        public bool AllowsStage(AtlasStageId stageId)
        {
            return !Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceAllowedStages) ||
                   _allowedStages.Length == 0 ||
                   ContainsStageId(_allowedStages, stageId);
        }

        /// <summary>
        /// Returns whether the policy forbids the supplied stage id.
        /// </summary>
        /// <param name="stageId">Stage id to test.</param>
        /// <returns><c>true</c> when forbidden-stage enforcement is enabled and the id is listed.</returns>
        public bool ForbidsStage(AtlasStageId stageId)
        {
            return Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceForbiddenStages) &&
                   ContainsStageId(_forbiddenStages, stageId);
        }

        /// <summary>
        /// Returns whether the policy requires the supplied operation id.
        /// </summary>
        /// <param name="operationId">Operation id to test.</param>
        /// <returns><c>true</c> when the id appears in the required operation list.</returns>
        public bool RequiresOperation(AtlasOperationId operationId)
        {
            return ContainsOperationId(_requiredOperations, operationId);
        }

        /// <summary>
        /// Returns whether the policy allows the supplied operation id.
        /// </summary>
        /// <param name="operationId">Operation id to test.</param>
        /// <returns>
        /// <c>true</c> when allowed-operation enforcement is disabled, the allowed list is empty, or the id is listed.
        /// </returns>
        public bool AllowsOperation(AtlasOperationId operationId)
        {
            return !Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceAllowedOperations) ||
                   _allowedOperations.Length == 0 ||
                   ContainsOperationId(_allowedOperations, operationId);
        }

        /// <summary>
        /// Returns whether the policy forbids the supplied operation id.
        /// </summary>
        /// <param name="operationId">Operation id to test.</param>
        /// <returns><c>true</c> when forbidden-operation enforcement is enabled and the id is listed.</returns>
        public bool ForbidsOperation(AtlasOperationId operationId)
        {
            return Flags.HasAny(AtlasPipelineValidationPolicyFlags.EnforceForbiddenOperations) &&
                   ContainsOperationId(_forbiddenOperations, operationId);
        }

        /// <summary>
        /// Returns a copy of required stage ids.
        /// </summary>
        /// <returns>A new array containing required stage ids.</returns>
        public AtlasStageId[] ToRequiredStageArray()
        {
            return Copy(_requiredStages);
        }

        /// <summary>
        /// Returns a copy of allowed stage ids.
        /// </summary>
        /// <returns>A new array containing allowed stage ids.</returns>
        public AtlasStageId[] ToAllowedStageArray()
        {
            return Copy(_allowedStages);
        }

        /// <summary>
        /// Returns a copy of forbidden stage ids.
        /// </summary>
        /// <returns>A new array containing forbidden stage ids.</returns>
        public AtlasStageId[] ToForbiddenStageArray()
        {
            return Copy(_forbiddenStages);
        }

        /// <summary>
        /// Returns a copy of required operation ids.
        /// </summary>
        /// <returns>A new array containing required operation ids.</returns>
        public AtlasOperationId[] ToRequiredOperationArray()
        {
            return Copy(_requiredOperations);
        }

        /// <summary>
        /// Returns a copy of allowed operation ids.
        /// </summary>
        /// <returns>A new array containing allowed operation ids.</returns>
        public AtlasOperationId[] ToAllowedOperationArray()
        {
            return Copy(_allowedOperations);
        }

        /// <summary>
        /// Returns a copy of forbidden operation ids.
        /// </summary>
        /// <returns>A new array containing forbidden operation ids.</returns>
        public AtlasOperationId[] ToForbiddenOperationArray()
        {
            return Copy(_forbiddenOperations);
        }

        /// <summary>
        /// Returns a compact diagnostic label.
        /// </summary>
        /// <returns>A compact policy label.</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "AtlasPipelineValidationPolicy(Name={0}, Flags={1}, RequiredStages={2}, AllowedStages={3}, ForbiddenStages={4}, RequiredOperations={5}, AllowedOperations={6}, ForbiddenOperations={7})",
                Name.IsEmpty ? "<unnamed>" : Name.ToString(),
                Flags,
                RequiredStageCount,
                AllowedStageCount,
                ForbiddenStageCount,
                RequiredOperationCount,
                AllowedOperationCount,
                ForbiddenOperationCount);
        }

        private static AtlasStageId[] CopyAndValidateStageIds(
            AtlasStageId[] source,
            string parameterName)
        {
            if (source == null || source.Length == 0)
            {
                return NoStages;
            }

            var copy = new AtlasStageId[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var id = source[i];


                copy[i] = id;
            }

            return copy;
        }

        private static AtlasOperationId[] CopyAndValidateOperationIds(
            AtlasOperationId[] source,
            string parameterName)
        {
            if (source == null || source.Length == 0)
            {
                return NoOperations;
            }

            var copy = new AtlasOperationId[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                var id = source[i];


                copy[i] = id;
            }

            return copy;
        }

        private static void ValidateNoDuplicateStageIds(
            AtlasStageId[] ids,
            string parameterName)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                for (var j = i + 1; j < ids.Length; j++)
                {
                    if (ids[i] == ids[j])
                    {
                        throw new ArgumentException(
                            $"Atlas pipeline validation policy contains duplicate stage id '{ids[i]}' at indices '{i}' and '{j}'.",
                            parameterName);
                    }
                }
            }
        }

        private static void ValidateNoDuplicateOperationIds(
            AtlasOperationId[] ids,
            string parameterName)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                for (var j = i + 1; j < ids.Length; j++)
                {
                    if (ids[i] == ids[j])
                    {
                        throw new ArgumentException(
                            $"Atlas pipeline validation policy contains duplicate operation id '{ids[i]}' at indices '{i}' and '{j}'.",
                            parameterName);
                    }
                }
            }
        }

        private static bool ContainsStageId(
            AtlasStageId[] ids,
            AtlasStageId stageId)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == stageId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsOperationId(
            AtlasOperationId[] ids,
            AtlasOperationId operationId)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == operationId)
                {
                    return true;
                }
            }

            return false;
        }

        private static AtlasStageId[] Copy(AtlasStageId[] source)
        {
            if (source.Length == 0)
            {
                return NoStages;
            }

            var copy = new AtlasStageId[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static AtlasOperationId[] Copy(AtlasOperationId[] source)
        {
            if (source.Length == 0)
            {
                return NoOperations;
            }

            var copy = new AtlasOperationId[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }
    }
}