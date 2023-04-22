namespace SF_Server;

public struct PositionPackage
{
    public Vector3 Position;
    public Vector2 Rotation;
    public sbyte YValue;
    public byte MovementType;
    
    public static int ByteSize => 11;
    
    public override string ToString() 
        => $"Position: {Position} Rotation: {Rotation}\nYValue: {YValue}\nMovementType: {MovementType}";
}

public struct WeaponPackage
{
    public byte WeaponType;
    public byte FightState;

    public ProjectilePackage[] ProjectilePackages;
    
    public override string ToString() => $"WeaponType: {WeaponType} FightState: {FightState}";
}

public struct ProjectilePackage
{
    public Vector2 ShootPosition;
    public Vector2 ShootVector;
    public ushort SyncIndex;

    public ProjectilePackage(Vector2 shootPosition, Vector2 shootVector, ushort syncIndex)
    {
        ShootPosition = shootPosition;
        ShootVector = shootVector;
        SyncIndex = syncIndex;
    }

    public static int ByteSize => 8;
}

public enum MovementStateEnum : byte
{
    None = 0,
    Left = 1,
    Right = 2,
    WallJump = 4,
    GroundJump = 8
}
