using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitOnJoinFix : MonoBehaviour
{
    void Update()
    {
        Application.CancelQuit();
    }
}
