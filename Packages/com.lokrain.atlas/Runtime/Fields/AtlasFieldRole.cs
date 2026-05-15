// Runtime/Fields/AtlasFieldRole.cs

namespace Lokrain.Atlas.Fields
{
    /// <summary>
    /// Defines the semantic role of an Atlas Field in the generated-world contract.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Field role answers why the Field exists. It is separate from storage lifetime,
    /// ownership, access, and hashing. A canonical Field may be allocated temporarily
    /// during generation, and a scratch Field may use persistent workspace memory for
    /// reuse across frames or generation runs.
    /// </para>
    ///
    /// <para>
    /// Validators, operation compilers, hash builders, artifact writers, and editor tooling
    /// use this value to prevent semantic drift between generated-world meaning, transient
    /// execution data, exported payloads, diagnostics, and externally owned native data.
    /// </para>
    /// </remarks>
    public enum AtlasFieldRole : byte
    {
        /// <summary>
        /// No Field role is declared.
        /// </summary>
        None = 0,

        /// <summary>
        /// Durable generated-world state that defines canonical world meaning.
        /// </summary>
        Canonical = 1,

        /// <summary>
        /// Internal support data required to produce, validate, or transform canonical Fields.
        /// </summary>
        Support = 2,

        /// <summary>
        /// Transient execution data used only as scratch memory.
        /// </summary>
        Scratch = 3,

        /// <summary>
        /// Data prepared for a downstream consumer outside the canonical generation model.
        /// </summary>
        Payload = 4,

        /// <summary>
        /// Data used for validation, inspection, profiling, tests, or editor tooling.
        /// </summary>
        Diagnostic = 5,

        /// <summary>
        /// Data owned by an external system but declared in Atlas for validation and binding.
        /// </summary>
        External = 6
    }
}