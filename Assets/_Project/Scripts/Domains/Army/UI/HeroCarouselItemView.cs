using UnityEngine;
using UnityEngine.UI;

public class HeroCarouselItemView : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO Definition;
    [SerializeField] private UnitStatsInfoView statsInfoViewUI;
    [SerializeField] private Image icon;

    private void Start()
    {
        icon.sprite = Definition.Icon;
        statsInfoViewUI.Render(Definition);
    }

    public UnitDefinitionSO GetDefinition()
    {
        return Definition;
    }
}
