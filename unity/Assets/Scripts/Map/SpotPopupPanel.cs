using System.Collections;
using Pilgrimage.Data;
using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// スポットマーカーをタップしたときのポップアップ。
    /// Web版 Script.js の popupHTML (タイトル/スポット名/シーン説明/画像/
    /// スタンプ取得ボタン/ルート追加ボタン/外部リンク) を移植したもの。
    /// </summary>
    public class SpotPopupPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleBadgeText;
        [SerializeField] private Image titleBadgeBackground;
        [SerializeField] private Text spotNameText;
        [SerializeField] private Text sceneText;
        [SerializeField] private RawImage photoImage;
        [SerializeField] private Text stampButtonLabel;
        [SerializeField] private Text stampMessageText;
        [SerializeField] private Button stampButton;
        [SerializeField] private Button addToRouteButton;
        [SerializeField] private Button openLinkButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private Texture2D placeholderImage;

        private StampService stampService;
        private SpotData currentSpot;
        private Coroutine imageDownloadCoroutine;

        public event System.Action<SpotData> OnAddToRouteRequested;

        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            addToRouteButton.onClick.AddListener(() => OnAddToRouteRequested?.Invoke(currentSpot));
            openLinkButton.onClick.AddListener(OpenExternalLink);
            stampButton.onClick.AddListener(OnStampButtonClicked);

            Hide();
        }

        public void Show(SpotData spot, StampService stampService, Color titleColor)
        {
            currentSpot = spot;
            this.stampService = stampService;

            root.SetActive(true);

            titleBadgeText.text = spot.TitleName;
            titleBadgeBackground.color = titleColor;
            spotNameText.text = spot.SpotName;
            sceneText.text = spot.SpotInfo;

            openLinkButton.gameObject.SetActive(!string.IsNullOrEmpty(spot.TitleUrl));

            RefreshStampUI();
            LoadImage(spot.ImageUrl);
        }

        public void Hide()
        {
            currentSpot = null;
            root.SetActive(false);

            if (imageDownloadCoroutine != null)
            {
                StopCoroutine(imageDownloadCoroutine);
                imageDownloadCoroutine = null;
            }
        }

        public void RefreshStampUI()
        {
            if (currentSpot == null || stampService == null)
            {
                return;
            }

            var state = stampService.GetStampState(currentSpot);
            stampButtonLabel.text = state.Label;
            stampMessageText.text = state.Message;
            stampButton.interactable = state.IsCollectible;
        }

        private async void OnStampButtonClicked()
        {
            if (currentSpot == null || stampService == null)
            {
                return;
            }

            var (success, message) = await stampService.CollectStampAsync(currentSpot);
            Debug.Log(message);
            RefreshStampUI();

            // Web版はalert()で結果を表示していた。実プロジェクトではトースト等のUIに置き換える。
            if (success)
            {
                Debug.Log($"[スタンプ取得] {message}");
            }
        }

        private void OpenExternalLink()
        {
            if (currentSpot != null && !string.IsNullOrEmpty(currentSpot.TitleUrl))
            {
                Application.OpenURL(currentSpot.TitleUrl);
            }
        }

        private void LoadImage(string imageUrl)
        {
            if (imageDownloadCoroutine != null)
            {
                StopCoroutine(imageDownloadCoroutine);
            }

            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "not_image")
            {
                photoImage.texture = placeholderImage;
                return;
            }

            imageDownloadCoroutine = StartCoroutine(DownloadImageRoutine(imageUrl));
        }

        private IEnumerator DownloadImageRoutine(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                photoImage.texture = DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                Debug.LogWarning($"スポット画像の取得に失敗しました ({url}): {request.error}");
                photoImage.texture = placeholderImage;
            }
        }
    }
}
