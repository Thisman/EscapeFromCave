using System.Threading.Tasks;

public interface IBattleDamageResolver
{
    Task ResolveDamage(IBattleDamageProvider actor, IBattleDamageReceiver target);
}
