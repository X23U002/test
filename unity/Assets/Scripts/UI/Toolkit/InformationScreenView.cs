using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>お問い合わせ (UI Toolkit版)。uGUI版 InformationScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class InformationScreenView : MonoBehaviour
    {
        private VisualElement formPage;
        private VisualElement completePage;
        private TextField contactTextField;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            formPage = root.Q<VisualElement>("form-page");
            completePage = root.Q<VisualElement>("complete-page");
            contactTextField = root.Q<TextField>("contact-text-field");

            formPage.style.display = DisplayStyle.Flex;
            completePage.style.display = DisplayStyle.None;

            root.Q<Button>("close-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("complete-close-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("back-to-map-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("select-image-button").clicked += () =>
                Debug.Log("画像選択は未実装です (NativeGallery等の導入が必要)");

            root.Q<Button>("send-button").clicked += SendContact;
        }

        private void SendContact()
        {
            // TODO: 実際にはFirestore等へ問い合わせ内容を送信する
            formPage.style.display = DisplayStyle.None;
            completePage.style.display = DisplayStyle.Flex;
        }
    }
}
