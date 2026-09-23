using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Phasebreak.Gameplay
{
    // Binding overrides are applied to the existing gameplay InputActions, not a second input map.
    public static class PhasebreakSettings
    {
        public enum BindingModifier { None, Shift, Ctrl, Alt }

        public readonly struct BindingChord : IEquatable<BindingChord>
        {
            private const string Prefix = "chord:v1:";
            public readonly BindingModifier Modifier;
            public readonly string PrimaryPath;
            public bool IsBound => !string.IsNullOrWhiteSpace(PrimaryPath);

            public BindingChord(BindingModifier modifier, string primaryPath)
            { Modifier = modifier; PrimaryPath = NormalizePath(primaryPath); }

            public static BindingChord Parse(string saved)
            {
                if (string.IsNullOrWhiteSpace(saved)) return default;
                if (!saved.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    return new BindingChord(BindingModifier.None, saved);
                int separator = saved.IndexOf(':', Prefix.Length);
                if (separator < 0 || !Enum.TryParse(saved.Substring(Prefix.Length, separator - Prefix.Length), true,
                        out BindingModifier modifier) || modifier == BindingModifier.None)
                    return default;
                return new BindingChord(modifier, saved.Substring(separator + 1));
            }

            public string Serialize() => !IsBound ? string.Empty : Modifier == BindingModifier.None
                ? PrimaryPath : Prefix + Modifier + ":" + PrimaryPath;

            public string ToDisplayString(bool compact)
            {
                if (!IsBound) return "Unbound";
                string primary = PrimaryDisplay(PrimaryPath, compact);
                if (Modifier == BindingModifier.None) return primary;
                string modifier = compact ? Modifier switch
                { BindingModifier.Shift => "S", BindingModifier.Ctrl => "C", _ => "A" } : Modifier.ToString();
                return modifier + (compact ? "+" : " + ") + primary;
            }

            public bool Equals(BindingChord other) => Modifier == other.Modifier &&
                string.Equals(PrimaryPath, other.PrimaryPath, StringComparison.OrdinalIgnoreCase);
            public override bool Equals(object obj) => obj is BindingChord other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Modifier,
                StringComparer.OrdinalIgnoreCase.GetHashCode(PrimaryPath ?? string.Empty));
        }

        public readonly struct Binding
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string Category;
            public readonly string DefaultPath;
            public Binding(string id, string label, string category, string path)
            { Id = id; Label = label; Category = category; DefaultPath = path; }
        }

        public readonly struct PendingBindingChange
        {
            internal readonly string DestinationId;
            internal readonly BindingChord Proposed;
            internal readonly BindingChord OriginalDestination;
            internal readonly string[] ConflictIds;
            internal readonly BindingChord[] OriginalConflicts;

            internal PendingBindingChange(string destinationId, BindingChord proposed,
                BindingChord originalDestination, string[] conflictIds, BindingChord[] originalConflicts)
            {
                DestinationId = destinationId; Proposed = proposed; OriginalDestination = originalDestination;
                ConflictIds = conflictIds; OriginalConflicts = originalConflicts;
            }

            public bool HasConflicts => ConflictIds != null && ConflictIds.Length > 0;
            public string DestinationLabel => LabelFor(DestinationId);
            public string ProposedDisplay => Proposed.ToDisplayString(false);
            public string ExistingOwnersLabel
            {
                get
                {
                    if (!HasConflicts) return string.Empty;
                    string[] labels = new string[ConflictIds.Length];
                    for (int i = 0; i < labels.Length; i++) labels[i] = LabelFor(ConflictIds[i]);
                    return string.Join(", ", labels);
                }
            }
        }

        private readonly struct RegisteredAction
        {
            public readonly string Id;
            public readonly InputAction Action;
            public readonly int PrimaryBindingIndex;
            public RegisteredAction(string id, InputAction action, int primaryBindingIndex)
            { Id = id; Action = action; PrimaryBindingIndex = primaryBindingIndex; }
        }

        public static readonly Binding[] Bindings =
        {
            new("move.forward", "Move forward", "Movement", "<Keyboard>/w"),
            new("move.backward", "Move backward", "Movement", "<Keyboard>/s"),
            new("move.left", "Turn left", "Movement", "<Keyboard>/a"),
            new("move.right", "Turn right", "Movement", "<Keyboard>/d"),
            new("strafe.left", "Strafe left", "Movement", "<Keyboard>/q"),
            new("strafe.right", "Strafe right", "Movement", "<Keyboard>/e"),
            new("jump", "Jump / fly rise", "Movement", "<Keyboard>/space"),
            new("autorun", "Toggle auto run", "Movement", "<Keyboard>/numLock"),
            new("ability.1", "Action Slot 1", "Abilities", "<Keyboard>/1"),
            new("ability.2", "Action Slot 2", "Abilities", "<Keyboard>/2"),
            new("ability.3", "Action Slot 3", "Abilities", "<Keyboard>/3"),
            new("ability.4", "Action Slot 4", "Abilities", "<Keyboard>/4"),
            new("ability.5", "Action Slot 5", "Abilities", "<Keyboard>/5"),
            new("ability.6", "Action Slot 6", "Abilities", ""),
            new("ability.7", "Action Slot 7", "Abilities", ""),
            new("ability.8", "Action Slot 8", "Abilities", ""),
            new("ability.9", "Action Slot 9", "Abilities", ""),
            new("ability.10", "Action Slot 10", "Abilities", ""),
            new("ability.11", "Action Slot 11", "Abilities", ""),
            new("ability.12", "Action Slot 12", "Abilities", ""),
            new("ability.13", "Action Slot 13", "Abilities", ""),
            new("ability.14", "Action Slot 14", "Abilities", ""),
            new("ability.15", "Action Slot 15", "Abilities", ""),
            new("ability.16", "Action Slot 16", "Abilities", ""),
            new("ability.17", "Action Slot 17", "Abilities", ""),
            new("ability.18", "Action Slot 18", "Abilities", ""),
            new("ability.19", "Action Slot 19", "Abilities", ""),
            new("ability.20", "Action Slot 20", "Abilities", ""),
            new("ability.21", "Action Slot 21", "Abilities", ""),
            new("ability.22", "Action Slot 22", "Abilities", ""),
            new("ability.23", "Action Slot 23", "Abilities", ""),
            new("ability.24", "Action Slot 24", "Abilities", ""),
            new("inventory", "Inventory", "Panels", "<Keyboard>/b"),
            new("character", "Character", "Panels", "<Keyboard>/c"),
            new("talents", "Talents", "Panels", "<Keyboard>/t"),
            new("spellbook", "Spellbook", "Panels", "<Keyboard>/p"),
            new("journal", "Journal", "Panels", "<Keyboard>/j"),
            new("worldmap", "World map", "Panels", "<Keyboard>/m"),
            new("target.next", "Cycle targets", "Combat", "<Keyboard>/tab"),
            new("combat.autoAttack", "Toggle auto-attack", "Combat", "<Keyboard>/f"),
            new("interact", "Interact", "World", "<Keyboard>/e"),
            new("chat", "Chat", "Interface", "<Keyboard>/enter"),
            new("teleport", "Godmode cursor teleport", "Debug", "<Keyboard>/g"),
            new("arena.reset", "Reset combat arena", "Debug", "<Keyboard>/r"),
            new("debug.hud", "Build snapshot", "Debug", "<Keyboard>/f8"),
            new("developer.tools", "Developer Tools", "Debug", "<Keyboard>/f10")
        };

        private const string SettingsPrefix = "Phasebreak.Settings.v1.";
        private static readonly List<RegisteredAction> Actions = new();
        private static readonly Dictionary<string, int> BindingIndices = new(StringComparer.Ordinal);
        private static readonly BindingChord[] ChordCache = new BindingChord[Bindings.Length];
        private static readonly bool[] ChordLoaded = new bool[Bindings.Length];
        public static event Action BindingsChanged;

        static PhasebreakSettings()
        {
            for (int i = 0; i < Bindings.Length; i++) BindingIndices[Bindings[i].Id] = i;
            InputSystem.RegisterBindingComposite<PhasebreakBindingComposite>("PhasebreakBinding");
        }

        // Simple legacy paths remain valid; modified chords use a versioned serialized identity.
        public static string Path(string id) => Chord(id).Serialize();
        public static string Display(string id) => Chord(id).ToDisplayString(false);
        public static string DisplayCompact(string id) => Chord(id).ToDisplayString(true);

        public static InputAction Button(string id, string actionName)
        {
            int bindingIndex = IndexOf(id);
            BindingChord chord = ChordAt(bindingIndex);
            InputAction action = new(actionName, InputActionType.Button);
            action.AddCompositeBinding($"PhasebreakBinding(bindingIndex={bindingIndex})")
                .With("Primary", chord.PrimaryPath ?? string.Empty);
            Actions.Add(new RegisteredAction(id, action, 1));
            return action;
        }

        public static void Unregister(InputAction action) => Actions.RemoveAll(entry => entry.Action == action);

        public static bool TryPrepareRebind(string id, string primaryPath, BindingModifier modifier,
            out PendingBindingChange change, out string message)
        {
            change = default;
            if (!BindingIndices.TryGetValue(id, out int destinationIndex))
            { message = "Unknown action."; return false; }
            BindingChord proposed = new(modifier, primaryPath);
            if (!IsSupportedPrimary(proposed.PrimaryPath) || IsModifierPrimary(proposed.PrimaryPath) ||
                string.Equals(proposed.PrimaryPath, "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
            { message = "Escape and modifier keys cannot be primary bindings."; return false; }
            if (id != "chat" && string.Equals(proposed.PrimaryPath, "<Keyboard>/numpadEnter", StringComparison.OrdinalIgnoreCase))
            { message = "Numpad Enter is reserved for chat."; return false; }
            BindingChord original = ChordAt(destinationIndex);
            if (original.Equals(proposed))
            { message = "Binding unchanged."; return false; }

            List<string> conflictIds = new();
            List<BindingChord> conflictBindings = new();
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (i == destinationIndex || CanShare(id, Bindings[i].Id)) continue;
                BindingChord current = ChordAt(i);
                if (!current.Equals(proposed)) continue;
                conflictIds.Add(Bindings[i].Id); conflictBindings.Add(current);
            }
            change = new PendingBindingChange(id, proposed, original, conflictIds.ToArray(), conflictBindings.ToArray());
            message = change.HasConflicts ? "Binding conflict." : "Binding ready.";
            return true;
        }

        // Kept for non-UI callers; exact conflicts are never silently replaced.
        public static bool TryRebind(string id, string path, out string message)
        {
            BindingChord parsed = BindingChord.Parse(path);
            if (!TryPrepareRebind(id, parsed.PrimaryPath, parsed.Modifier, out PendingBindingChange change, out message))
                return message == "Binding unchanged.";
            if (change.HasConflicts)
            { message = "Already used by " + change.ExistingOwnersLabel + "."; return false; }
            return Apply(change, false, out message);
        }

        public static bool Apply(PendingBindingChange change, bool replaceConflicts, out string message)
        {
            if (!BindingIndices.ContainsKey(change.DestinationId) ||
                !Chord(change.DestinationId).Equals(change.OriginalDestination))
            { message = "Bindings changed while confirmation was open. Try again."; return false; }
            if (change.HasConflicts && !replaceConflicts)
            { message = "Binding conflict was not replaced."; return false; }
            for (int i = 0; i < (change.ConflictIds?.Length ?? 0); i++)
                if (!Chord(change.ConflictIds[i]).Equals(change.OriginalConflicts[i]))
                { message = "Bindings changed while confirmation was open. Try again."; return false; }

            Dictionary<string, BindingChord> oldValues = new() { [change.DestinationId] = change.OriginalDestination };
            if (replaceConflicts)
                for (int i = 0; i < change.ConflictIds.Length; i++) oldValues[change.ConflictIds[i]] = change.OriginalConflicts[i];
            try
            {
                if (replaceConflicts) foreach (string conflictId in change.ConflictIds) SetStoredChord(conflictId, default);
                SetStoredChord(change.DestinationId, change.Proposed);
                PlayerPrefs.Save();
                foreach (string changedId in oldValues.Keys) ApplyToActions(changedId);
                BindingsChanged?.Invoke();
                message = replaceConflicts && change.HasConflicts ? "Binding replaced." : "Binding saved.";
                return true;
            }
            catch (Exception exception)
            {
                foreach (KeyValuePair<string, BindingChord> old in oldValues) SetStoredChord(old.Key, old.Value);
                PlayerPrefs.Save();
                foreach (string changedId in oldValues.Keys) ApplyToActions(changedId);
                message = "Could not apply binding: " + exception.Message;
                return false;
            }
        }

        public static bool Clear(string id, out string message)
        {
            if (!BindingIndices.TryGetValue(id, out int index)) { message = "Unknown action."; return false; }
            BindingChord original = ChordAt(index);
            if (!original.IsBound) { message = "Already unbound."; return true; }
            return Apply(new PendingBindingChange(id, default, original, Array.Empty<string>(), Array.Empty<BindingChord>()),
                false, out message);
        }

        public static void ResetBindings()
        {
            foreach (Binding binding in Bindings) PlayerPrefs.DeleteKey(SettingsPrefix + "binding." + binding.Id);
            Array.Clear(ChordLoaded, 0, ChordLoaded.Length);
            PlayerPrefs.Save();
            foreach (Binding binding in Bindings) ApplyToActions(binding.Id);
            BindingsChanged?.Invoke();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void DebugClearRuntimeCache()
        {
            Array.Clear(ChordCache, 0, ChordCache.Length);
            Array.Clear(ChordLoaded, 0, ChordLoaded.Length);
        }
#endif

        internal static bool IsBindingActive(int bindingIndex)
        {
            if (bindingIndex < 0 || bindingIndex >= Bindings.Length) return false;
            BindingChord chord = ChordAt(bindingIndex);
            if (!chord.IsBound) return false;
            BindingModifier held = HeldModifier();
            if (chord.Modifier != BindingModifier.None) return held == chord.Modifier;
            if (held == BindingModifier.None) return true;
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (i == bindingIndex) continue;
                BindingChord other = ChordAt(i);
                if (other.Modifier == held && HasEnabledAction(Bindings[i].Id) &&
                    string.Equals(other.PrimaryPath, chord.PrimaryPath, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private static BindingModifier HeldModifier()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return BindingModifier.None;
            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
            int count = (shift ? 1 : 0) + (ctrl ? 1 : 0) + (alt ? 1 : 0);
            if (count != 1) return BindingModifier.None;
            return shift ? BindingModifier.Shift : ctrl ? BindingModifier.Ctrl : BindingModifier.Alt;
        }

        private static BindingChord Chord(string id) => ChordAt(IndexOf(id));
        private static BindingChord ChordAt(int index)
        {
            if (!ChordLoaded[index])
            {
                Binding binding = Bindings[index];
                ChordCache[index] = BindingChord.Parse(PlayerPrefs.GetString(SettingsPrefix + "binding." + binding.Id, binding.DefaultPath));
                ChordLoaded[index] = true;
            }
            return ChordCache[index];
        }

        private static int IndexOf(string id) => BindingIndices.TryGetValue(id, out int index)
            ? index : throw new ArgumentException("Unknown binding: " + id);
        private static void SetStoredChord(string id, BindingChord chord)
        {
            int index = IndexOf(id); ChordCache[index] = chord; ChordLoaded[index] = true;
            PlayerPrefs.SetString(SettingsPrefix + "binding." + id, chord.Serialize());
        }
        private static void ApplyToActions(string id)
        {
            BindingChord chord = Chord(id);
            foreach (RegisteredAction entry in Actions)
                if (entry.Id == id) entry.Action.ApplyBindingOverride(entry.PrimaryBindingIndex, chord.PrimaryPath ?? string.Empty);
        }

        private static bool HasEnabledAction(string id)
        {
            foreach (RegisteredAction entry in Actions)
                if (entry.Id == id && entry.Action.enabled) return true;
            return false;
        }

        private static bool CanShare(string left, string right) =>
            left == "strafe.right" && right == "interact" || left == "interact" && right == "strafe.right";
        private static string LabelFor(string id) => id != null && BindingIndices.TryGetValue(id, out int index) ? Bindings[index].Label : id ?? string.Empty;
        private static bool IsSupportedPrimary(string path) => path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("<Mouse>/leftButton", StringComparison.OrdinalIgnoreCase) || path.Equals("<Mouse>/rightButton", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("<Mouse>/middleButton", StringComparison.OrdinalIgnoreCase) || path.Equals("<Mouse>/backButton", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("<Mouse>/forwardButton", StringComparison.OrdinalIgnoreCase);
        private static bool IsModifierPrimary(string path) => path.EndsWith("/leftShift", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/rightShift", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/leftCtrl", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/rightCtrl", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/leftAlt", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/rightAlt", StringComparison.OrdinalIgnoreCase);
        private static string NormalizePath(string path) => (path ?? string.Empty).Trim();
        private static string PrimaryDisplay(string path, bool compact)
        {
            if (path.Equals("<Mouse>/leftButton", StringComparison.OrdinalIgnoreCase)) return compact ? "M1" : "Mouse 1";
            if (path.Equals("<Mouse>/rightButton", StringComparison.OrdinalIgnoreCase)) return compact ? "M2" : "Mouse 2";
            if (path.Equals("<Mouse>/middleButton", StringComparison.OrdinalIgnoreCase)) return compact ? "M3" : "Mouse 3";
            if (path.Equals("<Mouse>/backButton", StringComparison.OrdinalIgnoreCase)) return compact ? "M4" : "Mouse 4";
            if (path.Equals("<Mouse>/forwardButton", StringComparison.OrdinalIgnoreCase)) return compact ? "M5" : "Mouse 5";
            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public static float GetFloat(string name, float fallback) => PlayerPrefs.GetFloat(SettingsPrefix + name, fallback);
        public static int GetInt(string name, int fallback) => PlayerPrefs.GetInt(SettingsPrefix + name, fallback);
        public static bool Has(string name) => PlayerPrefs.HasKey(SettingsPrefix + name);
        public static void SetFloat(string name, float value) { PlayerPrefs.SetFloat(SettingsPrefix + name, value); PlayerPrefs.Save(); }
        public static void SetInt(string name, int value) { PlayerPrefs.SetInt(SettingsPrefix + name, value); PlayerPrefs.Save(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        { Actions.Clear(); Array.Clear(ChordLoaded, 0, ChordLoaded.Length); BindingsChanged = null; }
    }

    // Shared by every rebindable action so chord gating and base-key precedence stay centralized.
    internal sealed class PhasebreakBindingComposite : InputBindingComposite<float>
    {
        [InputControl(layout = "Button")] public int primary;
        public int bindingIndex;
        public override float ReadValue(ref InputBindingCompositeContext context) =>
            PhasebreakSettings.IsBindingActive(bindingIndex) ? context.ReadValue<float>(primary) : 0f;
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context) => ReadValue(ref context);
    }
}
