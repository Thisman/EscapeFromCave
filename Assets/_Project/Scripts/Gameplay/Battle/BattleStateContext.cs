public class BattleStateContext
{
    public BattleSceneLoadingPayload Payload { get; private set; }

    public void SetPayload(BattleSceneLoadingPayload payload)
    {
        Payload = payload;
    }
}
