using BattleRoyale.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BattleRoyale.Input
{
    [DisallowMultipleComponent]
    public sealed class ClickMoveController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private RectTransform viewRect;
        [SerializeField] private UnitMovement controlledUnit;
        [SerializeField] private bool ignoreClicksOverUI = true;

        [Header("Ground Raycast")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Min(1f)]
        [SerializeField] private float raycastDistance = 5000f;

        [Header("Destination Feedback")]
        [SerializeField] private Transform destinationMarker;
        [Min(0f)]
        [SerializeField] private float markerHeightOffset = 0.05f;
        [SerializeField] private bool hideMarkerWhenMoveIsRejected = true;

        private void Reset()
        {
            inputCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (inputCamera == null)
            {
                inputCamera = GetComponent<Camera>();
            }

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (ignoreClicksOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (inputCamera == null || controlledUnit == null)
            {
                return;
            }

            var screenPosition = Mouse.current.position.ReadValue();
            var ray = PointerRay(screenPosition);
            if (!Physics.Raycast(
                    ray,
                    out var hit,
                    raycastDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                HideDestinationMarker();
                return;
            }

            if (!controlledUnit.TryMoveTo(hit.point, out var acceptedDestination))
            {
                if (hideMarkerWhenMoveIsRejected)
                {
                    HideDestinationMarker();
                }

                return;
            }

            ShowDestinationMarker(acceptedDestination);
        }

        private Ray PointerRay(Vector2 screenPosition)
        {
            if (viewRect == null || !viewRect.gameObject.activeInHierarchy)
            {
                return inputCamera.ScreenPointToRay(screenPosition);
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewRect,
                    screenPosition,
                    null,
                    out var local))
            {
                return inputCamera.ScreenPointToRay(screenPosition);
            }

            var rect = viewRect.rect;
            var x = rect.width <= 0f ? 0.5f : (local.x - rect.xMin) / rect.width;
            var y = rect.height <= 0f ? 0.5f : (local.y - rect.yMin) / rect.height;
            return inputCamera.ViewportPointToRay(new Vector3(x, y, 0f));
        }

        private void ShowDestinationMarker(Vector3 destination)
        {
            if (destinationMarker == null)
            {
                return;
            }

            destinationMarker.position =
                destination + Vector3.up * markerHeightOffset;
            destinationMarker.gameObject.SetActive(true);
        }

        private void HideDestinationMarker()
        {
            if (destinationMarker != null)
            {
                destinationMarker.gameObject.SetActive(false);
            }
        }

        private void OnValidate()
        {
            raycastDistance = Mathf.Max(1f, raycastDistance);
            markerHeightOffset = Mathf.Max(0f, markerHeightOffset);
        }
    }
}
