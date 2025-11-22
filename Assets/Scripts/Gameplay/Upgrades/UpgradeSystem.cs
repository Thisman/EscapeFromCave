using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem
{
    private readonly PlayerController _playerController;
    private readonly PlayerArmyController _playerArmyController;

    public UpgradeSystem(PlayerController playerController, PlayerArmyController playerArmyController)
    {
        _playerController = playerController ?? throw new ArgumentNullException(nameof(playerController));
        _playerArmyController = playerArmyController ?? throw new ArgumentNullException(nameof(playerArmyController));
    }

    public List<UpgradeModel> GenerateRandomUpgrades(int count = 3)
    {
        List<SquadModel> squads = CollectAvailableSquads();
        var upgrades = new List<UpgradeModel>(count);

        if (squads.Count == 0)
            return upgrades;

        for (int i = 0; i < count; i++)
        {
            SquadModel target = squads[UnityEngine.Random.Range(0, squads.Count)];
            SquadUpgradeModifier modifier = GenerateModifier();
            string description = BuildDescription(target, modifier);
            upgrades.Add(new UpgradeModel(target, new[] { modifier }, description));
        }

        return upgrades;
    }

    private List<SquadModel> CollectAvailableSquads()
    {
        var squads = new List<SquadModel>();

        if (_playerController != null)
        {
            var player = _playerController.GetSquadModel();
            if (player != null)
                squads.Add(player);
        }

        IReadOnlyList<IReadOnlySquadModel> armySquads = _playerArmyController?.Army?.GetSquads();
        if (armySquads != null)
        {
            for (int i = 0; i < armySquads.Count; i++)
            {
                if (armySquads[i] is SquadModel squad && squad != null && !squad.IsEmpty)
                    squads.Add(squad);
            }
        }

        return squads;
    }

    private SquadUpgradeModifier GenerateModifier()
    {
        Array statValues = Enum.GetValues(typeof(SquadUpgradeStat));
        var stat = (SquadUpgradeStat)statValues.GetValue(UnityEngine.Random.Range(0, statValues.Length));
        float amount = stat switch
        {
            SquadUpgradeStat.Health => UnityEngine.Random.Range(10f, 30f),
            SquadUpgradeStat.MinDamage => UnityEngine.Random.Range(2f, 5f),
            SquadUpgradeStat.MaxDamage => UnityEngine.Random.Range(3f, 7f),
            SquadUpgradeStat.Speed => UnityEngine.Random.Range(0.1f, 0.5f),
            SquadUpgradeStat.CritMultiplier => UnityEngine.Random.Range(0.05f, 0.15f),
            _ => UnityEngine.Random.Range(0.02f, 0.1f)
        };

        return new SquadUpgradeModifier(stat, amount);
    }

    private string BuildDescription(IReadOnlySquadModel target, SquadUpgradeModifier modifier)
    {
        string statName = modifier.Stat.ToString();
        string value = $"+{modifier.Value:0.##}";
        string targetName = target?.UnitName ?? "Unit";
        return $"<b>{targetName}</b> получает {value} к {statName}";
    }
}
