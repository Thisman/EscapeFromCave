using System.Threading.Tasks;

public interface IBattleDamageReceiver
{
    Task ApplyDamage(BattleDamageData damageData);
}
