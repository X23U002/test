using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>パスワード再発行画面 (UI Toolkit版)。uGUI版 ForgotPasswordScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class ForgotPasswordScreenView : MonoBehaviour
    {
        private VisualElement completionModal;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            completionModal = root.Q<VisualElement>("completion-modal");
            completionModal.style.display = DisplayStyle.None;

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowLogin();

            // 実際のメール送信は未実装。Web版と同じく完了モーダルを出すだけ
            root.Q<Button>("submit-button").clicked += () => completionModal.style.display = DisplayStyle.Flex;

            root.Q<Button>("modal-close-button").clicked += () =>
            {
                completionModal.style.display = DisplayStyle.None;
                AppShellController.Instance.ShowLogin();
            };
        }
    }
}
