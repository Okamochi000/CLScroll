using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CLScrollサンプル
/// </summary>
public class CLScrollSample : MonoBehaviour
{
    [SerializeField] private CLScrollSync clScrollSync = null;
    [SerializeField] private CLScroll[] syncTargets = null;

    // Start is called before the first frame update
    void Start()
    {
        if (clScrollSync != null)
        {
            clScrollSync.AddCLScrolls(syncTargets);
        }
    }
}
