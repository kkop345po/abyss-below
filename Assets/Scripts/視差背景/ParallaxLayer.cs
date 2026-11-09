using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public float parallaxMultiplier = 0.5f;

    

    public bool limitMovement = true;
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -3f;
    public float maxY = 3f;

    private Transform cam;
    private Vector3 lastCamPosition;
    private Vector3 startPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPosition = cam.position;
        startPos = transform.position;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCamPosition;
        Vector3 newPos = transform.position + new Vector3(
            deltaMovement.x * parallaxMultiplier,
            deltaMovement.y * parallaxMultiplier,
            0f
        );

        if (limitMovement)
        {
            newPos.x = Mathf.Clamp(newPos.x, startPos.x + minX, startPos.x + maxX);
            newPos.y = Mathf.Clamp(newPos.y, startPos.y + minY, startPos.y + maxY);
        }

        transform.position = newPos;
        lastCamPosition = cam.position;
    }
}
