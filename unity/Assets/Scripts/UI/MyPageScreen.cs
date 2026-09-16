using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>マイページ画面。Web版 mypage.js を移植。</summary>
    public class MyPageScreen : MonoBehaviour
    {
        [SerializeField] private Text currentNameText;
        [SerializeField] private Text currentIconText;

        [SerializeField] private Button historyButton;
        [SerializeField] private Button stampButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button editProfileButton;

        [Header("編集モーダル")]
        [SerializeField] private GameObject editModal;
        [SerializeField] private InputField editNameInput;
        [SerializeField] private Button[] iconOptionButtons; // 各ボタンのテキストが絵文字アイコン
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeEditModalButton;

        private string pendingIcon = "👤";

        private void Awake()
        {
            historyButton.onClick.AddListener(() => AppShellController.Instance.ShowHistory());
            stampButton.onClick.AddListener(() => AppShellController.Instance.ShowStamp());

            logoutButton.onClick.AddListener(() => AppShellController.Instance.ShowLogin());

            editProfileButton.onClick.AddListener(OpenEditModal);
            closeEditModalButton.onClick.AddListener(() => editModal.SetActive(false));
            saveButton.onClick.AddListener(SaveProfile);

            foreach (var button in iconOptionButtons)
            {
                var icon = button.GetComponentInChildren<Text>().text;
                button.onClick.AddListener(() => pendingIcon = icon);
            }

            editModal.SetActive(false);
        }

        private void OpenEditModal()
        {
            editNameInput.text = currentNameText.text;
            pendingIcon = currentIconText.text;
            editModal.SetActive(true);
        }

        private void SaveProfile()
        {
            var newName = editNameInput.text.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning("名前を入力してください。");
                return;
            }

            currentNameText.text = newName;
            currentIconText.text = pendingIcon;
            editModal.SetActive(false);
        }
    }
}
