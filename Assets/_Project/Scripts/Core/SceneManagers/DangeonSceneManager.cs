using UnityEngine;
using VContainer;

public class DangeonSceneManager : MonoBehaviour
{
    [Inject] private readonly InputRouter _inputRouter;

    public void Start()
    {
        _inputRouter.EnterGameplay();
    }
}
