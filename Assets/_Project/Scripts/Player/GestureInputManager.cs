using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GestureInputManager : MonoBehaviour
{
    private const float REFERENCE_DPI = 160f;
    private const float TAP_RADIUS = 0.6f;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private ARCameraToggle arToggle;
    [SerializeField] private Camera inputCamera;
    [SerializeField] private float doubleTapWindow = 0.45f;
    [SerializeField] private float doubleTapRadiusInches = 0.6f;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float tapMaxDuration = 0.35f;
    [SerializeField] private float slopInches = 0.12f;

    private bool touching;
    private bool ignoreTouch;
    private bool swiping;
    private bool holdFired;
    private bool suppressTap;
    private float touchTime;
    private float lastTapTime = -10f;
    private Vector2 startPosition;
    private Vector2 lastPosition;
    private Vector2 lastTapPosition;

    private bool ARActive => arToggle != null && arToggle.IsARActive;
    private static float Dpi => Screen.dpi > 0f ? Screen.dpi : REFERENCE_DPI;

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            touching = false;
            return;
        }

        if (!TouchReader.TryGet(out TouchSample sample)) return;

        switch (sample.phase)
        {
            case GesturePhase.Began:
                Begin(sample.position);
                break;
            case GesturePhase.Ended:
                End();
                break;
            default:
                Hold(sample.position);
                break;
        }
    }

    private void Begin(Vector2 position)
    {
        ignoreTouch = IsOverUI(position);
        touching = !ignoreTouch;
        if (ignoreTouch) return;

        swiping = false;
        holdFired = false;
        suppressTap = false;
        touchTime = 0f;
        startPosition = position;
        lastPosition = position;

        bool inTime = Time.unscaledTime - lastTapTime <= doubleTapWindow;
        bool inRange = (position - lastTapPosition).magnitude <= doubleTapRadiusInches * Dpi;
        if (!inTime || !inRange) return;

        lastTapTime = -10f;
        holdFired = true;
        suppressTap = true;
        OnDoubleTap(position);
    }

    private void Hold(Vector2 position)
    {
        if (!touching) return;

        touchTime += Time.unscaledDeltaTime;

        if (!swiping && (position - startPosition).magnitude > slopInches * Dpi)
        {
            swiping = true;
            lastPosition = position;
        }

        if (swiping)
        {
            Vector2 delta = (position - lastPosition) * (REFERENCE_DPI / Dpi);
            lastPosition = position;

            if (ARActive)
            {
                if (arToggle.CanSwipeLook) arToggle.SimulateLook(delta);
            }
            else playerController.ProcessSwipe(delta);
            return;
        }

        if (!holdFired && touchTime >= holdDuration)
        {
            holdFired = true;
            suppressTap = true;
            playerController.TryThrowItem();
        }
    }

    private void End()
    {
        bool wasTouching = touching;
        touching = false;
        ignoreTouch = false;

        if (!wasTouching || swiping || suppressTap || touchTime > tapMaxDuration) return;

        lastTapTime = Time.unscaledTime;
        lastTapPosition = startPosition;
        OnSingleTap(startPosition);
    }

    private void OnDoubleTap(Vector2 position)
    {
        if (ARActive)
        {
            arToggle.TryCapture(position, playerController);
            return;
        }

        playerController.TryPickUpItem();
    }

    private void OnSingleTap(Vector2 position)
    {
        if (ARActive)
        {
            arToggle.TryCapture(position, playerController);
            return;
        }

        Camera cam = inputCamera != null ? inputCamera : Camera.main;
        if (cam == null) return;

        Vector2 world = cam.ScreenToWorldPoint(position);
        foreach (var hit in Physics2D.OverlapCircleAll(world, TAP_RADIUS))
        {
            var note = hit.GetComponentInParent<NoteReadable>();
            if (note == null) continue;

            note.Read();
            return;
        }
    }

    private static bool IsOverUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = position };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }
}
