using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 地図画面全体の統括役。Web版 Script.js のトップレベル処理
    /// (Firestore読み込み → マーカー描画 → 検索初期化 → 現在地取得) を
    /// Unity側でも同じ順序で行う。
    /// </summary>
    public class PilgrimageMapController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour mapViewBehaviour;
        private IMapView MapView => (IMapView)mapViewBehaviour;

        [SerializeField] private SpotPopupPanel popupPanel;
        [SerializeField] private AnimeFilterPanel filterPanel;
        [SerializeField] private RoutePanelController routePanel;
        [SerializeField] private InputField searchInput;
        [SerializeField] private Button filterOpenButton;

        private readonly SpotRepository spotRepository = new SpotRepository();
        private readonly LocalStampStore localStampStore = new LocalStampStore();
        private StampService stampService;
        private SpotMarkerFactory markerFactory;

        private List<SpotData> allSpots = new List<SpotData>();
        private List<SpotData> currentSpots = new List<SpotData>();

        private async void Start()
        {
            stampService = new StampService(localStampStore);
            markerFactory = new SpotMarkerFactory(MapView);

            markerFactory.OnSpotMarkerClicked += ShowSpotPopup;
            markerFactory.OnClusterMarkerClicked += HandleClusterClicked;

            popupPanel.OnAddToRouteRequested += routePanel.AddSpot;

            MapView.OnZoomChanged += () => markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);
            stampService.OnStampStateChanged += popupPanel.RefreshStampUI;

            searchInput.onEndEdit.AddListener(HandleSearchSubmitted);
            filterOpenButton.onClick.AddListener(() => filterPanel.Open(GetAvailableTitles()));
            filterPanel.OnApply += HandleFilterApplied;
            filterPanel.OnReset += ResetFilter;

            GeolocationService.Instance.OnPositionUpdated += stampService.UpdatePosition;

            await LoadSpotsAsync();

            // Web版と同じく、地図の読み込み完了後に現在地取得を試みる
            GeolocationService.Instance.TriggerLocate();

            _ = stampService.InitializeAsync(id => allSpots.FirstOrDefault(s => s.Id == id));
        }

        private async System.Threading.Tasks.Task LoadSpotsAsync()
        {
            try
            {
                allSpots = await spotRepository.LoadSpotsAsync();
                currentSpots = allSpots;
                markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"データの読み込みに失敗: {e}");
            }
        }

        private IEnumerable<string> GetAvailableTitles()
        {
            return allSpots.Select(s => s.TitleName).Distinct();
        }

        private void HandleSearchSubmitted(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                currentSpots = allSpots;
                markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);
                return;
            }

            var matches = SearchController.Search(query, allSpots);

            if (matches.Count == 0)
            {
                Debug.LogWarning("該当するスポットが見つかりませんでした。");
                return;
            }

            currentSpots = matches;
            markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);

            if (matches.Count == 1)
            {
                MapView.FlyTo(matches[0].Latitude, matches[0].Longitude, 16f);
                ShowSpotPopup(matches[0]);
            }
        }

        private void HandleFilterApplied(List<string> selectedTitles, string keyword)
        {
            var normalizedKeyword = SearchController.NormalizeText(keyword);

            if (string.IsNullOrEmpty(normalizedKeyword) && selectedTitles.Count == 0)
            {
                ResetFilter();
                return;
            }

            var filtered = allSpots.Where(spot =>
                (!string.IsNullOrEmpty(normalizedKeyword) && SearchController.NormalizeText(spot.TitleName).Contains(normalizedKeyword)) ||
                selectedTitles.Contains(spot.TitleName)
            ).ToList();

            if (filtered.Count == 0)
            {
                Debug.LogWarning("入力された作品名のスポットは見つかりませんでした。");
                currentSpots = allSpots;
            }
            else
            {
                currentSpots = filtered;
            }

            markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);
        }

        private void ResetFilter()
        {
            currentSpots = allSpots;
            markerFactory.Redraw(currentSpots, stampService.IsSpotStamped);
        }

        private void HandleClusterClicked(List<SpotData> members)
        {
            // Web版はホバーポップアップで一覧を出していたが、Unity版では
            // 該当エリアへズームインして個別マーカー表示に切り替える簡易実装にしている。
            var representative = members[0];
            MapView.FlyTo(representative.Latitude, representative.Longitude, 12f);
        }

        private void ShowSpotPopup(SpotData spot)
        {
            popupPanel.Show(spot, stampService, markerFactory.GetColorForTitle(spot.TitleName));
        }
    }
}
