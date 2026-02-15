using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class MosaSpeed : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {
            GorillaLocomotion.Player.Instance.maxJumpSpeed = 999f; // MAX PLAYER SPEED
            GorillaLocomotion.Player.Instance.jumpMultiplier = 1.1f; // PLAYER SPEED MULTIPLIER
    }
}
