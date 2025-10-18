using EscapeFromCave.Core.StateMachine;
using UnityEngine;

public class TakticsState : State<BattleStateContext>
{
    public override void Enter(BattleStateContext context)
    {
        Debug.Log("TakticsState Entered");
    }
}
