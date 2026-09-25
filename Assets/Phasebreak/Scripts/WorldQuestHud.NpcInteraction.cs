using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public sealed partial class WorldQuestHud
    {
        private RectTransform npcWindow;
        private TextMeshProUGUI npcName, npcRole, npcStatus, npcTitle, npcDescription, npcObjective, npcRewards;
        private UnityEngine.UI.Button npcAccept, npcComplete, npcDecline;
        private TextMeshProUGUI npcDeclineLabel;
        private QuestNpc interactionNpc;
        private string presentedQuestId;
        private bool npcOpen;

        private void BuildNpcWindow()
        {
            npcWindow = Window("NPC Interaction", string.Empty, Vector2.zero, Vector2.one);
            Pin(npcWindow, new Vector2(.28f, .5f), Vector2.zero, new Vector2(580f, 640f));
            npcWindow.pivot = new Vector2(.5f, .5f);
            PhasebreakUiTheme.StyleSurface(npcWindow.GetComponent<UnityEngine.UI.Image>(), Back);
            npcName = npcWindow.Find("Title").GetComponent<TextMeshProUGUI>();
            npcName.fontSize = 24f;
            npcRole = Label("NPC Role", npcWindow, "", 17f, TextAlignmentOptions.Left,
                new(.05f, .81f), new(.94f, .86f), Muted);
            npcStatus = Label("NPC Quest Status", npcWindow, "", 16f, TextAlignmentOptions.Left,
                new(.05f, .75f), new(.94f, .8f), Cyan);
            npcTitle = Label("NPC Quest Title", npcWindow, "", 25f, TextAlignmentOptions.TopLeft,
                new(.05f, .65f), new(.94f, .74f), Text);
            npcDescription = Label("NPC Description", npcWindow, "", 20f, TextAlignmentOptions.TopLeft,
                new(.05f, .41f), new(.94f, .64f), Text);
            npcObjective = Label("NPC Objective", npcWindow, "", 18f, TextAlignmentOptions.TopLeft,
                new(.05f, .26f), new(.94f, .40f), Muted);
            npcRewards = Label("NPC Rewards", npcWindow, "", 19f, TextAlignmentOptions.TopLeft,
                new(.05f, .12f), new(.94f, .25f), Text);
            npcAccept = NpcButton("Accept Quest", "ACCEPT", new(.05f, .035f), new(.60f, .105f), AcceptNpcQuest);
            npcComplete = NpcButton("Complete Quest", "COMPLETE QUEST", new(.05f, .035f), new(.60f, .105f), CompleteNpcQuest);
            npcDecline = NpcButton("Decline Quest", "DECLINE", new(.65f, .035f), new(.94f, .105f), CloseWindows);
            npcDeclineLabel = npcDecline.GetComponentInChildren<TextMeshProUGUI>();
            npcWindow.gameObject.SetActive(false);
        }

        private UnityEngine.UI.Button NpcButton(string name, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Block(name, npcWindow, Raised);
            Place(rect, min, max);
            rect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            PhasebreakUiTheme.StyleButton(button);
            Label(name + " Label", rect, text, 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Text);
            button.onClick.AddListener(action);
            return button;
        }

        private void OpenNpc(QuestNpc npc)
        {
            if (journal == null || !journal.CanInteractWith(npc)) return;
            PhasebreakInventoryHud.CloseMajorMenu();
            CloseWindows();
            interactionNpc = npc;
            npcOpen = true;
            npcWindow.gameObject.SetActive(true);
            RefreshNpcWindow();
            SetCursor();
        }

        private void RefreshNpcWindow()
        {
            if (!npcOpen || interactionNpc == null || journal == null) return;
            bool turnIn = journal.CanTurnInAt(interactionNpc);
            QuestDefinition quest = turnIn ? journal.ActiveQuest : journal.GetOfferedQuest(interactionNpc);
            bool offer = quest != null && !turnIn;
            presentedQuestId = quest != null ? quest.id : null;
            npcName.text = interactionNpc.DisplayName;
            npcRole.text = interactionNpc.Role;
            npcStatus.text = turnIn ? "?  READY TO COMPLETE" : offer ? "!  QUEST AVAILABLE" : "FRIENDLY";
            npcTitle.text = quest != null ? quest.title : "Greetings, traveler";
            npcDescription.text = quest != null ? quest.description : interactionNpc.Greeting;
            npcObjective.text = quest == null ? string.Empty : turnIn
                ? "OBJECTIVE COMPLETE\n" + quest.objectiveText
                : "OBJECTIVE\n" + quest.objectiveText + (quest.requiredCount > 1 ? $" ({quest.requiredCount})" : "");
            string item = quest != null && !string.IsNullOrWhiteSpace(quest.itemRewardId)
                ? "\n" + (build?.FindItemById(quest.itemRewardId)?.displayName ?? quest.itemRewardId) : "";
            npcRewards.text = quest != null ? $"REWARDS\n{quest.experienceReward} XP   •   {quest.marksReward} Rift Marks{item}" : "";
            npcAccept.gameObject.SetActive(offer);
            npcComplete.gameObject.SetActive(turnIn);
            npcDeclineLabel.text = offer ? "DECLINE" : "CLOSE";
        }

        private void AcceptNpcQuest()
        {
            if (journal != null && journal.TryAcceptQuest(interactionNpc, presentedQuestId)) CloseWindows();
            else RefreshNpcWindow();
        }

        private void CompleteNpcQuest()
        {
            if (journal != null && journal.CompleteQuestAt(interactionNpc, presentedQuestId)) CloseWindows();
            else RefreshNpcWindow();
        }

        private void UpdateNpcLifetime()
        {
            if (!npcOpen && !vendorOpen) return;
            if (interactionNpc == null || journal == null || !journal.CanInteractWith(interactionNpc) ||
                GameplayInputFocus.ChatFocused || GameplayInputFocus.MenuFocused || PhasebreakInventoryHud.IsMajorMenuOpen)
                CloseWindows();
        }

        private void CloseNpcWindow()
        {
            npcOpen = false;
            interactionNpc = null;
            presentedQuestId = null;
            if (npcWindow != null) npcWindow.gameObject.SetActive(false);
        }
    }
}
