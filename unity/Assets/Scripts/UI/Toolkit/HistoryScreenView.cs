using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>
    /// 履歴・ルート案内画面 (UI Toolkit版)。uGUI版 HistoryScreen.cs と同じ処理。
    /// Web版が静的モックのままなので、表示内容はUXMLに直接書いている。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HistoryScreenView : MonoBehaviour
    {
        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowMyPage();

            root.Q<Button>("start-route-button").clicked += () =>
            {
                Debug.Log("ルート案内を開始します！マップ画面に切り替わります。");
                AppShellController.Instance.ShowMap();
            };
        }
    }
}
