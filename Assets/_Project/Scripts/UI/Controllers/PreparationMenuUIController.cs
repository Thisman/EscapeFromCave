using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PreparationMenuUIController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private CarouselWidget _heroCarouselWidget;
    [SerializeField] private CarouselWidget[] _squadCarouselsWidget;

    public Action<UnitDefinitionSO, List<UnitDefinitionSO>> OnStartGame;

    private void OnEnable()
    {
        _startButton.onClick.AddListener(HandleStartGame);
    }

    private void OnDisable()
    {
        _startButton.onClick.RemoveAllListeners();
    }

    private void HandleStartGame()
    {
        UnitDefinitionSO selectedHero = GetSelectedHero();
        List<UnitDefinitionSO> selectedSquads = GetSelectedArmy();
        OnStartGame?.Invoke(selectedHero, selectedSquads);
    }

    private UnitDefinitionSO GetSelectedHero()
    {
        GameObject selectedObject = _heroCarouselWidget.GetCurrentObject();
        if (selectedObject == null)
            return null;

        HeroCarouselItemView heroItemView = selectedObject.GetComponentInChildren<HeroCarouselItemView>();
        if (heroItemView == null)
            return null;

        return heroItemView.Definition;
    }

    private List<UnitDefinitionSO> GetSelectedArmy()
    {
        List<UnitDefinitionSO> selectedSquads = new();

        for (int i = 0; i < _squadCarouselsWidget.Length; i++)
        {
            GameObject selectedObject = _squadCarouselsWidget[i].GetCurrentObject();
            if (selectedObject == null)
                continue;
            SquadCarouselItemView itemView = selectedObject.GetComponentInChildren<SquadCarouselItemView>();
            if (itemView == null)
                continue;

            selectedSquads.Add(itemView.Definition);
        }

        return selectedSquads;
    }
}
