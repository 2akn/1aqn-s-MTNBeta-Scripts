using UnityEngine;
using GorillaNetworking;

public class ClickSounds : MonoBehaviour
{
    [Header("MADE BY 1AQN!!")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public float cooldown = 0.1f;
    private float lastPlayTime;

    void Update()
    {
        if (GorillaTagger.Instance == null || Time.time < lastPlayTime + cooldown) return;

        Transform[] hands = { GorillaTagger.Instance.leftHandTransform, GorillaTagger.Instance.rightHandTransform };

        foreach (Transform hand in hands)
        {
            Collider[] hitColliders = Physics.OverlapSphere(hand.position, GorillaTagger.Instance.sphereCastRadius, -1, QueryTriggerInteraction.Collide);
            foreach (var hit in hitColliders)
            {
                GorillaPressableButton btn = hit.GetComponentInParent<GorillaPressableButton>();
                if (btn != null && Time.time <= btn.touchTime + 0.05f)
                {
                    Play(hit.transform.position);
                    return;
                }

                GorillaKeyboardButton kbd = hit.GetComponentInParent<GorillaKeyboardButton>();
                if (kbd != null && kbd.pressTime > 0f)
                {
                    Play(hit.transform.position);
                    return;
                }

                if (hit.GetComponentInParent<CosmeticStand>() != null || hit.GetComponentInParent<ShoppingCart>() != null)
                {
                    if (btn != null && Time.time <= btn.touchTime + 0.05f)
                    {
                        Play(hit.transform.position);
                        return;
                    }
                }
            }
        }
    }

    void Play(Vector3 pos)
    {
        if (audioSource != null && clickSound != null)
        {
            lastPlayTime = Time.time;
            audioSource.transform.position = pos;
            audioSource.PlayOneShot(clickSound);
        }
    }
}
