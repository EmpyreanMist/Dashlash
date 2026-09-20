using UnityEngine;

namespace Phasebreak.Gameplay
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private static Material emberMaterial;
        private static Material riftMaterial;
        private MeleeEnemy owner;
        private Vector3 direction;
        private int damage;
        private float lifetime;
        private bool rift;
        private const float Speed = 12f;
        private const float Radius = .22f;

        public static EnemyProjectile Spawn(Vector3 origin, Vector3 heading, MeleeEnemy source, int power, bool isRift = false)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = isRift ? "Rift Lance" : "Imp Ember Bolt";
            visual.transform.position = origin;
            visual.transform.localScale = isRift ? new Vector3(.38f, .38f, .8f) : Vector3.one * (Radius * 2f);
            Destroy(visual.GetComponent<Collider>());
            if (emberMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                emberMaterial = new Material(shader);
                emberMaterial.color = new Color(1f, .37f, .08f, 1f);
                emberMaterial.enableInstancing = true;
            }
            if (riftMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                riftMaterial = new Material(shader) { color = new Color(.58f, .29f, .85f, 1f), enableInstancing = true };
            }
            visual.GetComponent<Renderer>().sharedMaterial = isRift ? riftMaterial : emberMaterial;
            EnemyProjectile projectile = visual.AddComponent<EnemyProjectile>();
            projectile.owner = source;
            projectile.direction = heading.normalized;
            projectile.damage = power;
            projectile.rift = isRift;
            if (isRift) visual.transform.rotation = Quaternion.LookRotation(heading);
            return projectile;
        }

        private void Update()
        {
            float step = (rift ? 8f : Speed) * Time.deltaTime;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, Radius, direction, step,
                ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || (owner != null && hit.collider.transform.IsChildOf(owner.transform))) continue;
                PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (player != null) player.TakeHit(damage, direction);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * step;
            lifetime += Time.deltaTime;
            if (lifetime >= 5f) Destroy(gameObject);
        }
    }
}
