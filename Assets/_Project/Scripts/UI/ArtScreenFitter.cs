using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AspectRatioFitter))]
public class ArtScreenFitter : MonoBehaviour
{
    [SerializeField] private float artAspect = 0.559f;

    private AspectRatioFitter fitter;

    void Awake()
    {
        fitter = GetComponent<AspectRatioFitter>();
        Apply();
    }

    void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (fitter == null || Screen.height <= 0) return;

        float screen = Screen.width / (float)Screen.height;
        var mode = screen < artAspect ? AspectRatioFitter.AspectMode.EnvelopeParent : AspectRatioFitter.AspectMode.FitInParent;
        if (fitter.aspectMode != mode) fitter.aspectMode = mode;
    }
}
