using UnityEngine;
using UnityEngine.UI;

public class SquadCarouselItemView : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO Definition;
    [SerializeField] private UnitStatsInfoView statsInfoViewUI;
    [SerializeField] private Image icon;

    private void Start()
    {
        Render();
    }

    public void SetDefinition(UnitDefinitionSO definition)
    {
        Definition = definition;
        Render();
    }

    public UnitDefinitionSO GetDefinition()
    {
        return Definition;
    }

    private void Render()
    {
        if (Definition == null)
            return;

        icon.sprite = Definition.Icon;
        statsInfoViewUI.Render(Definition);
    }
}
