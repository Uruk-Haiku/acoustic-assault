using UnityEngine;

public class PlayerCursorMovement : MonoBehaviour
{
    public float clampTop = 300f;
    public float clampBottom = -300f;
    private float previousMouseY;

    void Update()
    {
        // Check if left mouse button is held down
        if (Input.GetMouseButton(0))
        {
            // Get current mouse Y in screen space
            float mouseY = Input.mousePosition.y;

            if (previousMouseY != 0f)
            {
                // Difference in mouse Y since last frame
                float deltaY = mouseY - previousMouseY;

                // Move the transform by deltaY in world units
                Vector3 pos = transform.position;
                pos.y += deltaY;
                // pos.y = Mathf.Clamp(pos.y, clampBottom, clampTop);
                transform.position = pos;
            }

            previousMouseY = mouseY;
        }
        else
        {
            // Reset previousMouseY when button is released
            previousMouseY = 0f;
        }
    }
}