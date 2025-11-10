using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PreparationMenuUIController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private CarouselWidget _heroCarouselWidget;
    [SerializeField] private CarouselWidget[] _squadCarouselsWidget;
    [SerializeField] private HeroCarouselItemView _heroCarouselItemPrefab;
    [SerializeField] private SquadCarouselItemView _squadCarouselItemPrefab;

    public Action<UnitSO, List<UnitSO>> OnStartGame;

    public void PopulateCarousels(UnitSO[] heroDefinitions, UnitSO[] squadDefinitions)
    {
        PopulateHeroCarousel(heroDefinitions);
        PopulateSquadCarousels(squadDefinitions);
    }

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
        UnitSO selectedHero = GetSelectedHero();
        List<UnitSO> selectedSquads = GetSelectedArmy();
        OnStartGame?.Invoke(selectedHero, selectedSquads);
    }

    private UnitSO GetSelectedHero()
    {
        GameObject selectedObject = _heroCarouselWidget.GetCurrentObject();
        if (selectedObject == null)
            return null;

        HeroCarouselItemView heroItemView = selectedObject.GetComponentInChildren<HeroCarouselItemView>();
        if (heroItemView == null)
            return null;

        return heroItemView.GetDefinition();
    }

    private List<UnitSO> GetSelectedArmy()
    {
        List<UnitSO> selectedSquads = new();

        for (int i = 0; i < _squadCarouselsWidget.Length; i++)
        {
            GameObject selectedObject = _squadCarouselsWidget[i].GetCurrentObject();
            if (selectedObject == null)
                continue;
            SquadCarouselItemView itemView = selectedObject.GetComponentInChildren<SquadCarouselItemView>();
            if (itemView == null)
                continue;

            selectedSquads.Add(itemView.GetDefinition());
        }

        return selectedSquads;
    }

    private void PopulateHeroCarousel(UnitSO[] heroDefinitions)
    {
        if (_heroCarouselWidget == null || _heroCarouselItemPrefab == null)
            return;

        ClearContent(_heroCarouselWidget.Content);

        foreach (UnitSO hero in heroDefinitions)
        {
            if (hero == null)
                continue;

            HeroCarouselItemView itemView = Instantiate(_heroCarouselItemPrefab, _heroCarouselWidget.Content);
            itemView.SetDefinition(hero);
        }

        _heroCarouselWidget.RefreshItems();
    }

    private void PopulateSquadCarousels(UnitSO[] squadDefinitions)
    {
        if (_squadCarouselsWidget == null || _squadCarouselItemPrefab == null)
            return;

        foreach (CarouselWidget carousel in _squadCarouselsWidget)
        {
            if (carousel == null)
                continue;

            ClearContent(carousel.Content);

            foreach (UnitSO squadUnit in squadDefinitions)
            {
                if (squadUnit == null)
                    continue;

                SquadCarouselItemView itemView = Instantiate(_squadCarouselItemPrefab, carousel.Content);
                itemView.SetDefinition(squadUnit);
            }

            carousel.RefreshItems();
        }
    }

    private static void ClearContent(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform childTransform = content.GetChild(i);
            childTransform.SetParent(null, false);

            if (Application.isPlaying)
                Destroy(childTransform.gameObject);
            else
                DestroyImmediate(childTransform.gameObject);
        }
    }
}
