using System.Text;
using CityBuilder.Commands;
using CityBuilder.Commands.Actions;
using CityBuilder.Grid;
using CityBuilder.Zoning;

namespace CityBuilder.Persistence;

/// <summary>
/// Binary wire format for the command stream: each command type registers a numeric id plus
/// write/read functions. This is what turns commands into bytes for replay files today and
/// network packets (lockstep multiplayer) later. Readers receive the target
/// <see cref="GameSimulation"/> so commands can rebind to live services (e.g. the tax policy).
/// </summary>
public sealed class CommandCodec
{
    private readonly Dictionary<Type, (byte Id, Action<BinaryWriter, ICommand> Write)> _writers = new();
    private readonly Dictionary<byte, Func<BinaryReader, GameSimulation, ICommand>> _readers = new();

    private const int LogVersion = 1;
    private static readonly byte[] LogMagic = { (byte)'C', (byte)'B', (byte)'R', (byte)'L' };

    public void Register<T>(byte typeId, Action<BinaryWriter, T> write, Func<BinaryReader, GameSimulation, ICommand> read)
        where T : class, ICommand
    {
        if (_readers.ContainsKey(typeId))
        {
            throw new ArgumentException($"Command type id {typeId} is already registered.");
        }

        _writers[typeof(T)] = (typeId, (w, c) => write(w, (T)c));
        _readers[typeId] = read;
    }

    /// <summary>Codec pre-loaded with the built-in player commands.</summary>
    public static CommandCodec CreateDefault()
    {
        var codec = new CommandCodec();

        codec.Register<ZoneAreaCommand>(
            typeId: 1,
            write: (w, c) =>
            {
                w.Write(c.Min.X);
                w.Write(c.Min.Y);
                w.Write(c.Max.X);
                w.Write(c.Max.Y);
                w.Write((byte)c.Type);
                w.Write((byte)c.Density);
            },
            read: (r, sim) => new ZoneAreaCommand(
                new GridCoord(r.ReadInt32(), r.ReadInt32()),
                new GridCoord(r.ReadInt32(), r.ReadInt32()),
                (ZoneType)r.ReadByte(),
                (ZoneDensity)r.ReadByte()));

        codec.Register<BuildRoadCommand>(
            typeId: 2,
            write: (w, c) =>
            {
                w.Write(c.From.X);
                w.Write(c.From.Y);
                w.Write(c.To.X);
                w.Write(c.To.Y);
                w.Write(c.Capacity);
            },
            read: (r, sim) => new BuildRoadCommand(
                new GridCoord(r.ReadInt32(), r.ReadInt32()),
                new GridCoord(r.ReadInt32(), r.ReadInt32()),
                r.ReadInt32()));

        codec.Register<BulldozeAreaCommand>(
            typeId: 3,
            write: (w, c) =>
            {
                w.Write(c.Min.X);
                w.Write(c.Min.Y);
                w.Write(c.Max.X);
                w.Write(c.Max.Y);
            },
            read: (r, sim) => new BulldozeAreaCommand(
                new GridCoord(r.ReadInt32(), r.ReadInt32()),
                new GridCoord(r.ReadInt32(), r.ReadInt32())));

        codec.Register<SetTaxRateCommand>(
            typeId: 4,
            write: (w, c) =>
            {
                w.Write((byte)c.Category);
                w.Write(c.NewRate);
            },
            read: (r, sim) => new SetTaxRateCommand(
                sim.Economy.Taxes,
                (ZoneType)r.ReadByte(),
                r.ReadSingle()));

        return codec;
    }

    /// <summary>Serialize a replay log (header + entries) to a stream.</summary>
    public void WriteLog(ReplayLog log, Stream stream)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        w.Write(LogMagic);
        w.Write(LogVersion);
        w.Write(log.Count);

        for (int i = 0; i < log.Count; i++)
        {
            ReplayEntry entry = log.Entries[i];
            w.Write(entry.Tick);
            w.Write((byte)entry.Kind);

            if (entry.Kind != ReplayEntryKind.Command)
            {
                continue;
            }

            ICommand command = entry.Command
                ?? throw new InvalidOperationException("Command entry without a command instance.");
            if (!_writers.TryGetValue(command.GetType(), out (byte Id, Action<BinaryWriter, ICommand> Write) writer))
            {
                throw new NotSupportedException($"No codec registered for command type '{command.GetType().Name}'.");
            }

            w.Write(writer.Id);
            writer.Write(w, command);
        }
    }

    /// <summary>Deserialize a replay log, rebinding commands to the given simulation's services.</summary>
    public ReplayLog ReadLog(Stream stream, GameSimulation sim)
    {
        using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        byte[] magic = r.ReadBytes(4);
        if (magic.Length != 4 || magic[0] != LogMagic[0] || magic[1] != LogMagic[1] || magic[2] != LogMagic[2] || magic[3] != LogMagic[3])
        {
            throw new InvalidDataException("Not a CityBuilder replay log.");
        }

        int version = r.ReadInt32();
        if (version != LogVersion)
        {
            throw new InvalidDataException($"Unsupported replay log version {version} (expected {LogVersion}).");
        }

        var log = new ReplayLog();
        int count = r.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            long tick = r.ReadInt64();
            var kind = (ReplayEntryKind)r.ReadByte();

            if (kind != ReplayEntryKind.Command)
            {
                log.Add(new ReplayEntry(tick, kind, null));
                continue;
            }

            byte typeId = r.ReadByte();
            if (!_readers.TryGetValue(typeId, out Func<BinaryReader, GameSimulation, ICommand>? read))
            {
                throw new InvalidDataException($"Unknown command type id {typeId} in replay log.");
            }

            log.Add(new ReplayEntry(tick, ReplayEntryKind.Command, read(r, sim)));
        }

        return log;
    }
}
