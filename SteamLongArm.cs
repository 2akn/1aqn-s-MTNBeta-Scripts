using UnityEngine;

public class SteamLongArm : MonoBehaviour
{
    public GameObject targetObject;

    private Vector3 originalScale;
    private Vector3 resetScale;

    private void Awake()
    {
        originalScale = targetObject.transform.localScale;
        resetScale = new Vector3(1.2f, 1.2f, 1.2f);
        ResizeObject();
    }

    private void OnEnable()
    {
        ResizeObject();
    }

    private void OnDisable()
    {
        ResetObject();
    }

    private void ResizeObject()
    {
        targetObject.transform.localScale = resetScale;
    }

    private void ResetObject()
    {
        targetObject.transform.localScale = originalScale;
    }
}
