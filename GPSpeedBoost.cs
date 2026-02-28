using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GorillaLocomotion;

public class GPSpeedBoost : MonoBehaviour
{
    public float baseJumpMultiplier = 1.1f;
    public float maxJumpMultiplier = 2.5f;
    public float acceleration = 0.1f;

    private float currentMultiplier;

    void Start()
    {
        currentMultiplier = baseJumpMultiplier;
    }

    void Update()
    {
        Player player = Player.Instance;
        if (player == null) return;

        player.maxJumpSpeed = 999f;

        float velocity = player.GetComponent<Rigidbody>().velocity.magnitude;

        if (velocity > 1f && (player.wasLeftHandTouching || player.wasRightHandTouching))
        {
            currentMultiplier += acceleration * (velocity / 5f) * Time.deltaTime;
        }
        else if (velocity < 0.5f)
        {
            currentMultiplier = Mathf.MoveTowards(currentMultiplier, baseJumpMultiplier, Time.deltaTime);
        }

        currentMultiplier = Mathf.Clamp(currentMultiplier, baseJumpMultiplier, maxJumpMultiplier);
        player.jumpMultiplier = currentMultiplier;
    }
}