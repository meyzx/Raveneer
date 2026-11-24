using UnityEngine;

public class WingFlap : MonoBehaviour
{
    [SerializeField] private Transform leftWing;
    [SerializeField] private Transform rightWing;
    [SerializeField] private float speed = 9f;
    [SerializeField] private float depth = 0.35f;

    void Update()
    {
        float stroke = 1f - depth * (0.5f + 0.5f * Mathf.Sin(Time.time * speed));
        if (leftWing != null) leftWing.localScale = new Vector3(stroke, 1f, 1f);
        if (rightWing != null) rightWing.localScale = new Vector3(stroke, 1f, 1f);
    }
}
