using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    // Binding overrides are applied to the existing gameplay InputActions, not a second input map.
    public static class PhasebreakSettings
    {
        public readonly struct Binding
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string Category;
            public readonly string DefaultPath;
            public Binding(string id, string label, string category, string path)
            { Id = id; Label = label; Category = category; DefaultPath = path; }
        }

        private readonly struct RegisteredAction
        {
            public readonly string Id;
            public readonly InputAction Action;
            public readonly int Index;
            public RegisteredAction(string id, InputAction action, int index)
            { Id = id; Action = action; Index = index; }
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
            new("debug.hud", "Build snapshot", "Debug", "<Keyboard>/f8")
        };

        private const string Prefix = "Phasebreak.Settings.v1.";
        private static readonly List<RegisteredAction> Actions = new();
        public static event Action BindingsChanged;

        public static string Path(string id)
        {
            foreach (Binding binding in Bindings)
                if (binding.Id == id)
                    return PlayerPrefs.GetString(Prefix + "binding." + id, binding.DefaultPath);
            throw new ArgumentException("Unknown binding: " + id);
        }

        public static string Display(string id)
        {
            string path = Path(id);
            return string.IsNullOrWhiteSpace(path) ? "Unbound" : InputControlPath.ToHumanReadableString(path,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public static InputAction Button(string id, string actionName)
        {
            InputAction action = new(actionName, InputActionType.Button);
            action.AddBinding(new InputBinding { path = Path(id) });
            Actions.Add(new RegisteredAction(id, action, 0));
            return action;
        }

        public static void RegisterCompositePart(string id, InputAction action, int bindingIndex)
        {
            action.ApplyBindingOverride(bindingIndex, Path(id));
            Actions.Add(new RegisteredAction(id, action, bindingIndex));
        }

        public static void Unregister(InputAction action) => Actions.RemoveAll(entry => entry.Action == action);

        public static bool TryRebind(string id, string path, out string message)
        {
            bool keyboard = path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase);
            bool mouseButton = path.Equals("<Mouse>/leftButton", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("<Mouse>/rightButton", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("<Mouse>/middleButton", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("<Mouse>/backButton", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("<Mouse>/forwardButton", StringComparison.OrdinalIgnoreCase);
            if ((!keyboard && !mouseButton) ||
                path.Equals("<Keyboard>/escape", StringComparison.OrdinalIgnoreCase))
            { message = "Escape is reserved for closing menus."; return false; }
            if (id != "chat" && path.Equals("<Keyboard>/numpadEnter", StringComparison.OrdinalIgnoreCase))
            { message = "Numpad Enter is reserved for chat."; return false; }
            if (Path(id).Equals(path, StringComparison.OrdinalIgnoreCase))
            { message = "Binding unchanged."; return true; }
            bool found = false;
            foreach (Binding binding in Bindings)
            {
                if (binding.Id == id) { found = true; continue; }
                // Strafe right and interaction intentionally share E in the shipped controls.
                if (Path(binding.Id).Equals(path, StringComparison.OrdinalIgnoreCase))
                { message = $"Already used by {binding.Label}."; return false; }
            }
            if (!found) { message = "Unknown action."; return false; }
            PlayerPrefs.SetString(Prefix + "binding." + id, path);
            PlayerPrefs.Save();
            Apply(id);
            BindingsChanged?.Invoke();
            message = "Binding saved.";
            return true;
        }

        public static void ResetBindings()
        {
            foreach (Binding binding in Bindings)
                PlayerPrefs.DeleteKey(Prefix + "binding." + binding.Id);
            PlayerPrefs.Save();
            foreach (Binding binding in Bindings) Apply(binding.Id);
            BindingsChanged?.Invoke();
        }

        private static void Apply(string id)
        {
            foreach (RegisteredAction entry in Actions)
                if (entry.Id == id)
                    entry.Action.ApplyBindingOverride(entry.Index, Path(id));
        }

        public static float GetFloat(string name, float fallback) => PlayerPrefs.GetFloat(Prefix + name, fallback);
        public static int GetInt(string name, int fallback) => PlayerPrefs.GetInt(Prefix + name, fallback);
        public static bool Has(string name) => PlayerPrefs.HasKey(Prefix + name);
        public static void SetFloat(string name, float value) { PlayerPrefs.SetFloat(Prefix + name, value); PlayerPrefs.Save(); }
        public static void SetInt(string name, int value) { PlayerPrefs.SetInt(Prefix + name, value); PlayerPrefs.Save(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime() { Actions.Clear(); BindingsChanged = null; }
    }
}
