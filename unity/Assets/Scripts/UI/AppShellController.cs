using UnityEngine;

namespace Pilgrimage.UI
{
    /// <summary>
    /// 画面遷移の管理役。Web版は login.html / map.html / mypage.html ...
    /// のように画面ごとに別HTMLへ window.location.href で遷移していたが、
    /// Unity版は1シーンの中でパネルを切り替える構成にしている
    /// (モバイルアプリとしてシーンロードのコストを避けるため)。
    ///
    /// シーンに1つ配置し、各パネルのGameObjectをInspectorで登録すること。
    /// </summary>
    public class AppShellController : MonoBehaviour
    {
        public static AppShellController Instance { get; private set; }

        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject newAccountPanel;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private GameObject forgotPasswordPanel;
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject myPagePanel;
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject stampPanel;
        [SerializeField] private GameObject registerSpotPanel;
        [SerializeField] private GameObject informationPanel;

        private GameObject[] AllPanels => new[]
        {
            loginPanel, newAccountPanel, confirmPanel, forgotPasswordPanel,
            mapPanel, myPagePanel, historyPanel, stampPanel,
            registerSpotPanel, informationPanel
        };

        private void Awake()
        {
            Instance = this;
            ShowOnly(loginPanel);
        }

        private void ShowOnly(GameObject panelToShow)
        {
            foreach (var panel in AllPanels)
            {
                if (panel != null)
                {
                    panel.SetActive(panel == panelToShow);
                }
            }
        }

        public void ShowLogin() => ShowOnly(loginPanel);
        public void ShowNewAccount() => ShowOnly(newAccountPanel);
        public void ShowConfirm() => ShowOnly(confirmPanel);
        public void ShowForgotPassword() => ShowOnly(forgotPasswordPanel);
        public void ShowMap() => ShowOnly(mapPanel);
        public void ShowMyPage() => ShowOnly(myPagePanel);
        public void ShowHistory() => ShowOnly(historyPanel);
        public void ShowStamp() => ShowOnly(stampPanel);
        public void ShowRegisterSpot() => ShowOnly(registerSpotPanel);
        public void ShowInformation() => ShowOnly(informationPanel);
    }
}
