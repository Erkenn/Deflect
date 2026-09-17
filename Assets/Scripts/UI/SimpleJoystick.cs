using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleJoystick : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private PlayerController player;
    [SerializeField] private float joystickRange = 50f;

    private Vector2 inputVector;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out pos);

        pos = Vector2.ClampMagnitude(pos, joystickRange);
        transform.localPosition = pos;

        inputVector = pos / joystickRange;
        player.SetMovementInput(inputVector);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.localPosition = Vector3.zero;
        inputVector = Vector2.zero;
        player.SetMovementInput(Vector2.zero);
    }
}