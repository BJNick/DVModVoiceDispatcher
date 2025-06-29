using DV;

namespace VoiceDispatcherMod {
    public class RadioMenuList {
        
        private int SelectedActionIndex = 0;
        private int SelectedActionScroll = 0;
        private const int MaxActionsPerPage = 4;

        private string[] AvailableActions = new[] {
            "Action Alfa",
            "Action Bravo",
            "Action Charlie",
            "Action Delta",
            "Action Echo",
            "Action Foxtrot",
            "Action Golf",
            "Action Hotel",
            "Action India",
        };
        
        public void RenderActions(CommsRadioDisplay display) {
            var message = "";
            var maxActions = AvailableActions.Length > MaxActionsPerPage ? MaxActionsPerPage : AvailableActions.Length;
            if (SelectedActionScroll > 0) {
                message += "   ...\n";
            } else {
                message += "\n";
            }
            for (int i = SelectedActionScroll; i < maxActions + SelectedActionScroll; i++) {
                if (i < 0 || i >= AvailableActions.Length) {
                    message += "\n";
                } else if (i == SelectedActionIndex) {
                    message += $"> {AvailableActions[i]}\n";
                } else {
                    message += $"   {AvailableActions[i]}\n";
                }
            }
            if (SelectedActionScroll + maxActions < AvailableActions.Length) {
                message += "   ...\n";
            }
            display.SetContent(message);
        }
        
        public void ScrollToSeeSelectedAction() {
            SelectedActionScroll = SelectedActionIndex;
        }
        
        public bool ButtonACustomAction(CommsRadioDisplay display) {
            SelectedActionIndex -= 1;
            if (SelectedActionIndex < 0) {
                SelectedActionIndex = 0;
                return false;
            }
            ScrollToSeeSelectedAction();

            RenderActions(display);
            return true;
        }

        public bool ButtonBCustomAction(CommsRadioDisplay display) {
            SelectedActionIndex += 1;
            if (SelectedActionIndex >= AvailableActions.Length) {
                SelectedActionIndex = AvailableActions.Length-1;
                return false;
            }
            ScrollToSeeSelectedAction();
            
            RenderActions(display);
            return true;
        }
        
    }
}