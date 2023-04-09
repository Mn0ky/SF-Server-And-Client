using System.Runtime.InteropServices;

namespace SF_Server;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct SteamId : IEquatable<SteamId>, IComparable<SteamId>
{
    private readonly ulong id;
    
    public SteamId() => id = 0;
    public SteamId(ulong steamId) => id = steamId;

    public bool Equals(SteamId other) => this == other;

    public int CompareTo(SteamId other) => id.CompareTo(other.id);

    public override bool Equals(object obj) => obj is SteamId steamId && steamId == this;

    public override int GetHashCode() => id.GetHashCode();

    public override string ToString() => id.ToString();

    public static bool operator ==(SteamId x, SteamId y) => (long) x.id == (long) y.id;

    public static bool operator !=(SteamId x, SteamId y) => !(x == y);

    public bool IsBadId() => id == 0;
}