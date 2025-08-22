using UnityEngine;
using System;
using UnityEngine.UIElements;

namespace Popup
{
    public class PopUpWindow : VisualElement
    {
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<PopUpWindow> { }
        private const string styleResource = "PopUpWindowStyleSheet";
        private const string ussPopup = "popup_window";
        private const string ussPopupContainer = "popup_container";
        private const string ussHorContainer = "horizontal_container";
        private const string ussPopupMsg = "popup_msg";
        private const string ussPopupButton = "popup_button";
        private const string ussCancel = "button-cancel";
        private const string ussConfirm = "button-confirm";
        public PopUpWindow()
        {
            styleSheets.Add(Resources.Load<StyleSheet>(styleResource));

            AddToClassList(ussPopupContainer);
            VisualElement window = new VisualElement();
            window.AddToClassList(ussPopup);
            hierarchy.Add(window);

            //text
            VisualElement HorizontalContainerText = new VisualElement();
            HorizontalContainerText.AddToClassList(ussHorContainer);
            window.Add(HorizontalContainerText);

            Label msgLabel = new Label();
            msgLabel.text = "This is a popup window 222";
            msgLabel.AddToClassList(ussPopupMsg);
            HorizontalContainerText.Add(msgLabel);

            //buttons
            VisualElement HorizontalContainerButton = new VisualElement();
            HorizontalContainerButton.AddToClassList(ussHorContainer);
            window.Add(HorizontalContainerButton);

            Button confirmButton = new Button() { text = "Confirm" };
            confirmButton.AddToClassList(ussPopupButton);
            confirmButton.AddToClassList(ussConfirm);
            HorizontalContainerButton.Add(confirmButton);

            Button cancelButton = new Button() { text = "cancel" };
            cancelButton.AddToClassList(ussPopupButton);
            cancelButton.AddToClassList(ussCancel);
            HorizontalContainerButton.Add(cancelButton);

            confirmButton.clicked += OnConfirm;
            cancelButton.clicked += OnCancel;
        }
        public event Action confirmed;
        public event Action cancelled;


        private void OnCancel()
        {
            cancelled?.Invoke();
        }

        private void OnConfirm()
        {
            confirmed?.Invoke();
        }
    }
}
