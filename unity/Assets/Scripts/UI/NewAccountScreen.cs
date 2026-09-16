using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>新規登録入力画面。Web版 newaccount.js を移植。</summary>
    public class NewAccountScreen : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private InputField emailInput;
        [SerializeField] private Button backButton;
        [SerializeField] private Button goToConfirmButton;
        [SerializeField] private ConfirmScreen confirmScreen;

        public string Username => usernameInput.text;
        public string Email => emailInput.text;

        private void Awake()
        {
            backButton.onClick.AddListener(() => AppShellController.Instance.ShowLogin());

            goToConfirmButton.onClick.AddListener(() =>
            {
                confirmScreen.Populate(usernameInput.text, emailInput.text);
                AppShellController.Instance.ShowConfirm();
            });
        }
    }
}
