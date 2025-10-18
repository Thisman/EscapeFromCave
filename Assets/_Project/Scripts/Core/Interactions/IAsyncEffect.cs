using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IAsyncEffect
{
    Task ApplyAsync(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
