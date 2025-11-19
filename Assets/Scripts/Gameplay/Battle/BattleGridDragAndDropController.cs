using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BattleGridDragAndDropController : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private string _draggableTag = "Draggable";
    [SerializeField] private BattleGridController _gridController;

    private Transform _originSlot;
    private Transform _hoveredSlot;
    private Transform _draggedObject;
    private Transform _originalParent;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private float _dragPlaneDistance;
    private readonly List<Collider2D> _disabledColliders2D = new();

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    private void Update()
    {
        if (IsPointerPressedThisFrame())
            TryStartDrag();

        if (_draggedObject == null)
            return;

        UpdateDrag();

        if (IsPointerReleasedThisFrame())
            FinishDrag();
    }

    private static bool TryGetPointerScreenPosition(out Vector3 position)
    {
        var mouse = Mouse.current;
        if (mouse == null)
        {
            position = default;
            return false;
        }

        Vector2 pointer = mouse.position.ReadValue();
        position = new Vector3(pointer.x, pointer.y, 0f);
        return true;
    }

    private static bool IsPointerPressedThisFrame()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private static bool IsPointerReleasedThisFrame()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasReleasedThisFrame;
    }

    private void TryStartDrag()
    {
        var draggable = RaycastForDraggable();
        if (draggable == null)
            return;

        if (!CanDrag(draggable))
            return;

        _originalParent = draggable.parent;
        _originalPosition = draggable.position;
        _originalRotation = draggable.rotation;

        _gridController.TryRemoveOccupant(draggable, out _originSlot);

        _draggedObject = draggable;
        _draggedObject.SetParent(null, true);

        DisableDragObjectColliders();

        CalculateDragPlaneDistance();
        UpdateDraggedObjectPosition();
    }

    private void UpdateDrag()
    {
        UpdateDraggedObjectPosition();

        var newHoveredSlot = FindSlotUnderPointer();
        if (newHoveredSlot != _hoveredSlot)
        {
            ClearHoveredSlotHighlight();
            _hoveredSlot = newHoveredSlot;
        }

        if (_hoveredSlot != null)
        {
            bool isValid = TryGetPlacementInfo(_hoveredSlot, out _, out _);
            _gridController.HighlightSlot(
                _hoveredSlot,
                isValid ? BattleGridSlotHighlightMode.Available : BattleGridSlotHighlightMode.Unavailable);
        }
    }

    private void FinishDrag()
    {
        var targetSlot = _hoveredSlot ?? FindSlotUnderPointer();
        bool placed = false;

        if (targetSlot != null && TryGetPlacementInfo(targetSlot, out var resolvedSlot, out var occupantToSwap))
        {
            placed = occupantToSwap != null
                ? TrySwapWithOccupant(resolvedSlot, occupantToSwap)
                : _gridController.TryAttachToSlot(resolvedSlot, _draggedObject);
        }

        if (!placed)
        {
            if (_originSlot != null)
            {
                _gridController.TryAttachToSlot(_originSlot, _draggedObject);
            }
            else
            {
                _draggedObject.SetParent(_originalParent, true);
                _draggedObject.position = _originalPosition;
                _draggedObject.rotation = _originalRotation;
            }
        }

        RestoreDragObjectColliders();
        ClearHoveredSlotHighlight();

        _draggedObject = null;
        _originSlot = null;
        _hoveredSlot = null;
    }

    private void ClearHoveredSlotHighlight()
    {
        if (_hoveredSlot != null)
        {
            _gridController.ResetSlotHighlight(_hoveredSlot);
        }
    }

    private void DisableDragObjectColliders()
    {
        _disabledColliders2D.Clear();

        if (_draggedObject == null)
            return;

        foreach (var collider2D in _draggedObject.GetComponentsInChildren<Collider2D>())
        {
            if (collider2D != null && collider2D.enabled)
            {
                _disabledColliders2D.Add(collider2D);
                collider2D.enabled = false;
            }
        }
    }

    private void RestoreDragObjectColliders()
    {
        foreach (var collider2D in _disabledColliders2D)
        {
            if (collider2D != null)
                collider2D.enabled = true;
        }

        _disabledColliders2D.Clear();
    }

    private void CalculateDragPlaneDistance()
    {
        if (_draggedObject == null || _camera == null)
            return;

        if (_camera.orthographic)
        {
            _dragPlaneDistance = _draggedObject.position.z - _camera.transform.position.z;
        }
        else
        {
            Vector3 cameraForward = _camera.transform.forward;
            _dragPlaneDistance = Vector3.Dot(_draggedObject.position - _camera.transform.position, cameraForward);
        }
    }

    private void UpdateDraggedObjectPosition()
    {
        if (!TryGetPointerScreenPosition(out var pointerPosition))
            return;

        Vector3 mousePosition = pointerPosition;

        if (_camera.orthographic)
        {
            mousePosition.z = _dragPlaneDistance;
            Vector3 worldPosition = _camera.ScreenToWorldPoint(mousePosition);
            worldPosition.z = _draggedObject.position.z;
            _draggedObject.position = worldPosition;
        }
        else
        {
            var ray = _camera.ScreenPointToRay(mousePosition);
            Vector3 worldPosition = ray.origin + ray.direction * _dragPlaneDistance;
            _draggedObject.position = worldPosition;
        }
    }

    private Transform FindSlotUnderPointer()
    {
        var hit = RaycastForTransform();
        if (hit == null)
            return null;

        return _gridController.TryResolveSlot(hit, out var slot) ? slot : null;
    }

    private Transform RaycastForDraggable()
    {
        if (_camera == null)
            return null;

        if (!TryGetPointerScreenPosition(out var pointerPosition))
            return null;

        Ray ray = _camera.ScreenPointToRay(pointerPosition);
        var hits2D = Physics2D.GetRayIntersectionAll(ray);

        foreach (var hit in hits2D)
        {
            if (hit.collider == null)
                continue;

            var draggable = ResolveDraggable(hit.transform);
            if (draggable != null)
                return draggable;
        }

        return null;
    }

    private Transform RaycastForTransform()
    {
        if (_camera == null)
            return null;

        if (!TryGetPointerScreenPosition(out var pointerPosition))
            return null;

        Ray ray = _camera.ScreenPointToRay(pointerPosition);

        var hit2D = Physics2D.GetRayIntersection(ray);
        if (hit2D.collider != null)
            return hit2D.transform;

        return null;
    }

    private Transform ResolveDraggable(Transform start)
    {
        if (start == null)
            return null;

        if (string.IsNullOrEmpty(_draggableTag))
            return start;

        for (Transform current = start; current != null; current = current.parent)
        {
            if (current.CompareTag(_draggableTag))
                return current;
        }

        return null;
    }

    private bool CanDrag(Transform draggable)
    {
        if (draggable == null)
            return false;

        var unitController = draggable.GetComponentInParent<BattleSquadController>();
        var squadModel = unitController.GetSquadModel();

        return squadModel.IsFriendly();
    }

    private bool TryGetPlacementInfo(Transform slot, out Transform resolvedSlot, out Transform occupantToSwap)
    {
        resolvedSlot = null;
        occupantToSwap = null;

        if (_gridController == null || _draggedObject == null)
            return false;

        if (!_gridController.TryResolveSlot(slot, out resolvedSlot))
            return false;

        if (!_gridController.TryGetSlotSide(resolvedSlot, out var side))
            return false;

        var unitController = _draggedObject.GetComponent<BattleSquadController>();
        if (unitController == null)
            return false;

        var squadModel = unitController.GetSquadModel();
        if (squadModel == null)
            return false;

        bool canOccupySide = side switch
        {
            BattleGridSlotSide.Ally => squadModel.Kind == UnitKind.Ally || squadModel.Kind == UnitKind.Hero,
            BattleGridSlotSide.Enemy => squadModel.Kind == UnitKind.Enemy,
            _ => false
        };

        if (!canOccupySide)
            return false;

        if (_gridController.IsSlotEmpty(resolvedSlot))
            return true;

        if (!_gridController.TryGetSlotOccupant(resolvedSlot, out var occupant) || occupant == null)
            return false;

        if (_originSlot == null)
            return false;

        if (!IsFriendlyOccupant(occupant))
            return false;

        occupantToSwap = occupant;
        return true;
    }

    private bool IsFriendlyOccupant(Transform occupant)
    {
        if (occupant == null)
            return false;

        var occupantController = occupant.GetComponent<BattleSquadController>();
        var occupantModel = occupantController?.GetSquadModel();
        return occupantModel != null && occupantModel.IsFriendly();
    }

    private bool TrySwapWithOccupant(Transform targetSlot, Transform occupantToSwap)
    {
        if (_gridController == null || _draggedObject == null)
            return false;

        if (_originSlot == null || targetSlot == null || occupantToSwap == null)
            return false;

        if (!_gridController.TryRemoveOccupant(occupantToSwap, out _))
            return false;

        if (!_gridController.TryAttachToSlot(_originSlot, occupantToSwap))
        {
            _gridController.TryAttachToSlot(targetSlot, occupantToSwap);
            return false;
        }

        if (_gridController.TryAttachToSlot(targetSlot, _draggedObject))
            return true;

        _gridController.TryRemoveOccupant(occupantToSwap, out _);
        _gridController.TryAttachToSlot(targetSlot, occupantToSwap);
        return false;
    }
}
