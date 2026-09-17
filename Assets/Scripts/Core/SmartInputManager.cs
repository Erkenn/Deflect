using UnityEngine;

public class SmartInputManager : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject joystickUI;
    [SerializeField] private PCInputHandler pcInputScript;
    [SerializeField] private GameObject mobileCombatButtons;

    [SerializeField] private PlayerCombat playerCombat;

    void Start()
    {
        bool isMobileDevice = Input.touchSupported; 

        if (isMobileDevice)
        {
            joystickUI.SetActive(true);
            pcInputScript.enabled = false;
            mobileCombatButtons.SetActive(true);

            if (playerCombat != null)
            {
                playerCombat.allowKeyboardInput = false;
            }

            Debug.Log("Запущено на мобильном: джойстик + кнопки боя. Клавиатура отключена.");
        }
        else
        {
            joystickUI.SetActive(false);
            pcInputScript.enabled = true;
            mobileCombatButtons.SetActive(false);

            if (playerCombat != null)
            {
                playerCombat.allowKeyboardInput = true;
            }

            Debug.Log("Запущено на ПК: клавиатура. Мобильные кнопки отключены.");
        }
    }
}