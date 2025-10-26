using System.Threading.Tasks;

public interface IBattleDamageResolver
{
    Task ResolveDamage(BattleSquadController actor, BattleSquadController target);
}
