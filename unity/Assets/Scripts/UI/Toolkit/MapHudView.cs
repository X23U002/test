using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using Pilgrimage.Map;
using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>
    /// 地図画面のHUD (UI Toolkit版)。MapHud.uxml / MapHud.uss と対になる。
    ///
    /// uGUI版では MapHudController / AnimeFilterPanel / RoutePanelController /
    /// SpotPopupPanel の4つに分かれていたものを、UXMLが1ファイルなので
    /// このクラスにまとめている。
    ///
    /// 地図の描画自体は行わない。地図は IMapView (FlatMapView等) の担当で、
    /// ここはその上に重ねるボタンやパネルだけを扱う。
    /// スポットの読み込みや絞り込みの実処理は PilgrimageMapController 側に置き、
    /// ここからはイベントで通知する。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MapHudView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour mapViewBehaviour; // IMapViewを実装したコンポーネント
        private IMapView MapView => (IMapView)mapViewBehaviour;

        [SerializeField] private Texture2D placeholderImage;

        /// <summary>検索バーで確定したときに発火 (Web版 geocoder の result 相当)。</summary>
        public event Action<string> OnSearchSubmitted;

        /// <summary>絞り込みの「決定」。選択された作品名と入力キーワードを渡す。</summary>
        public event Action<List<string>, string> OnFilterApplied;

        /// <summary>絞り込みの「リセット」。</summary>
        public event Action OnFilterReset;

        private VisualElement sideMenu;
        private VisualElement filterModal;
        private VisualElement animeList;
        private TextField animeSearchField;
        private VisualElement routePanel;
        private VisualElement routeList;
        private VisualElement spotPopup;

        private Label popupTitle;
        private Label popupSpotName;
        private Label popupScene;
        private VisualElement popupImage;
        private Label stampMessage;
        private Button stampButton;
        private Button toggleViewButton;

        private readonly List<Toggle> animeToggles = new List<Toggle>();
        private readonly List<SpotData> routeSpots = new List<SpotData>();

        private StampService stampService;
        private SpotData currentSpot;
        private Coroutine imageDownload;
        private bool is3D;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            sideMenu = root.Q<VisualElement>("side-menu");
            filterModal = root.Q<VisualElement>("filter-modal");
            animeList = root.Q<VisualElement>("anime-list");
            animeSearchField = root.Q<TextField>("anime-search-field");
            routePanel = root.Q<VisualElement>("route-panel");
            routeList = root.Q<VisualElement>("route-list");
            spotPopup = root.Q<VisualElement>("spot-popup");

            popupTitle = root.Q<Label>("popup-title");
            popupSpotName = root.Q<Label>("popup-spot-name");
            popupScene = root.Q<Label>("popup-scene");
            popupImage = root.Q<VisualElement>("popup-image");
            stampMessage = root.Q<Label>("stamp-message");
            stampButton = root.Q<Button>("stamp-button");
            toggleViewButton = root.Q<Button>("toggle-view-button");

            filterModal.style.display = DisplayStyle.None;
            routePanel.style.display = DisplayStyle.None;
            spotPopup.style.display = DisplayStyle.None;

            // --- 上部のボタン ---
            root.Q<Button>("hamburger-button").clicked += ToggleSideMenu;
            root.Q<Button>("close-menu-button").clicked += ToggleSideMenu;
            root.Q<Button>("profile-button").clicked += () => AppShellController.Instance.ShowMyPage();

            root.Q<Button>("menu-information-button").clicked += () => AppShellController.Instance.ShowInformation();
            root.Q<Button>("menu-register-button").clicked += () => AppShellController.Instance.ShowRegisterSpot();
            root.Q<Button>("menu-guide-button").clicked += () => Debug.Log("操作ガイド画面は今後実装予定です");

            // 検索バーはEnter確定で検索する
            root.Q<TextField>("search-field").RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnSearchSubmitted?.Invoke(((TextField)evt.currentTarget).value);
                }
            });

            // --- 右下のボタン ---
            root.Q<Button>("location-button").clicked += () => GeolocationService.Instance.TriggerLocate();
            toggleViewButton.clicked += ToggleView;

            // --- 絞り込み ---
            root.Q<Button>("filter-open-button").clicked += () => filterModal.style.display = DisplayStyle.Flex;
            root.Q<Button>("filter-close-button").clicked += () => filterModal.style.display = DisplayStyle.None;
            animeSearchField.RegisterValueChangedCallback(evt => FilterAnimeList(evt.newValue));

            root.Q<Button>("filter-apply-button").clicked += () =>
            {
                var selected = animeToggles.Where(t => t.value).Select(t => t.label).ToList();
                OnFilterApplied?.Invoke(selected, animeSearchField.value.Trim());
                filterModal.style.display = DisplayStyle.None;
            };

            root.Q<Button>("filter-reset-button").clicked += () =>
            {
                animeSearchField.value = string.Empty;
                foreach (var toggle in animeToggles)
                {
                    toggle.value = false;
                }

                OnFilterReset?.Invoke();
            };

            // --- 経路パネル ---
            root.Q<Button>("clear-route-button").clicked += ClearRoute;
            root.Q<Button>("open-google-route-button").clicked += OpenGoogleMapsRoute;

            // --- スポットポップアップ ---
            root.Q<Button>("popup-close-button").clicked += HideSpotPopup;
            root.Q<Button>("popup-home-button").clicked += OpenSpotHomePage;
            root.Q<Button>("add-route-button").clicked += () => AddSpotToRoute(currentSpot);
            stampButton.clicked += OnStampButtonClicked;
        }

        // =====================================================
        // サイドメニュー / 2D・3D切替
        // =====================================================
        private void ToggleSideMenu()
        {
            sideMenu.ToggleInClassList("side-menu--open");
        }

        private void ToggleView()
        {
            is3D = !is3D;

            MapView.SetPitchAndBearing(is3D ? 70f : 0f, is3D ? -20f : 0f);
            toggleViewButton.text = is3D ? "🔄 2D" : "🔄 3D";
            toggleViewButton.EnableInClassList("toggle-view-button--3d", is3D);
        }

        // =====================================================
        // 絞り込みリスト
        // =====================================================
        public void PopulateAnimeList(IEnumerable<string> titles)
        {
            animeList.Clear();
            animeToggles.Clear();

            foreach (var title in titles.Where(t => !string.IsNullOrEmpty(t)))
            {
                var toggle = new Toggle(title);
                toggle.AddToClassList("anime-list-item");
                animeList.Add(toggle);
                animeToggles.Add(toggle);
            }
        }

        private void FilterAnimeList(string keyword)
        {
            var normalized = SearchController.NormalizeText(keyword);

            foreach (var toggle in animeToggles)
            {
                var visible = string.IsNullOrEmpty(normalized) ||
                              SearchController.NormalizeText(toggle.label).Contains(normalized);

                toggle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // =====================================================
        // 経路パネル (Web版 selectedSpots まわり)
        // =====================================================
        public void AddSpotToRoute(SpotData spot)
        {
            if (spot == null)
            {
                return;
            }

            if (routeSpots.Any(s => s.Id == spot.Id))
            {
                Debug.LogWarning("この場所はすでに追加されています");
                return;
            }

            routeSpots.Add(spot);
            routePanel.style.display = DisplayStyle.Flex;
            RefreshRouteList();
        }

        private void ClearRoute()
        {
            routeSpots.Clear();
            RefreshRouteList();
            routePanel.style.display = DisplayStyle.None;
        }

        private void RefreshRouteList()
        {
            routeList.Clear();

            for (var i = 0; i < routeSpots.Count; i++)
            {
                var spot = routeSpots[i];

                var row = new VisualElement();
                row.AddToClassList("route-item");

                var label = new Label($"{i + 1}. {spot.SpotName}");
                label.AddToClassList("route-item__label");

                var removeButton = new Button(() =>
                {
                    routeSpots.RemoveAll(s => s.Id == spot.Id);
                    RefreshRouteList();

                    if (routeSpots.Count == 0)
                    {
                        routePanel.style.display = DisplayStyle.None;
                    }
                })
                {
                    text = "×"
                };
                removeButton.AddToClassList("route-item__remove");

                row.Add(label);
                row.Add(removeButton);
                routeList.Add(row);
            }
        }

        private void OpenGoogleMapsRoute()
        {
            if (routeSpots.Count == 0)
            {
                Debug.LogWarning("行きたい場所を追加してください");
                return;
            }

            var locations = routeSpots
                .Select(s => $"{s.Latitude:F6},{s.Longitude:F6}")
                .ToList();

            var destination = locations[^1];
            var waypoints = string.Join("|", locations.Take(locations.Count - 1));

            var url = "https://www.google.com/maps/dir/?api=1" +
                      $"&destination={UnityWebRequest.EscapeURL(destination)}";

            if (!string.IsNullOrEmpty(waypoints))
            {
                url += $"&waypoints={UnityWebRequest.EscapeURL(waypoints)}";
            }

            url += "&travelmode=walking";

            Application.OpenURL(url);
        }

        // =====================================================
        // スポットポップアップ
        // =====================================================
        public void ShowSpotPopup(SpotData spot, StampService stampService, Color titleColor)
        {
            currentSpot = spot;
            this.stampService = stampService;

            spotPopup.style.display = DisplayStyle.Flex;

            popupTitle.text = spot.TitleName;
            popupTitle.style.backgroundColor = titleColor;
            popupSpotName.text = spot.SpotName;
            popupScene.text = spot.SpotInfo;

            RefreshStampUI();
            LoadSpotImage(spot.ImageUrl);
        }

        public void HideSpotPopup()
        {
            currentSpot = null;
            spotPopup.style.display = DisplayStyle.None;

            if (imageDownload != null)
            {
                StopCoroutine(imageDownload);
                imageDownload = null;
            }
        }

        public void RefreshStampUI()
        {
            if (currentSpot == null || stampService == null)
            {
                return;
            }

            var state = stampService.GetStampState(currentSpot);
            stampButton.text = state.Label;
            stampMessage.text = state.Message;
            stampButton.SetEnabled(state.IsCollectible);
        }

        private async void OnStampButtonClicked()
        {
            if (currentSpot == null || stampService == null)
            {
                return;
            }

            var (_, message) = await stampService.CollectStampAsync(currentSpot);
            Debug.Log($"[スタンプ取得] {message}");
            RefreshStampUI();
        }

        private void OpenSpotHomePage()
        {
            if (currentSpot != null && !string.IsNullOrEmpty(currentSpot.TitleUrl))
            {
                Application.OpenURL(currentSpot.TitleUrl);
            }
        }

        private void LoadSpotImage(string imageUrl)
        {
            if (imageDownload != null)
            {
                StopCoroutine(imageDownload);
            }

            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "not_image")
            {
                popupImage.style.backgroundImage = new StyleBackground(placeholderImage);
                return;
            }

            imageDownload = StartCoroutine(DownloadImageRoutine(imageUrl));
        }

        private IEnumerator DownloadImageRoutine(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var texture = DownloadHandlerTexture.GetContent(request);
                popupImage.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                Debug.LogWarning($"スポット画像の取得に失敗しました ({url}): {request.error}");
                popupImage.style.backgroundImage = new StyleBackground(placeholderImage);
            }
        }
    }
}
