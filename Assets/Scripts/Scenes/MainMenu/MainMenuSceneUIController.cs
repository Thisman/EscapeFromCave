using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public enum MainMenuSceneElements
{
    Root,
    StartButton,
    QuitButton,
}

public class MainMenuSceneUIController : BaseUIController<MainMenuSceneElements>
{
    override protected void RegisterUIElements()
    {
        _uiElements[MainMenuSceneElements.Root] = _uiDocument.rootVisualElement;
        _uiElements[MainMenuSceneElements.StartButton] = GetElement<VisualElement>(MainMenuSceneElements.Root)
            .Q<Button>("StartGameButton");
    }

    override protected void SubcribeToUIEvents()
    {
        GetElement<Button>(MainMenuSceneElements.StartButton).clicked += HandleStartButtonClicked;
    }

    override protected void UnsubscriveFromUIEvents()
    {
        GetElement<Button>(MainMenuSceneElements.StartButton).clicked -= HandleStartButtonClicked;
    }

    override protected void SubscriveToGameEvents() { }

    override protected void UnsubscribeFromGameEvents() { }

    private void HandleStartButtonClicked()
    {
        _sceneEventBusService.Publish(new RequestGameStart());
    }
}
