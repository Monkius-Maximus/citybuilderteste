namespace CityBuilder.Data;

/// <summary>
/// Abstract provider of definitions. Keeps the core IO-agnostic: a JSON file loader, a
/// Unity ScriptableObject importer, a Godot resource loader, or a hardcoded catalog each
/// implement this and feed the same <see cref="DefinitionRegistry"/>. The core never knows
/// where definitions came from.
/// </summary>
public interface IDefinitionSource
{
    IEnumerable<IDefinition> Load();
}

/// <summary>Trivial in-memory source (unit tests, headless demo, procedural generation).</summary>
public sealed class InMemoryDefinitionSource : IDefinitionSource
{
    private readonly List<IDefinition> _definitions = new();

    public InMemoryDefinitionSource Add(IDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    public IEnumerable<IDefinition> Load() => _definitions;
}
