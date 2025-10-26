using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 5f;
    public float minOrtho = 1f, maxOrtho = 20f;
    public float panSpeed = 0.005f;

    private Camera cam;
    private Vector3 lastMousePos;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minOrtho, maxOrtho);
        }

        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
        {
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            transform.Translate(-delta.x * panSpeed * cam.orthographicSize, -delta.y * panSpeed * cam.orthographicSize, 0);
            lastMousePos = Input.mousePosition;
        }
    }
}
