using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UI.Widgets
{
    public sealed class SquadInfoPopup : VisualElement
    {
        private const string BodyName = "Body";

        private readonly ScrollView _body;

        public new class UxmlFactory : UxmlFactory<SquadInfoPopup, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
        }

        public SquadInfoPopup()
        {
            _body = new ScrollView
            {
                name = BodyName
            };

            _body.AddToClassList("body");
            _body.mode = ScrollViewMode.Vertical;
            _body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _body.style.flexGrow = 1f;
            Add(_body);
        }

        public void Render(IReadOnlySquadModel model)
        {
            _body.Clear();

            if (model == null)
                return;

            foreach (string entry in BuildEntries(model))
            {
                Label statLabel = new(entry);
                statLabel.AddToClassList("squadStatInfo");
                _body.Add(statLabel);
            }
        }

        private static IEnumerable<string> BuildEntries(IReadOnlySquadModel model)
        {
            if (model == null)
                yield break;

            yield return $"Название: {model.UnitName}";
            yield return $"Количество: {model.Count}";
            yield return $"Здоровье: {FormatValue(model.Health)}";
            yield return $"Физическая защита: {FormatPercent(model.PhysicalDefense)}";
            yield return $"Магическая защита: {FormatPercent(model.MagicDefense)}";
            yield return $"Абсолютная защита: {FormatPercent(model.AbsoluteDefense)}";
            yield return $"Тип атаки: {FormatAttackKind(model.AttackKind)}";
            yield return $"Тип урона: {FormatDamageType(model.DamageType)}";

            (float minDamage, float maxDamage) = model.GetBaseDamageRange();
            yield return $"Урон: {FormatValue(minDamage)} - {FormatValue(maxDamage)}";
            yield return $"Скорость: {FormatValue(model.Speed)}";
            yield return $"Инициатива: {FormatValue(model.Initiative)}";
            yield return $"Шанс критического удара: {FormatPercent(model.CritChance)}";
            yield return $"Критический множитель: {FormatValue(model.CritMultiplier)}";
            yield return $"Шанс промаха: {FormatPercent(model.MissChance)}";
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##");
        }

        private static string FormatPercent(float value)
        {
            return value.ToString("P0");
        }

        private static string FormatAttackKind(AttackKind attackKind)
        {
            return attackKind switch
            {
                AttackKind.Melee => "Ближняя",
                AttackKind.Range => "Дальняя",
                AttackKind.Magic => "Магическая",
                _ => attackKind.ToString()
            };
        }

        private static string FormatDamageType(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Physical => "Физический",
                DamageType.Magical => "Магический",
                DamageType.Pure => "Чистый",
                _ => damageType.ToString()
            };
        }
    }
}
