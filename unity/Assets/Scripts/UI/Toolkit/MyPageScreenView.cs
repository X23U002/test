using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>マイページ (UI Toolkit版)。uGUI版 MyPageScreen.cs と同じ処理。</summary>
    [RequireComponent(typeof(UIDocument))]
    public class MyPageScreenView : MonoBehaviour
    {
        private const string SelectedIconClass = "icon-option--selected";

        private Label profileName;
        private Label profileIcon;
        private VisualElement editModal;
        private TextField editNameField;
        private List<Button> iconOptions;
        private string pendingIcon = "👤";

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            profileName = root.Q<Label>("profile-name");
            profileIcon = root.Q<Label>("profile-icon");
            editModal = root.Q<VisualElement>("edit-modal");
            editNameField = root.Q<TextField>("edit-name-field");
            editModal.style.display = DisplayStyle.None;

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowMap();
            root.Q<Button>("history-button").clicked += () => AppShellController.Instance.ShowHistory();
            root.Q<Button>("stamp-button").clicked += () => AppShellController.Instance.ShowStamp();
            root.Q<Button>("logout-button").clicked += () => AppShellController.Instance.ShowLogin();

            root.Q<Button>("edit-profile-button").clicked += OpenEditModal;
            root.Q<Button>("modal-close-button").clicked += () => editModal.style.display = DisplayStyle.None;
            root.Q<Button>("save-button").clicked += SaveProfile;

            // アイコン候補はUXMLに並べた Button をまとめて取得する
            iconOptions = root.Q<VisualElement>("icon-grid").Query<Button>().ToList();

            foreach (var option in iconOptions)
            {
                var icon = option.text;
                option.clicked += () => SelectIcon(option, icon);
            }
        }

        private void OpenEditModal()
        {
            editNameField.value = profileName.text;
            pendingIcon = profileIcon.text;
            editModal.style.display = DisplayStyle.Flex;
        }

        private void SelectIcon(Button selected, string icon)
        {
            pendingIcon = icon;

            // クラスの付け外しで選択枠を表現する (Web版の classList.add/remove と同じ)
            foreach (var option in iconOptions)
            {
                option.EnableInClassList(SelectedIconClass, option == selected);
            }
        }

        private void SaveProfile()
        {
            var newName = editNameField.value.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning("名前を入力してください。");
                return;
            }

            profileName.text = newName;
            profileIcon.text = pendingIcon;
            editModal.style.display = DisplayStyle.None;
        }
    }
}
