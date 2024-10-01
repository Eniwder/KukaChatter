using UnityEngine;
using Live2D.Cubism.Framework.Raycasting;
using UnityEngine.EventSystems;

public class DraggableObject : MonoBehaviour
{
    private bool isDragging;
    private Vector2 dragStartMouse;
    private Vector3 dragStartObject;

    private void Update()
    {
        if (PlayerPrefs.GetInt("characterPositionToggle", 0) == 1) return;
        if (Input.GetMouseButtonDown(0))
        {
            StartDragging();
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            PerformDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    private void StartDragging()
    {
        var raycaster = GetComponent<CubismRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning("CubismRaycaster component not found.");
            return;
        }

        var results = new CubismRaycastHit[4];
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hitCount = raycaster.Raycast(ray, results);

        if (hitCount < 1 || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        isDragging = true;
        dragStartMouse = Input.mousePosition;
        dragStartObject = Camera.main.WorldToScreenPoint(transform.position);
    }

    private void PerformDrag()
    {
        Vector2 mouseDelta = (Vector2)Input.mousePosition - dragStartMouse;
        Vector3 newScreenPos = dragStartObject + new Vector3(mouseDelta.x, mouseDelta.y, 0);

        // スクリーン座標からワールド座標に変換する際、Z軸の値を設定する
        Vector3 newWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(
                newScreenPos.x,
                newScreenPos.y,
                Camera.main.WorldToScreenPoint(transform.position).z
            )
        );
        transform.position = newWorldPos;
    }

    private void StopDragging()
    {
        isDragging = false;
    }
}
