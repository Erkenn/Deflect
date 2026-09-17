using UnityEngine;

public class PCInputHandler : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        player.SetMovementInput(new Vector2(horizontal, vertical));
    }
}
