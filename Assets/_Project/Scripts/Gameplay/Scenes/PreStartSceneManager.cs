using UnityEngine;
using UnityEngine.UI;
using VContainer;
using System.Collections.Generic;

public class PreStartSceneManager : MonoBehaviour
{
    [SerializeField] private CarouselUI _heroCarouselUI;
    [SerializeField] private CarouselUI[] _squadCarouselsUI;
    [SerializeField] private Button _startButton;

    [Inject] IGameSession _gameSession;
    [Inject] GameFlowService _gameFlowService;
    [Inject] SceneLoader _sceneLoader;

    private void Start()
    {
        _gameFlowService.EnterGameplay();
    }

    private void OnEnable()
    {
        _startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnDisable()
    {
        _startButton.onClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        UnitDefinitionSO selectedHero = GetSelectedHero();
        List<UnitDefinitionSO> selectedSquads = GetSelectedArmy();

        _gameSession.SetSelection(selectedHero, selectedSquads);
        _sceneLoader.LoadScene("Cave_Level_1");
    }

    private UnitDefinitionSO GetSelectedHero()
    {
        GameObject selectedObject = _heroCarouselUI.GetCurrentObject();
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

        for(int i = 0; i < _squadCarouselsUI.Length; i++)
        {
            GameObject selectedObject = _squadCarouselsUI[i].GetCurrentObject();
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
