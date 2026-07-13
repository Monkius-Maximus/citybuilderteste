using CityBuilder.Library;

namespace CityBuilder.Shell;

/// <summary>The pre-game screens, matching the prototype's state model (menu|new|load|settings|game).</summary>
public enum ShellScreen : byte
{
    Title = 0,
    NewCity = 1,
    LoadCity = 2,
    Settings = 3,
    InGame = 4,
}

/// <summary>
/// Engine-agnostic state machine for the main-menu suite. The frontend renders whatever
/// <see cref="Screen"/> says (styling from <c>AegeanMarbleTheme</c>, copy from <c>GameInfo</c>)
/// and calls these navigation methods from its buttons; it never owns flow logic itself, so the
/// exact same menu behaviour runs under Unity, Godot or a test. Esc = <see cref="Back"/>.
/// </summary>
public sealed class GameShell
{
    public GameShell(GameSettings? settings = null, NewCityForm? newCityForm = null)
    {
        Settings = settings ?? new GameSettings();
        NewCity = newCityForm ?? new NewCityForm();
    }

    public ShellScreen Screen { get; private set; } = ShellScreen.Title;

    /// <summary>Fires on every navigation so the view can run its fade/rise transition.</summary>
    public event Action<ShellScreen>? ScreenChanged;

    /// <summary>FOUND CITY was clicked: the host constructs the simulation from this config.</summary>
    public event Action<GameConfig>? CityFounded;

    /// <summary>LOAD was clicked on a save row: the host restores the simulation from the slot.</summary>
    public event Action<CitySlot>? LoadRequested;

    // Load City row context actions (the affordances the design handoff reserved). The shell
    // only signals; the host owns the CityLibrary and performs the mutation.

    public event Action<CitySlot>? RenameRequested;
    public event Action<CitySlot>? DeleteRequested;

    public void RequestRename(in CitySlot slot) => RenameRequested?.Invoke(slot);

    public void RequestDelete(in CitySlot slot) => DeleteRequested?.Invoke(slot);

    public NewCityForm NewCity { get; }

    public GameSettings Settings { get; }

    // --- Title menu ---

    public void OpenNewCity() => Transition(ShellScreen.NewCity);

    public void OpenLoadCity() => Transition(ShellScreen.LoadCity);

    public void OpenSettings()
    {
        Settings.BeginEdit(); // so BACK can discard
        Transition(ShellScreen.Settings);
    }

    /// <summary>CONTINUE — straight into the most recent city (host resolves which).</summary>
    public void Continue() => Transition(ShellScreen.InGame);

    // --- Modal actions ---

    /// <summary>BACK from any modal (and Esc). Settings discards unapplied edits, per the design.</summary>
    public void Back()
    {
        if (Screen == ShellScreen.Settings)
        {
            Settings.Discard();
        }

        Transition(ShellScreen.Title);
    }

    /// <summary>APPLY on the Settings screen: commit and return to the menu.</summary>
    public void ApplySettings()
    {
        Settings.Apply();
        Transition(ShellScreen.Title);
    }

    /// <summary>FOUND CITY on the New City screen.</summary>
    public GameConfig FoundCity()
    {
        GameConfig config = NewCity.CreateConfig();
        Transition(ShellScreen.InGame);
        CityFounded?.Invoke(config);
        return config;
    }

    /// <summary>LOAD on a Load City row.</summary>
    public void LoadCity(in CitySlot slot)
    {
        Transition(ShellScreen.InGame);
        LoadRequested?.Invoke(slot);
    }

    /// <summary>BACK TO MENU from in-game.</summary>
    public void ExitToTitle() => Transition(ShellScreen.Title);

    private void Transition(ShellScreen next)
    {
        if (Screen == next)
        {
            return;
        }

        Screen = next;
        ScreenChanged?.Invoke(next);
    }
}
