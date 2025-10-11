using UnityEngine;

public class CameraObject : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        float targetAspect = 9f / 19f;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scale = windowAspect / targetAspect;

        if (scale < 1f)
        {
            Rect rect = cam.rect;
            rect.width = 1f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1f - scale) / 2f;
            cam.rect = rect;
        }
        else
        {
            Rect rect = cam.rect;
            rect.width = 1f / scale;
            rect.height = 1f;
            rect.x = (1f - 1f / scale) / 2f;
            rect.y = 0f;
            cam.rect = rect;
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}