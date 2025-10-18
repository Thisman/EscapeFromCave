public class UnitStatsModel
{
    public int Level { get; }
    public int Health { get; }
    public int Damage { get; }
    public int Defense { get; }
    public int Initiative { get; }
    public float Speed { get; }
    public int XPToNext { get; }

    public UnitStatsModel(
        int level,
        int health,
        int damage,
        int defense,
        int initiative,
        float speed,
        int xpToNext)
    {
        Level = level;
        Health = health;
        Damage = damage;
        Defense = defense;
        Initiative = initiative;
        Speed = speed;
        XPToNext = xpToNext;
    }
}
