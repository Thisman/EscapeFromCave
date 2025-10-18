using UnityEngine;

public class TacticState : State<BattleStateContext>
{
    public override void Enter(BattleStateContext context)
    {
        Debug.Log("TacticState Entered");
    }
}
