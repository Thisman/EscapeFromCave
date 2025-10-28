using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleAbilityItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;

    public event Action<BattleAbilityDefinitionSO> OnClick;

    private BattleAbilityDefinitionSO definition;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Render(BattleAbilityDefinitionSO abilityDefinition)
    {
        definition = abilityDefinition;

        if (icon != null)
        {
            icon.sprite = definition != null ? definition.Icon : null;
        }
    }

    private void HandleClick()
    {
        OnClick?.Invoke(definition);
    }
}
