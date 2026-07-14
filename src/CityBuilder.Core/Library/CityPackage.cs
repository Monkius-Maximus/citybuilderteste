using System.Text;
using CityBuilder.Persistence;
using CityBuilder.Shell;

namespace CityBuilder.Library;

/// <summary>Outcome of importing a <c>.polispack</c>.</summary>
public enum ImportStatus : byte
{
    Ok = 0,
    NotAPackage = 1,
    UnsupportedPackageVersion = 2,
    ChecksumMismatch = 3,
    CorruptSave = 4,
}

/// <summary>Result of <see cref="CityPackage.Import"/>: a status, the created slot on success, and a message.</summary>
public readonly struct ImportResult
{
    public readonly ImportStatus Status;
    public readonly CitySlot? Slot;
    public readonly string Message;

    public ImportResult(ImportStatus status, CitySlot? slot, string message)
    {
        Status = status;
        Slot = slot;
        Message = message;
    }

    public bool Ok => Status == ImportStatus.Ok;
}

/// <summary>
/// Export/import wrapper for sharing cities between machines and people. A <c>.polispack</c> is
/// the save's bytes verbatim wrapped in an envelope: magic + package version, a manifest (game
/// version, save version, and a copy of the display metadata) and an FNV-1a checksum of the
/// payload. Import verifies magic → version → checksum → that the payload is a loadable save,
/// ALL before touching the library, so a corrupt download or a save from a newer game yields an
/// exact message instead of a half-written slot. The working <c>.polis</c> saves stay lean; the
/// integrity/context data rides only on the export envelope.
/// </summary>
public static class CityPackage
{
    public const string Extension = ".polispack";

    private const int PackageVersion = 1;
    private static readonly byte[] Magic = { (byte)'C', (byte)'B', (byte)'P', (byte)'K' };

    /// <summary>Wrap a library slot's save file into a package stream.</summary>
    public static void Export(in CitySlot slot, Stream destination)
    {
        byte[] payload = File.ReadAllBytes(slot.FilePath);
        SaveMetadata metadata = slot.Metadata;

        using var w = new BinaryWriter(destination, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(PackageVersion);
        w.Write(GameInfo.Version);          // game version that produced it (display/diagnostics)
        w.Write(metadata.CityName);         // manifest: cheap fields for a pre-import preview
        w.Write(metadata.Population);
        w.Write(metadata.Treasury.Units);
        w.Write(metadata.Tick);
        w.Write(metadata.SavedAtUtc.Ticks);
        w.Write(Fnv64(payload));            // integrity of the payload
        w.Write(payload.Length);
        w.Write(payload);
    }

    /// <summary>
    /// Read a package, verify it end to end, and (on success) add its save to the library —
    /// renaming the imported city on a name collision so nothing is silently shadowed.
    /// </summary>
    public static ImportResult Import(Stream source, CityLibrary library)
    {
        byte[] payload;
        ulong storedChecksum;
        string cityName;

        try
        {
            using var r = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);

            byte[] magic = r.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != Magic[0] || magic[1] != Magic[1] || magic[2] != Magic[2] || magic[3] != Magic[3])
            {
                return new ImportResult(ImportStatus.NotAPackage, null, "Not a .polispack file.");
            }

            int version = r.ReadInt32();
            if (version != PackageVersion)
            {
                return new ImportResult(ImportStatus.UnsupportedPackageVersion, null,
                    $"Package version {version} is newer than this game supports ({PackageVersion}).");
            }

            r.ReadString();                 // game version (ignored on import)
            cityName = r.ReadString();
            r.ReadInt64();                  // population
            r.ReadInt64();                  // treasury units
            r.ReadInt64();                  // tick
            r.ReadInt64();                  // saved-at
            storedChecksum = r.ReadUInt64();
            int length = r.ReadInt32();
            if (length < 0)
            {
                return new ImportResult(ImportStatus.CorruptSave, null, "Package declares an invalid payload length.");
            }

            payload = r.ReadBytes(length);
            if (payload.Length != length)
            {
                return new ImportResult(ImportStatus.CorruptSave, null, "Package payload is truncated.");
            }
        }
        catch (Exception ex) when (ex is EndOfStreamException or IOException)
        {
            return new ImportResult(ImportStatus.NotAPackage, null, "Could not read the package.");
        }

        if (Fnv64(payload) != storedChecksum)
        {
            return new ImportResult(ImportStatus.ChecksumMismatch, null,
                "Integrity check failed — the file is corrupt or was modified.");
        }

        // Validate the payload is actually a loadable save before writing anything.
        try
        {
            using var probe = new MemoryStream(payload, writable: false);
            SaveGame.ReadMetadata(probe);
        }
        catch (InvalidDataException ex)
        {
            return new ImportResult(ImportStatus.CorruptSave, null, $"Contained save is unreadable: {ex.Message}");
        }

        // Collision-safe display name so an imported city never hides an existing one.
        byte[] toWrite = payload;
        if (NameExists(library, cityName))
        {
            string newName = cityName + " (importada)";
            using var renamed = new MemoryStream();
            SaveGame.RewriteCityName(payload, renamed, newName);
            toWrite = renamed.ToArray();
        }

        CitySlot slot = library.ImportSaveBytes(toWrite);
        return new ImportResult(ImportStatus.Ok, slot, $"Imported '{slot.CityName}'.");
    }

    private static bool NameExists(CityLibrary library, string cityName)
    {
        foreach (CitySlot slot in library.Refresh())
        {
            if (string.Equals(slot.CityName, cityName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ulong Fnv64(ReadOnlySpan<byte> data)
    {
        ulong h = 1469598103934665603UL;
        for (int i = 0; i < data.Length; i++)
        {
            h = (h ^ data[i]) * 1099511628211UL;
        }

        return h;
    }
}
