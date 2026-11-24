using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothing = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
