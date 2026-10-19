using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SeagullStorm
{
    /// <summary>Seagull's menus use one ordinary InputSystem/EventSystem navigation path.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SeagullMenuNavigation : MonoBehaviour
    {
        private EventSystem _events;
        private InputSystemUIInputModule _module;
        private InputActionAsset _actions;
        private InputAction _tab;
        private SeagullMenuScope _top;
        private Selectable[] _controls = Array.Empty<Selectable>();
        private GameObject _revealed;
        private int _tabDirection;
        private bool _cancel, _cancelEditing;

        public static SeagullMenuNavigation Ensure()
        {
            var events = EventSystem.current;
            if (events == null) events = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            return events.GetComponent<SeagullMenuNavigation>() ?? events.gameObject.AddComponent<SeagullMenuNavigation>();
        }
        private void Awake()
        {
            _events = GetComponent<EventSystem>();
            foreach (var old in GetComponents<BaseInputModule>())
                if (!(old is InputSystemUIInputModule)) { old.enabled = false; Destroy(old); }
            _module = GetComponent<InputSystemUIInputModule>() ?? gameObject.AddComponent<InputSystemUIInputModule>();
            _module.enabled = false;
            _module.AssignDefaultActions();
            // Package defaults are shared. Tab/Space additions belong only to this game.
            _actions = Instantiate(_module.actionsAsset);
            _actions.Disable();
            _module.actionsAsset = _actions;
            _module.submit.action.AddBinding("<Keyboard>/space");
            _tab = _actions.FindActionMap("UI").AddAction("CycleFocus", InputActionType.Button, "<Keyboard>/tab");
            _tab.performed += OnTab;
            _module.cancel.action.performed += OnCancel;
            _module.deselectOnBackgroundClick = false;
            _events.sendNavigationEvents = true;
            _module.enabled = true;
            _tab.Enable();
        }
        private TMP_InputField SelectedInput => _events.currentSelectedGameObject?.GetComponent<TMP_InputField>();
        private void OnTab(InputAction.CallbackContext context)
        {
            _tabDirection = Keyboard.current?.shiftKey.isPressed == true ? -1 : 1;
            SelectedInput?.DeactivateInputField();
        }
        private void OnCancel(InputAction.CallbackContext context)
        {
            _cancel = true;
            _cancelEditing = SelectedInput?.isFocused == true;
        }
        private void Update() => Refresh();
        private void LateUpdate()
        {
            Refresh();
            if (_cancel)
            {
                _cancel = false; _tabDirection = 0;
                if (_cancelEditing) { SelectedInput?.DeactivateInputField(); return; }
                var dropdown = _top != null ? _top.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(d => d.IsExpanded) : null;
                if (dropdown != null) { dropdown.Hide(); Select(dropdown); return; }
                if (_top != null) _top.Back?.Invoke();
                else if (GameManager.Instance?.CurrentState == GameState.Run) GameManager.Instance.ChangeState(GameState.Paused);
                return;
            }
            if (_tabDirection != 0 && _controls.Length > 0)
            {
                int current = Array.FindIndex(_controls, c => c.gameObject == _events.currentSelectedGameObject);
                Select(_controls[(current + _tabDirection + _controls.Length) % _controls.Length]);
            }
            _tabDirection = 0;
            var selected = _events.currentSelectedGameObject?.GetComponent<Selectable>();
            if (selected != null && Array.IndexOf(_controls, selected) >= 0)
            {
                _top.Last = selected;
                if (_revealed != selected.gameObject) { Reveal(selected); _revealed = selected.gameObject; }
            }
        }
        private void Refresh()
        {
            var active = SeagullMenuScope.Active.Where(s => s != null && s.isActiveAndEnabled && s.Group != null).ToArray();
            var top = active.OrderByDescending(s => s.Activation).FirstOrDefault();
            bool changed = top != _top;
            if (_top != null && _events.currentSelectedGameObject != null)
            {
                var previous = _events.currentSelectedGameObject.GetComponent<Selectable>();
                if (previous != null && previous.transform.IsChildOf(_top.transform)) _top.Last = previous;
            }
            _top = top;
            foreach (var scope in active) { scope.Group.interactable = scope == top; scope.Group.blocksRaycasts = scope == top; }
            var dropdown = top != null ? top.GetComponentsInChildren<TMP_Dropdown>().FirstOrDefault(d => d.IsExpanded) : null;
            var controls = top != null ? (dropdown != null ? dropdown.GetComponentsInChildren<Selectable>() : top.GetComponentsInChildren<Selectable>())
                .Where(c => c.IsActive() && c.IsInteractable() && (dropdown == null || c != dropdown)).ToArray() : Array.Empty<Selectable>();
            if (changed || !_controls.SequenceEqual(controls))
            {
                _controls = controls;
                Canvas.ForceUpdateCanvases();
                foreach (var control in controls)
                {
                    if (control.GetComponent<SeagullSelectionOutline>() == null) control.gameObject.AddComponent<SeagullSelectionOutline>();
                    control.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnUp = Nearest(control, Vector2.up), selectOnDown = Nearest(control, Vector2.down),
                        selectOnLeft = Nearest(control, Vector2.left), selectOnRight = Nearest(control, Vector2.right) };
                }
            }
            if (changed || !_controls.Any(c => c.gameObject == _events.currentSelectedGameObject))
                Select(top != null && _controls.Contains(top.Last) ? top.Last
                    : top != null && _controls.Contains(top.First) ? top.First : _controls.FirstOrDefault());
        }
        private Selectable Nearest(Selectable origin, Vector2 direction)
        {
            var centre = origin.transform.TransformPoint(((RectTransform)origin.transform).rect.center);
            float best = 0; Selectable result = null;
            foreach (var candidate in _controls)
            {
                if (candidate == origin) continue;
                Vector2 delta = candidate.transform.TransformPoint(((RectTransform)candidate.transform).rect.center) - centre;
                float dot = Vector2.Dot(delta, direction);
                float score = dot > .01f ? dot / delta.sqrMagnitude : 0;
                if (score > best) { best = score; result = candidate; }
            }
            return result;
        }
        public static void Focus(Selectable control)
        {
            var navigation = Ensure(); navigation.Refresh();
            if (control == null || navigation._controls.Contains(control)) navigation.Select(control);
        }
        private void Select(Selectable control)
        {
            var target = control != null ? control.gameObject : null;
            _events.firstSelectedGameObject = _top?.First != null ? _top.First.gameObject : target;
            if (_events.currentSelectedGameObject == target) return;
            SelectedInput?.DeactivateInputField(); _events.SetSelectedGameObject(target);
        }
        private static void Reveal(Selectable control)
        {
            var scroll = control.GetComponentInParent<ScrollRect>();
            if (scroll == null || !scroll.vertical || scroll.content == null || scroll.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, control.transform);
            float offset = bounds.min.y < scroll.viewport.rect.yMin ? scroll.viewport.rect.yMin - bounds.min.y
                : bounds.max.y > scroll.viewport.rect.yMax ? scroll.viewport.rect.yMax - bounds.max.y : 0;
            if (Mathf.Abs(offset) < .1f) return;
            scroll.StopMovement(); var position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + offset, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
            scroll.content.anchoredPosition = position;
        }
        private void OnDestroy()
        {
            if (_tab != null) _tab.performed -= OnTab;
            if (_module?.cancel?.action != null) _module.cancel.action.performed -= OnCancel;
            if (_actions != null) { _actions.Disable(); Destroy(_actions); }
        }
    }
    public sealed class SeagullMenuScope : MonoBehaviour
    {
        internal static readonly List<SeagullMenuScope> Active = new List<SeagullMenuScope>();
        private static long _sequence;
        internal long Activation;
        internal CanvasGroup Group;
        internal Action Back;
        internal Selectable First, Last;
        public static void Bind(GameObject root, Action back, Selectable first)
        {
            SeagullMenuNavigation.Ensure();
            var scope = root.GetComponent<SeagullMenuScope>() ?? root.AddComponent<SeagullMenuScope>();
            scope.Group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
            scope.Back = back; scope.First = first;
        }
        private void OnEnable() { Activation = ++_sequence; Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
    }
}
