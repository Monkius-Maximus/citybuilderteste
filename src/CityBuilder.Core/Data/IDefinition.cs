namespace CityBuilder.Data;

/// <summary>
/// Base contract for a piece of static, data-driven configuration ("what a thing is").
/// Definitions are authored as data (JSON, ScriptableObjects, code) and looked up by a
/// stable string <see cref="Id"/>. The factory turns a definition + a location into a live
/// entity, so new content is added as data without new code.
/// <para>
/// LEGAL: identifiers and names must stay generic/abstract — e.g. "CompactHatch_Tier1",
/// "Industrial_Heavy_Level2". Never reference real brands, models or companies.
/// </para>
/// </summary>
public interface IDefinition
{
    string Id { get; }
    string DisplayName { get; }
}
