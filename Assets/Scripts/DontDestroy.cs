using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroy : MonoBehaviour
{
    private const string targetTag = "DontDestroy";

    private void Awake()
    {
        GameObject[] objList = GameObject.FindGameObjectsWithTag(targetTag);

        if (objList.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
}
