using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ClassResourcePipsUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image[] pips;
        [SerializeField] private Color activeColor = new(.28f, .85f, 1f, 1f);
        [SerializeField] private Color inactiveColor = new(.08f, .11f, .18f, .9f);

        public void Configure(UnityEngine.UI.Image[] pipImages, Color active, Color inactive)
        {
            pips = pipImages;
            activeColor = active;
            inactiveColor = inactive;
        }

        public void SetPalette(Color active, Color inactive)
        {
            activeColor = active;
            inactiveColor = inactive;
        }

        public void SetPips(int current, int maximum)
        {
            if (pips == null)
                return;
            int visibleCount = Mathf.Clamp(maximum, 0, pips.Length);
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null)
                    continue;
                pips[i].gameObject.SetActive(i < visibleCount);
                pips[i].color = i < current ? activeColor : inactiveColor;
            }
        }
    }
}
