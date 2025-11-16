using System;

public interface IBattleAction
{
    event Action OnResolve;
    event Action OnCancel;

    void Resolve();
}
