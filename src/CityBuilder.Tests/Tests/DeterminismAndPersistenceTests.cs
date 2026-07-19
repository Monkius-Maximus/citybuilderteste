using CityBuilder;
using CityBuilder.Commands.Actions;
using CityBuilder.Grid;
using CityBuilder.Persistence;
using CityBuilder.Tests.Framework;
using CityBuilder.Zoning;

namespace CityBuilder.Tests.Tests;

public static class DeterminismAndPersistenceTests
{
    [TestCase]
    public static void SameSeed_SameCommands_IdenticalChecksum()
    {
        ulong a = StateChecksum.Compute(Scenarios.GrownResidential(1337, 180));
        ulong b = StateChecksum.Compute(Scenarios.GrownResidential(1337, 180));
        Check.Equal(a, b, "checksums for identical seed+inputs");
    }

    [TestCase]
    public static void DifferentSeed_DifferentChecksum()
    {
        ulong a = StateChecksum.Compute(Scenarios.GrownResidential(1, 180));
        ulong b = StateChecksum.Compute(Scenarios.GrownResidential(2, 180));
        Check.NotEqual(a, b, "checksums for different seeds");
    }

    [TestCase]
    public static void SaveLoad_ChecksumMatches()
    {
        GameSimulation original = Scenarios.GrownResidential(42, 180);
        ulong before = StateChecksum.Compute(original);

        var stream = new MemoryStream();
        SaveGame.Write(original, stream);

        stream.Position = 0;
        GameConfig config = SaveGame.ReadConfig(stream);
        var loaded = new GameSimulation(config);
        Scenarios.Bootstrap(loaded);
        stream.Position = 0;
        SaveGame.ReadInto(loaded, stream);

        Check.Equal(before, StateChecksum.Compute(loaded), "save/load checksum");
    }

    [TestCase]
    public static void Save_IsVersion3_WithThumbnail()
    {
        GameSimulation sim = Scenarios.GrownResidential(7, 120);
        var stream = new MemoryStream();
        SaveGame.Write(sim, stream);

        stream.Position = 0;
        SaveMetadata meta = SaveGame.ReadMetadata(stream);

        Check.True(meta.HasThumbnail, "v3 save carries a thumbnail");
        Check.Equal(64, meta.ThumbnailWidth, "thumbnail width");
        Check.Equal(44, meta.ThumbnailHeight, "thumbnail height");
        Check.Equal(64 * 44 * 4, meta.Thumbnail.Length, "thumbnail byte count");
    }

    [TestCase]
    public static void Metadata_MatchesState()
    {
        GameSimulation sim = Scenarios.GrownResidential(9, 180);
        var stream = new MemoryStream();
        SaveGame.Write(sim, stream);

        stream.Position = 0;
        SaveMetadata meta = SaveGame.ReadMetadata(stream);

        Check.Equal(sim.CurrentTick, meta.Tick, "metadata tick");
        Check.Equal(sim.Economy.Balance.Units, meta.Treasury.Units, "metadata treasury");
        Check.Equal("New City", meta.CityName, "metadata city name default");
    }

    [TestCase]
    public static void RecordReplay_ReproducesChecksum()
    {
        var codec = CommandCodec.CreateDefault();

        // Recorded session: zone at tick 0, raise residential tax at tick 60.
        var recorded = new GameSimulation(new GameConfig(32, 32, 99));
        var log = new ReplayLog();
        recorded.Commands.Recorder = new ReplayRecorder(log);
        recorded.Submit(new ZoneAreaCommand(new GridCoord(10, 10), new GridCoord(22, 22), ZoneType.Residential, ZoneDensity.Medium));
        for (int i = 0; i < 120; i++)
        {
            recorded.Step();
            if (recorded.CurrentTick == 60)
            {
                recorded.Submit(new SetTaxRateCommand(recorded.Economy.Taxes, ZoneType.Residential, 0.15f));
            }
        }

        ulong expected = StateChecksum.Compute(recorded);
        long finalTick = recorded.CurrentTick;

        var wire = new MemoryStream();
        codec.WriteLog(log, wire);

        // Replay into a fresh, identically-configured simulation.
        var replayed = new GameSimulation(new GameConfig(32, 32, 99));
        wire.Position = 0;
        ReplayLog decoded = codec.ReadLog(wire, replayed);
        ReplayPlayer.Play(replayed, decoded, finalTick);

        Check.Equal(expected, StateChecksum.Compute(replayed), "replay checksum");
    }
}
