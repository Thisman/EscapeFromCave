using EscapeFromCave.Core.StateMachine;
using UnityEngine;

namespace EscapeFromCave.Gameplay.Battle
{
    public class TakticsState : State<BattleStateContext>
    {
        public override void Enter(BattleStateContext context)
        {
            Debug.Log("TakticsState Entered");
        }
    }
}
