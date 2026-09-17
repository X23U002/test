using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>聖地登録 (UI Toolkit版)。uGUI版 RegisterSpotScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RegisterSpotScreenView : MonoBehaviour
    {
        private VisualElement inputPage;
        private VisualElement completePage;
        private TextField animeNameField;
        private TextField addressField;
        private TextField spotTextField;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            inputPage = root.Q<VisualElement>("input-page");
            completePage = root.Q<VisualElement>("complete-page");
            animeNameField = root.Q<TextField>("anime-name-field");
            addressField = root.Q<TextField>("address-field");
            spotTextField = root.Q<TextField>("spot-text-field");

            ShowInputPage();

            root.Q<Button>("close-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("complete-close-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("back-to-map-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("register-button").clicked += CompleteRegister;

            // Web版の <input type="file"> 相当。端末のギャラリーを開く処理は
            // NativeGallery等のプラグイン導入後にここへ実装する
            root.Q<Button>("select-image-button").clicked += () =>
                Debug.Log("画像選択は未実装です (NativeGallery等の導入が必要)");
        }

        private void ShowInputPage()
        {
            inputPage.style.display = DisplayStyle.Flex;
            completePage.style.display = DisplayStyle.None;
        }

        private void CompleteRegister()
        {
            if (string.IsNullOrEmpty(animeNameField.value.Trim()))
            {
                Debug.LogWarning("作品名を入力してください。");
                return;
            }

            if (string.IsNullOrEmpty(addressField.value.Trim()))
            {
                Debug.LogWarning("住所を入力してください。");
                return;
            }

            if (string.IsNullOrEmpty(spotTextField.value.Trim()))
            {
                Debug.LogWarning("スポット説明を入力してください。");
                return;
            }

            // TODO: FirestoreのspotRequestsコレクション等へ申請データを保存する

            inputPage.style.display = DisplayStyle.None;
            completePage.style.display = DisplayStyle.Flex;
        }
    }
}
