using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum GesturePhase { Began, Moved, Stationary, Ended }

public struct TouchSample
{
    public GesturePhase phase;
    public Vector2 position;
    public Vector2 delta;
}

public static class TouchReader
{
    private static bool pointerWasDown;
    private static Vector2 lastPointer;

    private static bool FromPointer(bool down, Vector2 position, out TouchSample sample)
    {
        sample = default;
        sample.position = position;

        if (down && !pointerWasDown)
        {
            pointerWasDown = true;
            lastPointer = position;
            sample.phase = GesturePhase.Began;
            return true;
        }

        if (down)
        {
            sample.delta = position - lastPointer;
            lastPointer = position;
            sample.phase = sample.delta.sqrMagnitude > 0.01f ? GesturePhase.Moved : GesturePhase.Stationary;
            return true;
        }

        if (pointerWasDown)
        {
            pointerWasDown = false;
            sample.phase = GesturePhase.Ended;
            return true;
        }

        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private static bool trackingTouch;
    private static bool pendingEnd;

    public static bool TryGet(out TouchSample sample)
    {
        sample = default;
        var screen = Touchscreen.current;

        if (screen != null)
        {
            var primary = screen.primaryTouch;
            var phase = primary.phase.ReadValue();
            bool down = phase == UnityEngine.InputSystem.TouchPhase.Began
                || phase == UnityEngine.InputSystem.TouchPhase.Moved
                || phase == UnityEngine.InputSystem.TouchPhase.Stationary;

            if (down)
            {
                sample.position = primary.position.ReadValue();
                sample.delta = primary.delta.ReadValue();

                if (!trackingTouch)
                {
                    trackingTouch = true;
                    sample.phase = GesturePhase.Began;
                }
                else
                {
                    sample.phase = phase == UnityEngine.InputSystem.TouchPhase.Moved ? GesturePhase.Moved : GesturePhase.Stationary;
                }
                return true;
            }

            if (trackingTouch)
            {
                trackingTouch = false;
                sample.position = primary.position.ReadValue();
                sample.phase = GesturePhase.Ended;
                return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse == null) return false;

        Vector2 pointer = mouse.position.ReadValue();

        if (pendingEnd)
        {
            pendingEnd = false;
            pointerWasDown = false;
            sample.position = pointer;
            sample.phase = GesturePhase.Ended;
            return true;
        }

        if (mouse.leftButton.wasPressedThisFrame && !mouse.leftButton.isPressed && !pointerWasDown)
        {
            pointerWasDown = true;
            pendingEnd = true;
            sample.position = pointer;
            sample.phase = GesturePhase.Began;
            return true;
        }

        return FromPointer(mouse.leftButton.isPressed, pointer, out sample);
    }
#elif ENABLE_LEGACY_INPUT_MANAGER
    private static int trackedFinger = -1;
    private static Vector2 lastTouchPosition;

    public static bool TryGet(out TouchSample sample)
    {
        sample = default;

        if (trackedFinger < 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch candidate = Input.GetTouch(i);
                if (candidate.phase != UnityEngine.TouchPhase.Began) continue;

                trackedFinger = candidate.fingerId;
                sample.position = candidate.position;
                sample.phase = GesturePhase.Began;
                lastTouchPosition = candidate.position;
                return true;
            }
        }
        else
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId != trackedFinger) continue;

                sample.position = touch.position;
                sample.delta = touch.deltaPosition;
                lastTouchPosition = touch.position;

                if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                {
                    trackedFinger = -1;
                    sample.phase = GesturePhase.Ended;
                }
                else
                {
                    sample.phase = touch.phase == UnityEngine.TouchPhase.Moved ? GesturePhase.Moved : GesturePhase.Stationary;
                }
                return true;
            }

            trackedFinger = -1;
            sample.position = lastTouchPosition;
            sample.phase = GesturePhase.Ended;
            return true;
        }

        if (Input.touchCount > 0) return false;
        return FromPointer(Input.GetMouseButton(0), Input.mousePosition, out sample);
    }
#else
    public static bool TryGet(out TouchSample sample)
    {
        sample = default;
        return false;
    }
#endif
}
