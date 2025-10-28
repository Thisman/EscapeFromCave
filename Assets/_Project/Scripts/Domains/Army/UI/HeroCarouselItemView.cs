using UnityEngine;
using UnityEngine.UI;

public class HeroCarouselItemView : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO Definition;
    [SerializeField] private UnitStatsInfoView statsInfoViewUI;
    [SerializeField] private Image icon;
    [SerializeField] private RectTransform abilityList;

    private void Start()
    {
        icon.sprite = Definition.Icon;
        statsInfoViewUI.Render(Definition);
        RenderAbilities();
    }

    public UnitDefinitionSO GetDefinition()
    {
        return Definition;
    }

    private void RenderAbilities()
    {
        if (abilityList == null || Definition == null || Definition.Abilities == null)
        {
            return;
        }

        for (int i = abilityList.childCount - 1; i >= 0; i--)
        {
            Destroy(abilityList.GetChild(i).gameObject);
        }

        foreach (BattleAbilityDefinitionSO ability in Definition.Abilities)
        {
            if (ability == null)
            {
                continue;
            }

            GameObject abilityObject = new("Ability", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = abilityObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64, 64);
            rectTransform.SetParent(abilityList, false);

            Image abilityImage = abilityObject.GetComponent<Image>();
            abilityImage.sprite = ability.Icon;
            abilityImage.preserveAspect = true;
        }
    }
}
