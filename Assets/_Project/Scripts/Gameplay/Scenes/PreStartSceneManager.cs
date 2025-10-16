using UnityEngine;
using UnityEngine.UI;

public class PreStartSceneManager : MonoBehaviour
{
    [SerializeField] private CarouselUI _heroCarouselUI;
    [SerializeField] private CarouselUI[] _squadCarouselsUI;
    [SerializeField] private Button _startButton;

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
        GetSelectedHero();
        GetSelectedArmy();
    }

    private void GetSelectedHero()
    {
        GameObject selectedObject = _heroCarouselUI.GetCurrentObject();
        if (selectedObject == null)
            return;

        HeroCarouselItemView heroItemView = selectedObject.GetComponent<HeroCarouselItemView>();
        if (heroItemView == null)
            return;

        UnitDefinitionSO heroDefinition = heroItemView.Definition;
        Debug.Log($"Hero {heroDefinition.UnitName}");
    }

    private void GetSelectedArmy()
    {
        for(int i = 0; i < _squadCarouselsUI.Length; i++)
        {
            GameObject selectedObject = _squadCarouselsUI[i].GetCurrentObject();
            if (selectedObject == null)
                continue;
            SquadCarouselItemView itemView = selectedObject.GetComponent<SquadCarouselItemView>();
            if (itemView == null)
                continue;
            UnitDefinitionSO definition = itemView.Definition;
            Debug.Log($"Squad {i}: {definition.UnitName}");
        }
    }
}
