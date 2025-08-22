using System;
using UnityEngine;
using UnityEngine.UIElements;


namespace Popup
{
    public class PopupTest : MonoBehaviour
    {
        VisualElement _root;
        private void OnEnable()
        {
            UIDocument uidoc = GetComponent<UIDocument>();
            _root = uidoc.rootVisualElement;
            PopUpWindow popup = new PopUpWindow();
            _root.Add(popup);
            popup.confirmed += OnConfirm;
            popup.cancelled += OnCancel;

        }

        private void OnCancel()
        {
            Debug.Log("Cancelled");
            _root.Remove(_root.ElementAt(0));

        }

        private void OnConfirm()
        {
            Debug.Log("Confirmed");
        }
    }
}
