using UnityEngine;

namespace Phasebreak.Gameplay
{
    // Authored on a weapon visual prefab. The same mesh can have a sound grip and sheath pose.
    [DisallowMultipleComponent]
    public sealed class EquipmentWeaponPlacement : MonoBehaviour
    {
        [SerializeField] private Vector3 handPosition;
        [SerializeField] private Vector3 handEuler = new(180f, 0f, 0f);
        [SerializeField] private Vector3 backPosition;
        [SerializeField] private Vector3 backEuler;

        public void Configure(Vector3 drawnPosition, Vector3 drawnEuler,
            Vector3 sheathedPosition, Vector3 sheathedEuler)
        {
            handPosition = drawnPosition;
            handEuler = drawnEuler;
            backPosition = sheathedPosition;
            backEuler = sheathedEuler;
        }

        public void Apply(bool drawn)
        {
            transform.localPosition = drawn ? handPosition : backPosition;
            transform.localRotation = Quaternion.Euler(drawn ? handEuler : backEuler);
        }
    }
}
