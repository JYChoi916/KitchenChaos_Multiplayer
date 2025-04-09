using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    private Transform targetTransform;

    public void SetTargetTransform(Transform target)
    {
        targetTransform = target;
    }

    private void LateUpdate()
    {
        if (targetTransform == null)
            return;

        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
    }
}
