using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [RequireComponent(typeof(MeleeEnemy))]
    [DisallowMultipleComponent]
    public sealed class RiftWardenBoss : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform frontalTelegraph;
        [SerializeField] private Transform groundTelegraph;
        [SerializeField, Min(0.5f)] private float specialCooldown = 4.2f;
        [SerializeField, Min(0.2f)] private float telegraphDuration = 1.15f;
        [SerializeField, Min(1f)] private float frontalRange = 5.5f;
        [SerializeField, Min(0.5f)] private float frontalHalfWidth = 1.35f;
        [SerializeField, Min(0.5f)] private float groundRadius = 2.35f;
        [SerializeField, Min(1)] private int specialDamage = 2;

        private MeleeEnemy enemy;
        private PlayerHealth playerHealth;
        private float nextSpecialAt;
        private bool useGroundAttack;
        private Coroutine attackRoutine;

        public string CurrentMechanic { get; private set; } = string.Empty;

        public void Configure(Transform player, Transform frontal, Transform ground)
        {
            target = player;
            frontalTelegraph = frontal;
            groundTelegraph = ground;
        }

        private void Awake()
        {
            enemy = GetComponent<MeleeEnemy>();
            if (target == null)
            {
                PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
                if (player != null)
                    target = player.transform;
            }
            playerHealth = target != null ? target.GetComponent<PlayerHealth>() : null;
            SetTelegraphs(false);
        }

        private void OnEnable()
        {
            nextSpecialAt = Time.time + 2.5f;
            SetTelegraphs(false);
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
                StopCoroutine(attackRoutine);
            attackRoutine = null;
            CurrentMechanic = string.Empty;
            SetTelegraphs(false);
        }

        private void Update()
        {
            if (enemy == null || !enemy.IsAlive || target == null || attackRoutine != null ||
                Time.time < nextSpecialAt)
                return;
            if ((target.position - transform.position).sqrMagnitude > 12f * 12f)
                return;

            attackRoutine = StartCoroutine(useGroundAttack ? GroundRupture() : FrontalSlam());
            useGroundAttack = !useGroundAttack;
        }

        private IEnumerator FrontalSlam()
        {
            CurrentMechanic = "FRONTAL SLAM — MOVE ASIDE";
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            if (frontalTelegraph != null)
            {
                frontalTelegraph.gameObject.SetActive(true);
                frontalTelegraph.localScale = new Vector3(frontalHalfWidth * 2f, 0.035f, frontalRange);
                frontalTelegraph.localPosition = new Vector3(0f, -0.94f, frontalRange * 0.5f);
            }
            yield return GrowTelegraph(frontalTelegraph);

            Vector3 localPlayer = transform.InverseTransformPoint(target.position);
            if (Mathf.Abs(localPlayer.x) <= frontalHalfWidth && localPlayer.z >= 0f &&
                localPlayer.z <= frontalRange)
                DamagePlayer((target.position - transform.position).normalized, 8f);
            FinishSpecial(frontalTelegraph);
        }

        private IEnumerator GroundRupture()
        {
            CurrentMechanic = "RIFT RUPTURE — LEAVE THE CIRCLE";
            Vector3 impactPoint = target.position;
            impactPoint.y = transform.position.y - 0.94f;
            if (groundTelegraph != null)
            {
                groundTelegraph.position = impactPoint;
                groundTelegraph.localScale = new Vector3(groundRadius * 2f, 0.035f, groundRadius * 2f);
                groundTelegraph.gameObject.SetActive(true);
            }
            yield return GrowTelegraph(groundTelegraph);

            Vector3 flatDelta = target.position - impactPoint;
            flatDelta.y = 0f;
            if (flatDelta.magnitude <= groundRadius)
                DamagePlayer(flatDelta.sqrMagnitude > 0.01f ? flatDelta.normalized : transform.forward, 7f);
            FinishSpecial(groundTelegraph);
        }

        private IEnumerator GrowTelegraph(Transform visual)
        {
            Vector3 finalScale = visual != null ? visual.localScale : Vector3.one;
            float elapsed = 0f;
            while (elapsed < telegraphDuration && enemy != null && enemy.IsAlive)
            {
                elapsed += Time.deltaTime;
                if (visual != null)
                {
                    float t = Mathf.Clamp01(elapsed / telegraphDuration);
                    visual.localScale = new Vector3(finalScale.x, finalScale.y,
                        finalScale.z) * Mathf.Lerp(0.18f, 1f, t);
                }
                yield return null;
            }
        }

        private void DamagePlayer(Vector3 direction, float knockback)
        {
            if (playerHealth != null && playerHealth.IsAlive)
                playerHealth.TakeHit(specialDamage, direction, knockback);
        }

        private void FinishSpecial(Transform visual)
        {
            if (visual != null)
                visual.gameObject.SetActive(false);
            CurrentMechanic = string.Empty;
            nextSpecialAt = Time.time + specialCooldown;
            attackRoutine = null;
        }

        private void SetTelegraphs(bool visible)
        {
            if (frontalTelegraph != null)
                frontalTelegraph.gameObject.SetActive(visible);
            if (groundTelegraph != null)
                groundTelegraph.gameObject.SetActive(visible);
        }
    }
}
