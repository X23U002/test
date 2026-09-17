using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>
    /// ログイン画面 (UI Toolkit版)。Login.uxml / Login.uss と対になる。
    ///
    /// uGUI版 (Scripts/UI/LoginScreen.cs) との違いは「UI部品の取り方」だけで、
    /// 処理内容は同じ。
    ///   uGUI        : [SerializeField] Button loginButton; → Inspectorで1つずつ配線
    ///   UI Toolkit  : root.Q&lt;Button&gt;("login-button")   → UXMLのname属性で取得
    ///
    /// 使い方:
    ///   1. 空のGameObjectに UIDocument コンポーネントを追加
    ///   2. UIDocumentの Source Asset に Login.uxml を設定
    ///   3. Panel Settings に PanelSettings アセットを設定
    ///      (Project右クリック > Create > UI Toolkit > Panel Settings Asset)
    ///   4. このスクリプトを同じGameObjectに追加
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LoginScreenView : MonoBehaviour
    {
        private TextField userIdField;
        private TextField passwordField;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            userIdField = root.Q<TextField>("userid-field");
            passwordField = root.Q<TextField>("password-field");

            // Web版 login.html の onclick="goToMap()" に相当
            root.Q<Button>("login-button").clicked += OnLoginClicked;
            root.Q<Button>("newaccount-button").clicked += () => AppShellController.Instance.ShowNewAccount();
            root.Q<Button>("forgot-password-button").clicked += () => AppShellController.Instance.ShowForgotPassword();
        }

        private void OnLoginClicked()
        {
            // Web版と同じくプロトタイプ段階のため入力チェックはしない。
            // 実装する場合はここで Firebase Authentication を呼ぶ。
            AppShellController.Instance.ShowMap();
        }
    }
}
