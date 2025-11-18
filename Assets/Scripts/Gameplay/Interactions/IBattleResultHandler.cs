using System.Threading.Tasks;

public interface IBattleResultHandler
{
    Task ApplyResultAsync(InteractionContext ctx, BattleResult result);
}
