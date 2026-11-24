using UnityEngine;

public class ModelTurntable : MonoBehaviour
{
    [SerializeField] private float swayDegrees = 12f;
    [SerializeField] private float swaySpeed = 0.8f;
    [SerializeField] private float bobAmount = 0.12f;

    private Vector3 origin;

    void Start()
    {
        origin = transform.position;
    }

    void Update()
    {
        float t = Time.time * swaySpeed;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * swayDegrees);
        transform.position = origin + Vector3.up * Mathf.Sin(t * 2f) * bobAmount;
    }
}
