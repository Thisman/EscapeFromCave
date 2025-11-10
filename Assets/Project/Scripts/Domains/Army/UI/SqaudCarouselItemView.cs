using UnityEngine;
using UnityEngine.UI;

public class SquadCarouselItemView : MonoBehaviour
{
    [SerializeField] private UnitSO Definition;
    [SerializeField] private UnitStatsInfoView statsInfoViewUI;
    [SerializeField] private Image icon;

    private void Start()
    {
        Render();
    }

    public void SetDefinition(UnitSO definition)
    {
        Definition = definition;
        Render();
    }

    public UnitSO GetDefinition()
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
