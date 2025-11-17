using System.Threading.Tasks;

public interface IBattleDamageResolver
{
    Task ResolveDamage(IBattleDamageSource actor, IBattleDamageReceiver target);
}
