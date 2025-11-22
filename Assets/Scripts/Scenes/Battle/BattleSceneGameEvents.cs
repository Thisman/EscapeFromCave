using UnityEngine;

public record RequestStartCombat();

public record RequestFleeCombat();

public record RequestReturnToDungeon();

public record RequestDefend();

public record RequestSkipTurn();

public record RequestSelectAbility(BattleAbilitySO Ability);