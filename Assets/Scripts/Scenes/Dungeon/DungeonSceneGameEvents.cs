using System.Collections.Generic;
using UnityEngine;

public record RequestPlayerUpgrade();

public record SelectSquadUpgrade();

public record PlayerSquadsChanged(IReadOnlyList<IReadOnlySquadModel> Squads);

public record RequestDialogShow(string Dialog);

public record RequestBattle();

public record BattleEnded();