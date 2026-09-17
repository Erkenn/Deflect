using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Настройки боя")]
    [SerializeField] private float parryDuration = 0.4f;
    [SerializeField] private float dodgeSpeed = 15f;
    [SerializeField] private float dodgeDuration = 0.2f;

    [Header("Настройки Атаки и Комбо")]
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float comboResetTime = 2f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Кулдауны (только для Парри и Рывка)")]
    [SerializeField] private float parryCooldown = 1.5f;
    [SerializeField] private float dodgeCooldown = 1.0f;
    [SerializeField] private float successfulParryCooldownReduction = 0.5f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private PlayerController playerController;
    private Color originalColor;

    public bool IsAttacking { get; private set; } = false;
    public bool IsParrying { get; private set; } = false;
    public bool IsDodging { get; private set; } = false;
    public bool JustParried { get; private set; } = false;
    private float parryTimer = 0f;
    public bool JustDodged { get; private set; } = false;
    private float dodgeTimer = 0f;

    private int comboCount = 0;
    private float comboTimer = 0f;

    private float parryCooldownTimer = 0f;
    private float dodgeCooldownTimer = 0f;

    public bool allowKeyboardInput = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();

        if (sr != null) originalColor = sr.color;
    }

    void Update()
    {
        UpdateCooldownTimers();
        UpdateComboTimer();

        if (IsDodging) return;

        if (JustParried)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
            {
                JustParried = false;
            }
        }

        if (JustDodged)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                JustDodged = false;
            }
        }

        if (allowKeyboardInput)
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                if (!IsAttacking && !IsParrying)
                {
                    StartAttack();
                }
            }

            if (Input.GetKeyDown(KeyCode.F) && parryCooldownTimer <= 0)
            {
                if (!IsAttacking) StartCoroutine(ParryRoutine());
            }

            if ((Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftShift)) && dodgeCooldownTimer <= 0)
            {
                StartCoroutine(DodgeRoutine());
            }
        }
    }

    private void UpdateCooldownTimers()
    {
        if (parryCooldownTimer > 0) parryCooldownTimer -= Time.deltaTime;
        if (dodgeCooldownTimer > 0) dodgeCooldownTimer -= Time.deltaTime;
    }

    private void UpdateComboTimer()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                comboCount = 0;
                Debug.Log("Комбо сброшено");
            }
        }
    }

    private void StartAttack()
    {
        comboCount++;
        comboTimer = comboResetTime;
        if (comboCount > 4) comboCount = 1;
        StartCoroutine(AttackRoutine(comboCount));
    }

    private IEnumerator AttackRoutine(int currentCombo)
    {
        IsAttacking = true; // <-- Большая буква I
        playerController.SpeedMultiplier = 0.4f;
        HitEnemies(currentCombo);

        switch (currentCombo)
        {
            case 1: sr.color = Color.white; Debug.Log("️ Удар 1!"); break;
            case 2: sr.color = Color.yellow; Debug.Log("Удар 2!"); break;
            case 3: sr.color = Color.magenta; Debug.Log("Удар 3!"); break;
            case 4:
                sr.color = Color.red;
                Debug.Log("ФИНИШЕР!");
                break;
        }

        yield return new WaitForSeconds(attackDuration);

        IsAttacking = false;
        playerController.SpeedMultiplier = 1f;
        if (sr != null) sr.color = originalColor;
    }

    private void HitEnemies(int comboCount)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    float force = knockbackForce;
                    bool isFinisher = (comboCount == 4);

                    if (!enemy.CanBeParried && !isFinisher)
                    {
                        Debug.Log("Обычные удары не работают на этого врага! Нужен финишер!");
                        continue;
                    }

                    enemy.TakeDamage(1f, knockbackDir, force, isFinisher);
                }
            }
        }
    }

    private IEnumerator ParryRoutine()
    {
        IsParrying = true;
        JustParried = true;
        parryTimer = 1f;
        parryCooldownTimer = parryCooldown;

        if (sr != null) sr.color = Color.blue;
        Debug.Log($"Парирование! Кулдаун: {parryCooldown}с");

        yield return new WaitForSeconds(parryDuration);

        IsParrying = false;
        if (sr != null) sr.color = originalColor;
    }

    private IEnumerator DodgeRoutine()
    {
        IsDodging = true; 
        JustDodged = true;
        dodgeTimer = 1f;   
        dodgeCooldownTimer = dodgeCooldown;
        playerController.IsDodging = true;

        if (sr != null) sr.color = Color.green;
        Vector2 dodgeDirection = playerController.GetLastMovementDirection();
        rb.linearVelocity = dodgeDirection * dodgeSpeed;

        yield return new WaitForSeconds(dodgeDuration);

        IsDodging = false;
        playerController.IsDodging = false;
        playerController.SpeedMultiplier = 1f;
        if (sr != null) sr.color = originalColor;
        rb.linearVelocity = Vector2.zero;
    }

    public void OnSuccessfulParry()
    {
        Debug.Log("Успешное парирование!");
        parryCooldownTimer = Mathf.Max(0, parryCooldownTimer - successfulParryCooldownReduction);
        dodgeCooldownTimer = Mathf.Max(0, dodgeCooldownTimer - successfulParryCooldownReduction);
    }

    public void MobileAttack()
    {
        if (!IsAttacking && !IsParrying && !IsDodging) StartAttack();
    }

    public void MobileParry()
    {
        if (!IsAttacking && parryCooldownTimer <= 0) StartCoroutine(ParryRoutine());
    }

    public void MobileDodge()
    {
        if (!IsDodging && dodgeCooldownTimer <= 0) StartCoroutine(DodgeRoutine());
    }
}