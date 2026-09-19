using UnityEngine;

namespace Phasebreak.Gameplay
{
    // One focus authority for gameplay input consumers. The closing key remains consumed
    // for the rest of its frame, regardless of MonoBehaviour update order.
    public static class GameplayInputFocus
    {
        private static bool chatFocused;
        private static bool menuFocused;
        private static int blockedThroughFrame = -1;

        public static bool ChatFocused => chatFocused;
        public static bool GameplayInputBlocked => chatFocused || menuFocused || Time.frameCount <= blockedThroughFrame;

        internal static void SetChatFocused(bool focused)
        {
            chatFocused = focused;
            blockedThroughFrame = Time.frameCount;
        }

        internal static void SetMenuFocused(bool focused)
        {
            menuFocused = focused;
            blockedThroughFrame = Time.frameCount;
        }

        internal static void ConsumeFrame() => blockedThroughFrame = Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            chatFocused = false;
            menuFocused = false;
            blockedThroughFrame = -1;
        }
    }
}
