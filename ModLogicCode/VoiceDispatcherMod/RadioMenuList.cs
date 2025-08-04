using System;
using System.Collections.Generic;
using DV;

namespace VoiceDispatcherMod {
    public class ActionItem {
        public Func<string> Name;
        private Action Run;
        public Func<string> ActionName;
        
        public ActionItem(string key, Action run, Func<string> actionName = null) {
            Name = FromKey(key);
            Run = run;
            ActionName = actionName ?? Name;
        }
        
        public ActionItem(Func<string> name, Action run, Func<string> actionName = null) {
            Name = name;
            Run = run;
            ActionName = actionName ?? (() => name());
        }
        
        public void OnUse() {
            Run?.Invoke();
        }

        public override string ToString() {
            return Name();
        }

        public static Func<string> FromKey(string key) {
            return () => JsonLinesLoader.GetRandomAndReplace(key);
        }
        
        public static Func<string> FromKey(string key, string blankKey, Func<string> replacement) {
            return () => JsonLinesLoader.GetRandomAndReplace(key, new Dictionary<string, string> {
                { blankKey, replacement() }
            });
        }
    }
    
    public class RadioMenuList {
        
        private int _selectedActionIndex = 0;
        private int _selectedActionScroll = 0;
        private const int MaxActionsPerPage = 6;

        private List<ActionItem> _availableActions = new();

        private CommsRadioDisplay display;
        
        public bool IsOpen;
        
        public RadioMenuList(CommsRadioDisplay display) {
            this.display = display;
        }
        
        public void SetAvailableActions(List<ActionItem> actions) {
            _availableActions = actions;
            _selectedActionIndex = 0;
            _selectedActionScroll = 0;
            ScrollToSeeSelectedAction();
            RenderActions();
            IsOpen = true;
        }
        
        public void ClearActions() {
            _availableActions = new List<ActionItem>();
            _selectedActionIndex = 0;
            _selectedActionScroll = 0;
            IsOpen = false;
        }
        
        public void RenderActions() {
            if (_availableActions.Count == 0) {
                display.SetContent("Error: No actions available");
                display.SetAction("");
                return;
            }
            var message = "";
            var maxActions = _availableActions.Count > MaxActionsPerPage ? MaxActionsPerPage-2 : _availableActions.Count;
            if (_selectedActionScroll > 0) {
                message += "   ...\n";
            } else if (_availableActions.Count > MaxActionsPerPage) {
                message += "\n";
            }
            for (int i = _selectedActionScroll; i < maxActions + _selectedActionScroll; i++) {
                if (i < 0 || i >= _availableActions.Count) {
                    message += "\n";
                } else if (i == _selectedActionIndex) {
                    message += $"> {_availableActions[i]}\n";
                } else {
                    message += $"   {_availableActions[i]}\n";
                }
            }
            if (_selectedActionScroll + maxActions < _availableActions.Count) {
                message += "   ...\n";
            }
            display.SetContent(message);
            display.SetAction(_availableActions[_selectedActionIndex].ActionName());
        }
        
        public void ScrollToSeeSelectedAction() {
            if (_availableActions.Count <= MaxActionsPerPage) {
                _selectedActionScroll = 0;
                return;
            }
            if (_selectedActionScroll > _selectedActionIndex) {
                _selectedActionScroll = _selectedActionIndex;
            } else if (_selectedActionScroll + MaxActionsPerPage <= _selectedActionIndex) {
                _selectedActionScroll = _selectedActionIndex - MaxActionsPerPage + 1;
            }
        }
        
        public bool ButtonACustomAction() {
            _selectedActionIndex -= 1;
            if (_selectedActionIndex < 0) {
                _selectedActionIndex = 0;
                return false;
            }
            ScrollToSeeSelectedAction();

            RenderActions();
            return true;
        }

        public bool ButtonBCustomAction() {
            _selectedActionIndex += 1;
            if (_selectedActionIndex >= _availableActions.Count) {
                _selectedActionIndex = _availableActions.Count-1;
                return false;
            }
            ScrollToSeeSelectedAction();
            
            RenderActions();
            return true;
        }

        public void OnUse() {
            if (_selectedActionIndex < 0 || _selectedActionIndex >= _availableActions.Count) {
                return; // No action available
            }
            _availableActions[_selectedActionIndex].OnUse();
        }
        
    }
}