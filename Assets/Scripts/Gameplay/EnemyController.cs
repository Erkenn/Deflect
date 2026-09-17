using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType
    {
        Passive,
        ParryTutorial,
        DodgeTutorial
    }

    [Header("Настройки врага")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float maxHealth = 4f;
    [SerializeField] private Transform player;
    [SerializeField] private EnemyType enemyType = EnemyType.Passive;

    [Header("Настройки атаки")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float windupTime = 1.2f;
    [SerializeField] private float attackCooldown = 3f;

    [Header("Настройки стана")]
    [SerializeField] private float stunDuration = 1.5f;

    private float currentHealth;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Color originalColor;

    private float stunTimer = 0f;
    private float attackCooldownTimer = 0f;
    private bool isWindingUp = false;
    private bool isAttacking = false;

    // НОВЫЙ ФЛАГ: может ли эта атака быть парирована
    public bool CanBeParried { get; private set; } = true;

    public bool IsStunned { get; private set; } = false;
    public bool IsFrozen { get; set; } = false;
    public bool IsAttacking { get; private set; } = false;
    public bool IsWindingUp { get; private set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (enemyType == EnemyType.DodgeTutorial)
        {
            CanBeParried = false;
        }
    }

    void Update()
    {
        if (player == null) return;

        if (IsFrozen || stunTimer > 0)
        {
            if (stunTimer > 0) stunTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (enemyType == EnemyType.Passive)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
            return;
        }

        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange && attackCooldownTimer <= 0 && !isWindingUp && !isAttacking)
        {
            StartCoroutine(AttackWindup());
        }
        else if (!isWindingUp && !isAttacking)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
    }

    private IEnumerator AttackWindup()
    {
        isWindingUp = true;
        IsWindingUp = true;
        rb.linearVelocity = Vector2.zero;

        if (sr != null) sr.color = Color.green;
        Debug.Log(" Враг замахивается!");

        float actualWindupTime = (enemyType == EnemyType.DodgeTutorial) ? 0.3f : windupTime;
        yield return new WaitForSeconds(actualWindupTime);

        isWindingUp = false;
        IsWindingUp = false;
        isAttacking = true;
        IsAttacking = true;

        if (sr != null) sr.color = Color.red;
        Debug.Log("🔴 Враг атакует!");

        if (enemyType == EnemyType.DodgeTutorial)
        {
            yield return new WaitForSeconds(0.3f);

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.5f)
            {
                PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
                if (playerCombat != null && !playerCombat.JustDodged)
                {
                    Vector2 knockbackDir = (player.position - transform.position).normalized;
                    player.GetComponent<Rigidbody2D>().AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
                    Debug.Log("Игрок не увернулся - оттолкнут!");
                }
            }
        }

        attackCooldownTimer = attackCooldown;
        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
        IsAttacking = false;
        if (sr != null) sr.color = originalColor;
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce, bool isFinisher)
    {
        currentHealth -= damage;
        StartCoroutine(FlashWhite());

        if (isFinisher)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            Debug.Log("ФИНИШЕР!");
        }
        else
        {
            if (CanBeParried)
            {
                IsStunned = true;
                rb.linearVelocity = Vector2.zero;
                stunTimer = stunDuration;
                Debug.Log("Враг оглушен!");
            }
            else
            {
                Debug.Log("️Эта атака не может быть парирована!");
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Враг уничтожен!");
        Destroy(gameObject);
    }

    private IEnumerator FlashWhite()
    {
        if (sr != null) sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = originalColor;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }

    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;
        if (frozen)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetEnemyType(EnemyType type)
    {
        enemyType = type;
    }

    public void MakeVulnerableAfterDodge()
    {
        CanBeParried = true;
        currentHealth = 1f;
        Debug.Log("Враг стал уязвимым после уворота!");
    }
}