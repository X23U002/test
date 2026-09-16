using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>
    /// 聖地登録画面。Web版 register.js を移植。
    /// 現状のWeb版は入力チェックのみで実際の送信処理はまだ無いプロトタイプ。
    /// 本実装では、運営が確認するまでの申請フローとして
    /// Firestoreの "spotRequests" 等のコレクションへ書き込む処理を
    /// completeRegisterAsync 内に追加する想定。
    /// </summary>
    public class RegisterSpotScreen : MonoBehaviour
    {
        [SerializeField] private GameObject inputPage;
        [SerializeField] private GameObject completePage;

        [SerializeField] private InputField animeNameInput;
        [SerializeField] private InputField addressInput;
        [SerializeField] private InputField spotTextInput;

        [SerializeField] private Button closeButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button backToMapButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => Pilgrimage.UI.AppShellController.Instance.ShowMap());
            backToMapButton.onClick.AddListener(() => Pilgrimage.UI.AppShellController.Instance.ShowMap());
            registerButton.onClick.AddListener(CompleteRegister);
        }

        private void OnEnable()
        {
            inputPage.SetActive(true);
            completePage.SetActive(false);
        }

        private void CompleteRegister()
        {
            var animeName = animeNameInput.text.Trim();
            var address = addressInput.text.Trim();
            var spotText = spotTextInput.text.Trim();

            if (string.IsNullOrEmpty(animeName))
            {
                Debug.LogWarning("作品名を入力してください。");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogWarning("住所を入力してください。");
                return;
            }

            if (string.IsNullOrEmpty(spotText))
            {
                Debug.LogWarning("スポット説明を入力してください。");
                return;
            }

            // TODO: FirestoreのspotRequestsコレクション等へ申請データを保存する

            inputPage.SetActive(false);
            completePage.SetActive(true);
        }
    }
}
