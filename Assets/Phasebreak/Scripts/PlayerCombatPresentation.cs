using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;

        private Transform spine;
        private Transform chest;
        private Transform rightUpperArm;
        private Transform rightLowerArm;
        private Transform rightHand;
        private EquipmentVisualController equipment;
        private Transform flickerSlash;
        private Vector3 savedSlashScale;
        private Quaternion savedSlashRotation;
        private readonly System.Collections.Generic.Dictionary<Renderer, MaterialPropertyBlock> slashBlocks = new();
        private Transform leftUpperArm;
        private Transform leftLowerArm;
        private Transform head;
        private AbilityPresentationEvent activeAbility;
        private float abilityStartedAt;
        private float hitWeight;
        private Vector3 hitDirection;
        private bool abilityActive;
        private bool dead;
        private readonly System.Collections.Generic.Dictionary<Renderer, bool> hiddenRenderers = new();
        private readonly System.Collections.Generic.List<GameObject> afterimages = new();
        private Material afterimageMaterial;

        private void Awake()
        {
            animator ??= GetComponent<Animator>();
            combat ??= GetComponentInParent<PlayerCombat>();
            health ??= GetComponentInParent<PlayerHealth>();
            CacheBones();
        }

        private void CacheBones()
        {
            if (animator == null && combat != null) animator = combat.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
                return;
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                    animator.GetBoneTransform(HumanBodyBones.Chest);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            equipment = combat != null ? combat.GetComponentInChildren<EquipmentVisualController>() : GetComponent<EquipmentVisualController>();
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.AbilityStarted += HandleAbilityStarted;
                combat.AbilityCompleted += HandleAbilityCompleted;
                combat.FlickerDeparted += HandleFlickerDeparture;
                combat.FlickerArrived += HandleFlickerArrival;
            }
            if (health != null)
            {
                health.HitReceived += HandleHit;
                health.Died += HandleDeath;
                health.ResetPerformed += HandleReset;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AbilityStarted -= HandleAbilityStarted;
                combat.AbilityCompleted -= HandleAbilityCompleted;
                combat.FlickerDeparted -= HandleFlickerDeparture;
                combat.FlickerArrived -= HandleFlickerArrival;
            }
            ClearFlickerVisuals();
            if (health != null)
            {
                health.HitReceived -= HandleHit;
                health.Died -= HandleDeath;
                health.ResetPerformed -= HandleReset;
            }
        }

        private void HandleAbilityStarted(AbilityPresentationEvent value)
        {
            // The runtime visual owner can assign the humanoid avatar after Awake.
            if (spine == null) CacheBones();
            if (value.Type == AbilityExecutionType.FlickerStrike && flickerSlash == null)
            {
                flickerSlash = combat.SlashVisual;
                if (flickerSlash != null)
                {
                    savedSlashScale = flickerSlash.localScale;
                    savedSlashRotation = flickerSlash.localRotation;
                    foreach (Renderer renderer in flickerSlash.GetComponentsInChildren<Renderer>(true))
                    {
                        var saved = new MaterialPropertyBlock(); renderer.GetPropertyBlock(saved); slashBlocks[renderer] = saved;
                        var tint = new MaterialPropertyBlock();
                        tint.SetColor("_BaseColor", new Color(.38f, .7f, 1f));
                        tint.SetColor("_Color", new Color(.38f, .7f, 1f));
                        tint.SetColor("_EmissionColor", new Color(.16f, .35f, .6f));
                        renderer.SetPropertyBlock(tint);
                    }
                }
            }
            activeAbility = value;
            abilityStartedAt = Time.time;
            abilityActive = true;
        }

        private void HandleAbilityCompleted(AbilityPresentationEvent value)
        {
            if (value.Type == AbilityExecutionType.FlickerStrike) ClearFlickerVisuals();
            if (activeAbility.Index == value.Index)
                abilityActive = false;
        }

        private void RestoreRenderers()
        {
            foreach (var pair in hiddenRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
            hiddenRenderers.Clear();
        }

        private void ClearFlickerVisuals()
        {
            if (flickerSlash != null)
            {
                flickerSlash.localScale = savedSlashScale;
                flickerSlash.localRotation = savedSlashRotation;
                foreach (var pair in slashBlocks) if (pair.Key != null) pair.Key.SetPropertyBlock(pair.Value);
            }
            slashBlocks.Clear();
            flickerSlash = null;
            RestoreRenderers();
            StopAllCoroutines();
            foreach (GameObject ghost in afterimages)
                if (ghost != null)
                {
                    foreach (MeshFilter mesh in ghost.GetComponentsInChildren<MeshFilter>()) Destroy(mesh.sharedMesh);
                    Destroy(ghost);
                }
            afterimages.Clear();
        }

        private void OnDestroy() { if (afterimageMaterial != null) Destroy(afterimageMaterial); }

        private void HandleFlickerDeparture(AbilityPresentationEvent value)
        {
            RestoreRenderers();
            if (afterimageMaterial == null)
            {
                afterimageMaterial = new Material(Shader.Find("Sprites/Default"));
                afterimageMaterial.color = new Color(.32f, .65f, 1f, .22f);
            }
            GameObject ghost = new("Flicker departure silhouette");
            afterimages.Add(ghost);
            foreach (Renderer renderer in combat.GetComponentsInChildren<Renderer>())
            {
                if (renderer is not SkinnedMeshRenderer && renderer is not MeshRenderer) continue;
                hiddenRenderers[renderer] = renderer.enabled;
                if (renderer.enabled && renderer is SkinnedMeshRenderer skin)
                {
                    Mesh mesh = new();
                    skin.BakeMesh(mesh);
                    GameObject part = new("Afterimage");
                    part.transform.SetParent(ghost.transform);
                    part.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                    part.transform.localScale = skin.transform.lossyScale;
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var copy = part.AddComponent<MeshRenderer>();
                    var materials = new Material[mesh.subMeshCount];
                    System.Array.Fill(materials, afterimageMaterial);
                    copy.sharedMaterials = materials;
                    copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                renderer.enabled = false;
            }
            StartCoroutine(FadeAfterimage(ghost));
        }

        private System.Collections.IEnumerator FadeAfterimage(GameObject ghost)
        {
            float start = Time.time;
            var block = new MaterialPropertyBlock();
            while (ghost != null && Time.time - start < .18f)
            {
                block.SetColor("_Color", new Color(.32f, .65f, 1f, .22f * (1f - (Time.time - start) / .18f)));
                foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
                yield return null;
            }
            if (ghost != null)
            {
                foreach (MeshFilter mesh in ghost.GetComponentsInChildren<MeshFilter>()) Destroy(mesh.sharedMesh);
                afterimages.Remove(ghost);
                Destroy(ghost);
            }
        }

        private void HandleFlickerArrival(AbilityPresentationEvent value)
        {
            RestoreRenderers();
            HandleAbilityStarted(value);
        }

        private void HandleHit(Vector3 direction, int damage)
        {
            hitDirection = direction;
            hitWeight = 1f;
        }

        private void HandleDeath() => dead = true;

        private void HandleReset()
        {
            dead = false;
            hitWeight = 0f;
            abilityActive = false;
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isHuman)
                return;

            if (dead)
            {
                AddRotation(spine, 0f, 0f, 62f);
                AddRotation(chest, 22f, 0f, 18f);
                AddRotation(head, -18f, 0f, -22f);
                AddRotation(rightUpperArm, 12f, -20f, 34f);
                AddRotation(leftUpperArm, -8f, 18f, -28f);
                return;
            }

            if (abilityActive)
                ApplyAbilityPose();

            if (hitWeight > 0.001f)
            {
                float side = Vector3.Dot(transform.right, hitDirection);
                AddRotation(spine, -12f * hitWeight, 0f, -side * 18f * hitWeight);
                AddRotation(chest, -18f * hitWeight, side * 12f * hitWeight, 0f);
                hitWeight = Mathf.MoveTowards(hitWeight, 0f, Time.unscaledDeltaTime * 6.5f);
            }
        }

        private void ApplyAbilityPose()
        {
            float duration = Mathf.Max(0.05f, activeAbility.Duration);
            float t = Mathf.Clamp01((Time.time - abilityStartedAt) / duration);
            float weight = Mathf.Sin(t * Mathf.PI);
            switch (activeAbility.Type)
            {
                case AbilityExecutionType.FlickerStrike:
                    ApplyFlickerPose(t, weight);
                    break;
                case AbilityExecutionType.Quickstep:
                    AddRotation(spine, 18f * weight, 0f, 0f);
                    AddRotation(chest, 12f * weight, 0f, 0f);
                    AddRotation(rightUpperArm, -14f * weight, 0f, 10f * weight);
                    AddRotation(leftUpperArm, -14f * weight, 0f, -10f * weight);
                    break;
                case AbilityExecutionType.PhaseDash:
                    AddRotation(spine, 26f * weight, 0f, 0f);
                    AddRotation(chest, 18f * weight, 0f, 0f);
                    AddRotation(rightUpperArm, -24f * weight, 0f, 16f * weight);
                    AddRotation(leftUpperArm, -24f * weight, 0f, -16f * weight);
                    break;
                case AbilityExecutionType.Charge:
                    AddRotation(spine, 34f * weight, 0f, -8f * weight);
                    AddRotation(chest, 24f * weight, 0f, -12f * weight);
                    AddRotation(rightUpperArm, -42f * weight, 12f * weight, 42f * weight);
                    AddRotation(leftUpperArm, 18f * weight, -8f * weight, -28f * weight);
                    break;
                case AbilityExecutionType.PhaseLunge:
                    AddRotation(spine, 20f * weight, 18f * weight, 0f);
                    AddRotation(chest, 12f * weight, 34f * weight, 0f);
                    AddRotation(rightUpperArm, -72f * weight, 18f * weight, 58f * weight);
                    AddRotation(rightLowerArm, 0f, 0f, -32f * weight);
                    break;
                case AbilityExecutionType.Guard:
                    AddRotation(spine, -9f * weight, 0f, 0f);
                    AddRotation(leftUpperArm, -65f * weight, 0f, -38f * weight);
                    AddRotation(rightUpperArm, -55f * weight, 0f, 38f * weight);
                    break;
                case AbilityExecutionType.Area:
                    AddRotation(spine, -20f * weight, 0f, 0f);
                    AddRotation(leftUpperArm, -100f * weight, 0f, -65f * weight);
                    AddRotation(rightUpperArm, -100f * weight, 0f, 65f * weight);
                    break;
                default:
                    ApplyMeleePose(activeAbility.Index, t, weight);
                    break;
            }
        }

        private void ApplyFlickerPose(float t, float weight)
        {
            if (rightUpperArm == null || rightLowerArm == null || rightHand == null) return;
            float side = activeAbility.Strike % 2 == 0 ? 1f : -1f;
            float cut = Mathf.Lerp(1f, -1f, Mathf.SmoothStep(0f, 1f, t / .8f));
            Transform root = combat.transform;
            AddRotation(spine, (activeAbility.Finisher ? 20f : 8f) * weight, -side * cut * 20f * weight, 0f);
            AddRotation(chest, 8f * weight, -side * cut * 35f * weight, 0f);
            Vector3 elbow = rightUpperArm.position + root.forward * .28f + root.right * .22f + Vector3.up * .05f;
            rightUpperArm.rotation = Quaternion.FromToRotation(rightLowerArm.position - rightUpperArm.position,
                elbow - rightUpperArm.position) * rightUpperArm.rotation;
            Vector3 hand = rightUpperArm.position + root.forward * .48f + root.right * (side * cut * .3f) +
                Vector3.up * (activeAbility.Finisher ? cut * .35f : .08f);
            rightLowerArm.rotation = Quaternion.FromToRotation(rightHand.position - rightLowerArm.position,
                hand - rightLowerArm.position) * rightLowerArm.rotation;
            Transform attachment = equipment != null ? equipment.RightHandAttachment : null;
            if (attachment != null)
            {
                Vector3 bladeDirection = root.forward + root.right * (side * cut * .8f) +
                    Vector3.up * (activeAbility.Finisher ? cut * .7f : side * cut * .2f);
                rightHand.rotation = Quaternion.FromToRotation(attachment.up, bladeDirection) * rightHand.rotation;
            }
            if (flickerSlash != null)
            {
                float size = activeAbility.Finisher ? 1.1f : .85f;
                flickerSlash.localScale = new Vector3(size, .22f, size);
                flickerSlash.localRotation = Quaternion.Euler(0f, -side * cut * 30f, activeAbility.Finisher ? -20f : side * 12f);
            }
        }

        private void ApplyMeleePose(int index, float t, float weight)
        {
            float swing = Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, t));
            if (index == 1)
            {
                AddRotation(spine, -12f * weight, 0f, 0f);
                AddRotation(chest, -18f * weight, 0f, 0f);
                AddRotation(rightUpperArm, -115f * weight, 0f, 32f * weight);
                AddRotation(leftUpperArm, -92f * weight, 0f, -28f * weight);
                AddRotation(rightLowerArm, 0f, 0f, -48f * weight);
                AddRotation(leftLowerArm, 0f, 0f, 42f * weight);
                return;
            }
            AddRotation(spine, 0f, swing * 18f * weight, swing * 8f * weight);
            AddRotation(chest, 0f, swing * 38f * weight, swing * 14f * weight);
            AddRotation(rightUpperArm, -38f * weight, swing * 62f * weight, 72f * weight);
            AddRotation(rightLowerArm, 0f, 0f, -48f * weight);
        }

        private static void AddRotation(Transform bone, float x, float y, float z)
        {
            if (bone != null)
                bone.localRotation *= Quaternion.Euler(x, y, z);
        }
    }
}
