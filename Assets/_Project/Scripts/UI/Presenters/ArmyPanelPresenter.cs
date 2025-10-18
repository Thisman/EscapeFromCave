using System;
using System.Collections.Generic;

public sealed class ArmyPanelPresenter : IDisposable
{
    private readonly PlayerArmyController _controller;
    private readonly List<SquadViewModel> _viewModels = new();

    public event Action<IReadOnlyList<SquadViewModel>> Updated;

    public ArmyPanelPresenter(PlayerArmyController controller)
    {
        _controller = controller;
        if (_controller != null)
        {
            _controller.ArmyChanged += HandleArmyChanged;
        }
    }

    public void Dispose()
    {
        if (_controller != null)
        {
            _controller.ArmyChanged -= HandleArmyChanged;
        }
        _viewModels.Clear();
    }

    public void RequestRefresh()
    {
        HandleArmyChanged();
    }

    private void HandleArmyChanged()
    {
        _viewModels.Clear();
        if (_controller == null)
        {
            Updated?.Invoke(_viewModels);
            return;
        }

        var squads = _controller.GetSquads();
        if (squads != null)
        {
            foreach (var squad in squads)
            {
                _viewModels.Add(SquadViewModel.FromSquad(squad));
            }
        }

        Updated?.Invoke(_viewModels);
    }
}
