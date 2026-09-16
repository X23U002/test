using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>
    /// ログイン画面。Web版 login.js を移植。
    /// プロトタイプ段階のため、ID/パスワードの検証は行わず
    /// ボタン押下でそのままマップ画面に遷移する (Web版と同じ)。
    /// 実際の認証を導入する場合は Firebase Authentication の
    /// メール/パスワードサインインに置き換えること。
    /// </summary>
    public class LoginScreen : MonoBehaviour
    {
        [SerializeField] private InputField userIdInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button newAccountLinkButton;
        [SerializeField] private Button forgotPasswordLinkButton;

        private void Awake()
        {
            loginButton.onClick.AddListener(() => AppShellController.Instance.ShowMap());
            newAccountLinkButton.onClick.AddListener(() => AppShellController.Instance.ShowNewAccount());
            forgotPasswordLinkButton.onClick.AddListener(() => AppShellController.Instance.ShowForgotPassword());
        }
    }
}
