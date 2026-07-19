using CityBuilder;
using CityBuilder.Library;
using CityBuilder.Shell;
using CityBuilder.Tests.Framework;

namespace CityBuilder.Tests.Tests;

public static class LibraryAndPackageTests
{
    [TestCase]
    public static void Save_List_Rename_Duplicate_Delete()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        GameSimulation sim = Scenarios.GrownResidential(3, 120);

        CitySlot slot = library.Save(sim);
        Check.Equal(1, library.Refresh().Count, "one save after Save");

        CitySlot renamed = library.Rename(slot, "Renamed City");
        Check.Equal("Renamed City", renamed.CityName, "rename changes display name");
        Check.Equal(slot.FileName, renamed.FileName, "rename keeps the file name");
        Check.Equal(1, library.Refresh().Count, "rename does not add a slot");

        library.Duplicate(renamed, "A Copy");
        Check.Equal(2, library.Refresh().Count, "duplicate adds a slot");

        CitySlot toDelete = library.Refresh().First(s => s.CityName == "A Copy");
        library.Delete(toDelete);
        Check.Equal(1, library.Refresh().Count, "delete removes from listing");
        Check.Equal(1, library.TrashContents().Count, "delete moves file to trash (non-destructive)");
    }

    [TestCase]
    public static void Save_IntoSameSlot_OverwritesInPlace()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        GameSimulation sim = Scenarios.GrownResidential(4, 60);

        CitySlot first = library.Save(sim);
        CitySlot second = library.Save(sim, into: first);

        Check.Equal(first.FilePath, second.FilePath, "same slot path");
        Check.Equal(1, library.Refresh().Count, "no duplicate file");
    }

    [TestCase]
    public static void Autosave_RotationIsCapped()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        var settings = new GameSettings { Autosave = AutosaveInterval.Every10Min };
        var autosave = new AutosaveService(library, settings, rotationSlots: 5);
        GameSimulation sim = Scenarios.GrownResidential(5, 30);

        for (int i = 0; i < 8; i++)
        {
            autosave.Update(sim, TimeSpan.FromMinutes(11)); // each call exceeds the interval
        }

        Check.Equal(5, autosave.SlotsFor(sim).Count, "autosave never exceeds the rotation cap");
    }

    [TestCase]
    public static void Autosave_OffDoesNothing()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        var settings = new GameSettings { Autosave = AutosaveInterval.Off };
        var autosave = new AutosaveService(library, settings);
        GameSimulation sim = Scenarios.GrownResidential(6, 10);

        bool fired = autosave.Update(sim, TimeSpan.FromHours(2));
        Check.False(fired, "autosave Off never fires");
        Check.Equal(0, library.Refresh().Count, "nothing written");
    }

    [TestCase]
    public static void Package_ExportImport_RoundTrips()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        CitySlot slot = library.Save(Scenarios.GrownResidential(11, 120));

        var package = new MemoryStream();
        CityPackage.Export(slot, package);
        package.Position = 0;

        ImportResult result = CityPackage.Import(package, library);
        Check.True(result.Ok, $"import ok ({result.Message})");
        Check.Equal(2, library.Refresh().Count, "import adds a slot");
    }

    [TestCase]
    public static void Package_TamperedPayload_RejectedOnChecksum()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        CitySlot slot = library.Save(Scenarios.GrownResidential(12, 60));

        var package = new MemoryStream();
        CityPackage.Export(slot, package);
        byte[] bytes = package.ToArray();
        bytes[^1] ^= 0xFF; // flip the last payload byte

        ImportResult result = CityPackage.Import(new MemoryStream(bytes), library);
        Check.Equal(ImportStatus.ChecksumMismatch, result.Status, "tampered package rejected");
    }

    [TestCase]
    public static void Package_NotAPackage_Rejected()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        var garbage = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        ImportResult result = CityPackage.Import(garbage, library);
        Check.Equal(ImportStatus.NotAPackage, result.Status, "non-package rejected");
    }

    [TestCase]
    public static void Package_NameCollision_GetsSuffix()
    {
        var library = new CityLibrary(Scenarios.TempDir());
        CitySlot slot = library.Save(Scenarios.GrownResidential(13, 60));

        var package = new MemoryStream();
        CityPackage.Export(slot, package);
        package.Position = 0;

        ImportResult result = CityPackage.Import(package, library);
        Check.True(result.Ok, "import ok");
        Check.NotEqual(slot.CityName, result.Slot!.Value.CityName, "imported copy is renamed on collision");
    }
}
