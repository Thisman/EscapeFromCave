using System.Collections.Generic;
using UnityEngine;

public abstract class ValidatedMonoBehaviour : MonoBehaviour
{
    protected abstract void Validate();

    protected virtual void Awake()
    {
        Validate();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!Application.isPlaying)
            Validate();
    }
#endif

    protected void RequireNotNull(object obj, string fieldName)
    {
        if (obj != null)
            return;

        var message = $"{GetType().Name}: field '{fieldName}' is not assigned on '{GetHierarchyPath()}'";
        Debug.LogError(message, this);
    }

    private string GetHierarchyPath()
    {
        var names = new List<string>();
        var current = transform;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }
}
