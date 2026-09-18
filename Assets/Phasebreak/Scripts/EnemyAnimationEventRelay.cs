using UnityEngine;

namespace Phasebreak.Gameplay
{
    /// <summary>
    /// Receives optional animation events on an enemy's visual child. Audio and
    /// VFX hooks can be connected here later without coupling them to enemy AI.
    /// </summary>
    public sealed class EnemyAnimationEventRelay : MonoBehaviour
    {
        public void OnFootstep()
        {
            // Intentionally silent until enemy footstep audio is authored.
        }
    }
}
