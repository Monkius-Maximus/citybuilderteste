namespace CityBuilder.Economy;

/// <summary>
/// Currency amount stored as an integer number of minor units (e.g. cents). Integer money
/// keeps the economy deterministic and free of floating-point drift across machines — a hard
/// requirement for lockstep multiplayer and reproducible saves.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    /// <summary>Amount in minor units (1/100 of a unit).</summary>
    public readonly long Units;

    public Money(long units) => Units = units;

    public static readonly Money Zero = new(0);

    public static Money FromWhole(long whole) => new(whole * 100);

    public decimal ToDecimal() => Units / 100m;

    public static Money operator +(Money a, Money b) => new(a.Units + b.Units);
    public static Money operator -(Money a, Money b) => new(a.Units - b.Units);
    public static Money operator *(Money a, long scalar) => new(a.Units * scalar);
    public static Money operator -(Money a) => new(-a.Units);

    public bool IsPositive => Units > 0;
    public bool IsNegative => Units < 0;

    public int CompareTo(Money other) => Units.CompareTo(other.Units);
    public bool Equals(Money other) => Units == other.Units;
    public override bool Equals(object? obj) => obj is Money m && Equals(m);
    public override int GetHashCode() => Units.GetHashCode();

    public static bool operator ==(Money a, Money b) => a.Units == b.Units;
    public static bool operator !=(Money a, Money b) => a.Units != b.Units;
    public static bool operator <(Money a, Money b) => a.Units < b.Units;
    public static bool operator >(Money a, Money b) => a.Units > b.Units;
    public static bool operator <=(Money a, Money b) => a.Units <= b.Units;
    public static bool operator >=(Money a, Money b) => a.Units >= b.Units;

    public override string ToString() => $"${ToDecimal():0.00}";
}
