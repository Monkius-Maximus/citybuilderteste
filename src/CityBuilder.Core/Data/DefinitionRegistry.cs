namespace CityBuilder.Data;

/// <summary>
/// The catalog of all loaded definitions, indexed by string id and by a dense runtime index.
/// Components store the compact <see cref="IndexOf">index</see> (a blittable int) instead of a
/// string, keeping ECS component data cache-friendly while designers still author by id.
/// </summary>
public sealed class DefinitionRegistry
{
    private readonly Dictionary<string, int> _indexById = new();
    private readonly List<IDefinition> _byIndex = new();

    public int Count => _byIndex.Count;

    public void Add(IDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.Id))
        {
            throw new ArgumentException("Definition must have a non-empty Id.");
        }

        if (_indexById.ContainsKey(definition.Id))
        {
            throw new ArgumentException($"Duplicate definition id '{definition.Id}'.");
        }

        _indexById[definition.Id] = _byIndex.Count;
        _byIndex.Add(definition);
    }

    public void LoadFrom(IDefinitionSource source)
    {
        foreach (IDefinition def in source.Load())
        {
            Add(def);
        }
    }

    /// <summary>Dense runtime index for an id, or -1 if unknown.</summary>
    public int IndexOf(string id) => _indexById.TryGetValue(id, out int i) ? i : -1;

    public IDefinition GetByIndex(int index) => _byIndex[index];

    public bool TryGet<T>(string id, out T definition) where T : class, IDefinition
    {
        if (_indexById.TryGetValue(id, out int i) && _byIndex[i] is T typed)
        {
            definition = typed;
            return true;
        }

        definition = null!;
        return false;
    }

    public T Get<T>(string id) where T : class, IDefinition
        => TryGet(id, out T def) ? def : throw new KeyNotFoundException($"No {typeof(T).Name} with id '{id}'.");

    public IEnumerable<T> OfType<T>() where T : class, IDefinition
    {
        foreach (IDefinition def in _byIndex)
        {
            if (def is T typed)
            {
                yield return typed;
            }
        }
    }
}
