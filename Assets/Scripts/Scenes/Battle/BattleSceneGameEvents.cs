using UnityEngine;

public record RequestStartCombat();

public record RequestFleeCombat();

public record RequestReturnToDungeon();

public record RequestDefend();

public record RequestSkipTurn();

public record RequestSelectAbility(BattleAbilitySO Ability);

public record BattleRoundStarted(int RoundNumber);

public record BattleTurnInited(IReadOnlySquadModel ActiveUnit, int ActionId);

public record BattleTurnEnded(IReadOnlySquadModel ActiveUnit, int ActionId);

public record BattleFinished(BattleResult Result);

public record BattleActionSelected(IBattleAction Action, IReadOnlySquadModel Actor, int ActionId);

public record BattleActionCancelled(IReadOnlySquadModel Actor, int ActionId);

public record BattleActionResolved(IBattleAction Action, IReadOnlySquadModel Actor, int ActionId);
