using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private GameObject mobileCombatButtons;
    [SerializeField] private GameObject talentSelectionUI;

    [Header("Ссылки на игровые объекты")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<Transform> enemySpawnPoints;

    [Header("Настройки")]
    [SerializeField] private float hintDisplayTime = 2f;

    private bool isTutorialActive = true;
    private int tutorialStep = 0;

    private HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>();
    private float movementHintTimer = 0f;

    private EnemyController currentTutorialEnemy;
    private bool isWaitingForAction = false;
    private bool isTransitioning = false;
    private bool hasShownParryHint = false;
    private bool hasShownDodgeHint = false;
    private bool hasShownDodgeSuccess = false;
    private bool hasShownParrySuccess = false;

    void Start()
    {
        if (enemyPrefab == null) Debug.LogError("Enemy Prefab не назначен!");
        if (enemySpawnPoints == null || enemySpawnPoints.Count < 3) Debug.LogError("sНужно минимум 3 точки спавна!");

        if (talentSelectionUI != null) talentSelectionUI.SetActive(false);

        bool isMobile = Input.touchSupported;
        if (isMobile)
        {
            mobileCombatButtons.SetActive(true);
            tutorialText.text = "Используй джойстик слева для движения";
        }
        else
        {
            mobileCombatButtons.SetActive(false);
            tutorialText.text = "Нажми W, A, S, D чтобы двигаться";
        }

        darkOverlay.SetActive(true);
        tutorialText.gameObject.SetActive(true);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!isTutorialActive) return;

        switch (tutorialStep)
        {
            case 0: CheckMovementTutorial(); break;
            case 1: CheckFirstAttackTutorial(); break;
            case 2: CheckParryTutorial(); break;
            case 3: CheckDodgeTutorial(); break;
            case 4: EndTutorial(); break;
        }
    }

    private void CheckMovementTutorial()
    {
        bool isMobile = Input.touchSupported;
        if (isMobile)
        {
            if (Input.GetMouseButton(0)) CompleteStep(0, "Отлично! Двигаемся дальше.");
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.W)) pressedKeys.Add(KeyCode.W);
            if (Input.GetKeyDown(KeyCode.A)) pressedKeys.Add(KeyCode.A);
            if (Input.GetKeyDown(KeyCode.S)) pressedKeys.Add(KeyCode.S);
            if (Input.GetKeyDown(KeyCode.D)) pressedKeys.Add(KeyCode.D);

            if (pressedKeys.Count >= 4)
            {
                movementHintTimer += Time.deltaTime;
                if (movementHintTimer > hintDisplayTime)
                {
                    CompleteStep(0, "Супер! Теперь найди врага.");
                }
            }
        }
    }

    private void CheckFirstAttackTutorial()
    {
        if (currentTutorialEnemy != null)
        {
            float dist = Vector2.Distance(player.position, currentTutorialEnemy.transform.position);
            if (dist < 2.5f && !isWaitingForAction)
            {
                isWaitingForAction = true;
                ShowHint("Враг рядом! Нажми ЛКМ, чтобы атаковать 4 раза.");
            }

            if (isWaitingForAction && playerCombat.IsAttacking)
            {
                darkOverlay.SetActive(false);
                tutorialText.gameObject.SetActive(false);
            }
            return;
        }

        if (!isTransitioning)
        {
            isTransitioning = true;
            isWaitingForAction = false;
            PauseAndShowSuccess("Отличная комбо-атака! Но следующий враг опаснее.");
            StartCoroutine(SpawnEnemyAfterDelay(2f, 2, EnemyController.EnemyType.ParryTutorial));
        }
    }

    private void CheckParryTutorial()
    {
        if (currentTutorialEnemy != null)
        {
            float dist = Vector2.Distance(player.position, currentTutorialEnemy.transform.position);

            if (dist < 2.5f && !hasShownParryHint)
            {
                hasShownParryHint = true;
                ShowHint("Враг замахиваетс! Нажми F, чтобы ПАРРИРОВАТЬ.");
            }

            // Проверяем: игрок нажал парри (JustParried) И враг в замахе
            if (hasShownParryHint && currentTutorialEnemy.IsWindingUp && !hasShownParrySuccess)
            {
                if (playerCombat.JustParried) // <-- ИЗМЕНЕНО: было IsParrying
                {
                    hasShownParrySuccess = true;
                    currentTutorialEnemy.TakeDamage(1f, Vector2.zero, 0f, false);
                    StartCoroutine(ShowParrySuccessAndResume());
                }
            }
            return;
        }

        if (!isTransitioning)
        {
            isTransitioning = true;
            PauseAndShowSuccess("Отлично! Теперь изучим уворот.");
            StartCoroutine(SpawnEnemyAfterDelay(2f, 3, EnemyController.EnemyType.DodgeTutorial));
        }
    }

    private IEnumerator ShowParrySuccessAndResume()
    {
        Time.timeScale = 0f;
        darkOverlay.SetActive(true);
        tutorialText.text = "Идеально! Враг оглушен. Добей его!";
        tutorialText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(2f);

        Time.timeScale = 1f;
        darkOverlay.SetActive(false);
        tutorialText.gameObject.SetActive(false);

        if (currentTutorialEnemy != null)
        {
            currentTutorialEnemy.SetFrozen(false);
        }
    }

    private void CheckDodgeTutorial()
    {
        if (currentTutorialEnemy != null)
        {
            float dist = Vector2.Distance(player.position, currentTutorialEnemy.transform.position);

            // Показываем подсказку об увороте (один раз)
            if (dist < 2.5f && !hasShownDodgeHint)
            {
                hasShownDodgeHint = true;
                ShowHint("Враг замахивается! Нажми Q, чтобы УКЛОНИТЬСЯ.");
            }

            // Проверяем успешный уворот (один раз)
            if (hasShownDodgeHint && currentTutorialEnemy.IsAttacking && !hasShownDodgeSuccess)
            {
                if (playerCombat.JustDodged)
                {
                    hasShownDodgeSuccess = true;
                    StartCoroutine(ShowDodgeSuccessAndResume());
                }
            }
            return; // Выходим, ждем смерти врага
        }

        // Враг умер (currentTutorialEnemy == null) — показываем таланты
        if (!isTransitioning)
        {
            isTransitioning = true;
            ShowTalentSelection();
        }
    }

    private IEnumerator ShowDodgeSuccessAndResume()
    {
        Time.timeScale = 0f;
        darkOverlay.SetActive(true);
        tutorialText.text = "Отличный уворот! Теперь добей врага.";
        tutorialText.gameObject.SetActive(true);

        // Делаем врага уязвимым после уворота
        if (currentTutorialEnemy != null)
        {
            currentTutorialEnemy.MakeVulnerableAfterDodge();
        }

        yield return new WaitForSecondsRealtime(2f);

        Time.timeScale = 1f;
        darkOverlay.SetActive(false);
        tutorialText.gameObject.SetActive(false);

        if (currentTutorialEnemy != null)
        {
            currentTutorialEnemy.SetFrozen(false);
        }
    }

    private void ShowTalentSelection()
    {
        isTutorialActive = false;
        Time.timeScale = 0f;
        darkOverlay.SetActive(true);
        tutorialText.gameObject.SetActive(false);

        if (talentSelectionUI != null)
        {
            talentSelectionUI.SetActive(true);
            Debug.Log("Панель талантов показана!");
        }
        else
        {
            Debug.LogError("talentSelectionUI не назначен в TutorialManager!");
        }

        Debug.Log("Обучение завершено! Выбор талантов.");
    }

    private void ShowHint(string text)
    {
        darkOverlay.SetActive(true);
        tutorialText.text = text;
        tutorialText.gameObject.SetActive(true);
    }

    private void PauseAndShowSuccess(string text)
    {
        Time.timeScale = 0f;
        darkOverlay.SetActive(true);
        tutorialText.text = text;
        tutorialText.gameObject.SetActive(true);
        StartCoroutine(ResumeGameAfterDelay(hintDisplayTime));
    }

    private IEnumerator ResumeGameAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ResumeGame();
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        darkOverlay.SetActive(false);
        tutorialText.gameObject.SetActive(false);
        if (currentTutorialEnemy != null) currentTutorialEnemy.SetFrozen(false);
    }

    private void CompleteStep(int step, string successText)
    {
        isTransitioning = true;
        tutorialStep = step + 1;
        pressedKeys.Clear();
        PauseAndShowSuccess(successText);
        StartCoroutine(SpawnEnemyAfterDelay(1.5f, 1, EnemyController.EnemyType.Passive));
    }

    private IEnumerator SpawnEnemyAfterDelay(float delay, int stepIndex, EnemyController.EnemyType type)
    {
        yield return new WaitForSecondsRealtime(delay);
        tutorialStep = stepIndex;
        isTransitioning = false;
        SpawnEnemyAt(stepIndex - 1, type);
    }

    private void SpawnEnemyAt(int index, EnemyController.EnemyType type)
    {
        if (enemyPrefab == null || index >= enemySpawnPoints.Count) return;

        Vector3 spawnPos = enemySpawnPoints[index].position;
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        currentTutorialEnemy = newEnemy.GetComponent<EnemyController>();

        if (currentTutorialEnemy != null && player != null)
        {
            currentTutorialEnemy.SetPlayer(player);
            currentTutorialEnemy.SetEnemyType(type);
            Debug.Log($"Враг {index + 1} заспавнен (тип: {type})!");
        }
    }

    private void EndTutorial()
    {
        tutorialStep = 4;
        isTutorialActive = false;
        ResumeGame();
        Debug.Log("ОБУЧЕНИЕ ЗАВЕРШЕНО!");
    }

    public void SelectTalent(int talentIndex)
    {
        Debug.Log($"Выбран талант {talentIndex + 1}!");

        if (talentSelectionUI != null)
        {
            talentSelectionUI.SetActive(false);
        }

        darkOverlay.SetActive(false);
        Time.timeScale = 1f;

        Debug.Log("Начинается настоящая игра!");
    }
}