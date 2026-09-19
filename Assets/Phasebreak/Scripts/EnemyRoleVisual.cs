using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyRoleVisual : MonoBehaviour
    {
        private MeleeEnemy enemy;
        private Transform modelRoot;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private float age;
        private Material ownedMaterial;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Quaternion leftArmRest;
        private Quaternion rightArmRest;
        private Quaternion leftLegRest;
        private Quaternion rightLegRest;

        public bool UsingLocalModel { get; private set; }

        public void Configure(EnemyRole role, EnemyRank rank, EnemyRoleDefinition definition)
        {
            enemy = GetComponent<MeleeEnemy>();
            EnemyVisualAnimator oldAnimation = GetComponent<EnemyVisualAnimator>();
            if (oldAnimation != null)
            {
                if (oldAnimation.Animator != null) oldAnimation.Animator.gameObject.SetActive(false);
                oldAnimation.enabled = false;
            }
            string species = definition != null ? definition.localModelName :
                role == EnemyRole.Brute ? "Puglin" : "Imp";
            GameObject source = Resources.Load<GameObject>("LocalMonsters/" + species);
            UsingLocalModel = source != null;
            GameObject model = source != null ? Instantiate(source, transform) : CreateFallback(role);
            model.name = species + " Visual";
            modelRoot = model.transform;
            modelRoot.SetParent(transform, false);
            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.identity;
            if (source != null)
            {
                float height = role == EnemyRole.Brute ? 2.65f : 1.7f;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    float factor = height / Mathf.Max(.01f, bounds.size.y);
                    modelRoot.localScale *= factor;
                    Bounds scaledBounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) scaledBounds.Encapsulate(renderers[i].bounds);
                    modelRoot.position += Vector3.up * (transform.position.y - scaledBounds.min.y);
                }
            }
            else
            {
                modelRoot.localScale = role == EnemyRole.Brute ? new Vector3(1.5f, 2.1f, 1.25f) :
                    new Vector3(.7f, 1.2f, .7f);
                modelRoot.localPosition = Vector3.up * (role == EnemyRole.Brute ? 1.05f : .6f);
            }
            EnemyRoleDefinition.RankTuning tuning = definition != null ? definition.Tuning(rank) : default;
            float rankScale = tuning.scaleMultiplier > 0f ? tuning.scaleMultiplier : 1f;
            modelRoot.localScale *= rankScale;
            int colorVariant = tuning.colorVariant > 0 ? tuning.colorVariant : (int)rank + 1;
            Texture2D texture = Resources.Load<Texture2D>($"LocalMonsters/T_{species}_BaseColor_{colorVariant}");
            Texture2D normal = Resources.Load<Texture2D>($"LocalMonsters/T_{species}_Normal");
            Color tint = role == EnemyRole.Brute ? new Color(.82f, .65f, .49f) : new Color(1f, .52f, .3f);
            if (rank == EnemyRank.Veteran) tint = Color.Lerp(tint, new Color(.45f, .78f, 1f), .35f);
            if (rank == EnemyRank.Elite) tint = Color.Lerp(tint, new Color(1f, .57f, .18f), .35f);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { color = tint };
            ownedMaterial = material;
            if (texture != null) material.SetTexture("_BaseMap", texture);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            restPosition = modelRoot.localPosition;
            restRotation = modelRoot.localRotation;
            foreach (Transform bone in model.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "upperarm_l") leftArm = bone;
                else if (bone.name == "upperarm_r") rightArm = bone;
                else if (bone.name == "thigh_l") leftLeg = bone;
                else if (bone.name == "thigh_r") rightLeg = bone;
            }
            if (leftArm != null) leftArmRest = leftArm.localRotation;
            if (rightArm != null) rightArmRest = rightArm.localRotation;
            if (leftLeg != null) leftLegRest = leftLeg.localRotation;
            if (rightLeg != null) rightLegRest = rightLeg.localRotation;
        }

        private void OnDestroy()
        {
            if (ownedMaterial != null) Destroy(ownedMaterial);
        }

        private GameObject CreateFallback(EnemyRole role)
        {
            GameObject shape = GameObject.CreatePrimitive(role == EnemyRole.Brute ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            Destroy(shape.GetComponent<Collider>());
            return shape;
        }

        private void LateUpdate()
        {
            if (modelRoot == null || enemy == null || enemy.IsDead) return;
            age += Time.deltaTime;
            float bob = enemy.IsMoving ? Mathf.Sin(age * 12f) * .055f : Mathf.Sin(age * 2f) * .02f;
            float windup = enemy.IsCasting ? enemy.CastProgress : 0f;
            modelRoot.localPosition = restPosition + Vector3.up * bob + Vector3.back * windup * .22f;
            modelRoot.localRotation = restRotation * Quaternion.Euler(windup * -15f,
                enemy.IsAttackActive ? 15f : 0f, enemy.IsStaggered ? 12f : 0f);
            float stride = enemy.IsMoving ? Mathf.Sin(age * 12f) * 18f : 0f;
            float strike = enemy.IsAttackActive ? 45f : windup * -30f;
            if (leftArm != null) leftArm.localRotation = leftArmRest * Quaternion.Euler(stride + strike, 0f, 68f);
            if (rightArm != null) rightArm.localRotation = rightArmRest * Quaternion.Euler(-stride - strike, 0f, -68f);
            if (leftLeg != null) leftLeg.localRotation = leftLegRest * Quaternion.Euler(-stride, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = rightLegRest * Quaternion.Euler(stride, 0f, 0f);
        }
    }
}
