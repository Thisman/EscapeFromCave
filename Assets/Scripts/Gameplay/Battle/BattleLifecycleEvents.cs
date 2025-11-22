public record RoundStartedEvent(int RoundNumber);

public record TurnPreparedEvent(IReadOnlySquadModel ActiveUnit, int ActionId);

public record TurnEndedEvent(IReadOnlySquadModel ActiveUnit, int ActionId);

public record BattleFinishedEvent(BattleResult Result);

public record ActionSelectedEvent(IBattleAction Action, IReadOnlySquadModel Actor, int ActionId);

public record ActionCancelledEvent(IReadOnlySquadModel Actor, int ActionId);

public record ActionResolvedEvent(IBattleAction Action, IReadOnlySquadModel Actor, int ActionId);
