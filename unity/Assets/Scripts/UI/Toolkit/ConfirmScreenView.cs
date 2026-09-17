using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>登録確認画面 (UI Toolkit版)。uGUI版 ConfirmScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConfirmScreenView : MonoBehaviour
    {
        private Label nameText;
        private Label emailText;

        // 画面が非表示の間に Populate が呼ばれても値を保持できるようにしておく
        private string pendingName = "-";
        private string pendingEmail = "-";

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            nameText = root.Q<Label>("name-text");
            emailText = root.Q<Label>("email-text");
            ApplyValues();

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowNewAccount();

            root.Q<Button>("register-button").clicked += () =>
            {
                Debug.Log("登録が完了しました！地図画面へ移動します。");
                AppShellController.Instance.ShowMap();
            };
        }

        public void Populate(string userName, string email)
        {
            pendingName = userName;
            pendingEmail = email;
            ApplyValues();
        }

        private void ApplyValues()
        {
            if (nameText == null)
            {
                return;
            }

            nameText.text = pendingName;
            emailText.text = pendingEmail;
        }
    }
}
