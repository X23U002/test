using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>登録確認画面。Web版 confirm.js を移植。</summary>
    public class ConfirmScreen : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text emailText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button registerButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() => AppShellController.Instance.ShowNewAccount());

            registerButton.onClick.AddListener(() =>
            {
                Debug.Log("登録が完了しました！地図画面へ移動します。");
                AppShellController.Instance.ShowMap();
            });
        }

        public void Populate(string userName, string email)
        {
            nameText.text = userName;
            emailText.text = email;
        }
    }
}
