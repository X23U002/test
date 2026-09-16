using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>パスワード再発行画面。Web版 forgot_password.js を移植。</summary>
    public class ForgotPasswordScreen : MonoBehaviour
    {
        [SerializeField] private InputField usernameInput;
        [SerializeField] private InputField emailInput;
        [SerializeField] private Button backButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private GameObject completionModal;
        [SerializeField] private Button closeModalButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() => AppShellController.Instance.ShowLogin());
            submitButton.onClick.AddListener(() => completionModal.SetActive(true));

            closeModalButton.onClick.AddListener(() =>
            {
                completionModal.SetActive(false);
                AppShellController.Instance.ShowLogin();
            });

            completionModal.SetActive(false);
        }
    }
}
