using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>
    /// 履歴・ルート案内画面。Web版 history.js を移植。
    /// Web版は現在のところタイムライン・履歴リストとも静的なモックデータで、
    /// 実データ連携はまだ行われていない。Unity版でも同様に、
    /// 表示専用のプレースホルダーとして扱う。
    /// </summary>
    public class HistoryScreen : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button startRouteButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() => AppShellController.Instance.ShowMyPage());

            startRouteButton.onClick.AddListener(() =>
            {
                Debug.Log("ルート案内を開始します！マップ画面に切り替わります。");
                AppShellController.Instance.ShowMap();
            });
        }
    }
}
