using UnityEngine;
using Dashlash.World.Encounters;

namespace Dashlash.World
{
    public class WorldSetup : MonoBehaviour
    {
        private void Start()
        {
            ExclusionValidator.Initialize();
        }
    }
}