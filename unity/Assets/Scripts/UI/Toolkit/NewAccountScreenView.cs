using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>新規登録画面 (UI Toolkit版)。uGUI版 NewAccountScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class NewAccountScreenView : MonoBehaviour
    {
        [SerializeField] private ConfirmScreenView confirmScreen;

        private TextField usernameField;
        private TextField passwordField;
        private TextField emailField;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            usernameField = root.Q<TextField>("username-field");
            passwordField = root.Q<TextField>("password-field");
            emailField = root.Q<TextField>("email-field");

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowLogin();
            root.Q<Button>("confirm-button").clicked += OnConfirmClicked;
        }

        private void OnConfirmClicked()
        {
            confirmScreen.Populate(usernameField.value, emailField.value);
            AppShellController.Instance.ShowConfirm();
        }
    }
}
