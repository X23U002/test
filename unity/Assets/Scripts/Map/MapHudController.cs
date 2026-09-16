using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 地図画面のHUD部分。Web版 map.html + Script.js のうち
    /// ・現在地ボタン (moveToCurrentLocation)
    /// ・2D/3D切替ボタン (toggleView)
    /// ・ハンバーガーメニュー (toggleMenu)
    /// ・地図タップ時の座標表示 + 仮マーカー設置
    /// を担当する。
    /// </summary>
    public class MapHudController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour mapViewBehaviour; // IMapViewを実装したコンポーネントをInspectorで指定
        private IMapView MapView => (IMapView)mapViewBehaviour;

        [Header("現在地")]
        [SerializeField] private Button locationButton;

        [Header("2D/3D切替")]
        [SerializeField] private Button toggle3DButton;
        [SerializeField] private Text toggle3DButtonLabel;
        private bool is3D;

        [Header("ハンバーガーメニュー")]
        [SerializeField] private Button hamburgerButton;
        [SerializeField] private GameObject sideMenuPanel;
        [SerializeField] private Button closeMenuButton;
        [SerializeField] private Button openInformationButton;
        [SerializeField] private Button openRegisterButton;
        [SerializeField] private Button openGuideButton;

        [Header("座標表示")]
        [SerializeField] private Text latitudeText;
        [SerializeField] private Text longitudeText;

        [Header("仮マーカー")]
        [SerializeField] private Button clearTempMarkerButton;

        private IMapMarkerHandle tempMarker;

        private void Awake()
        {
            locationButton.onClick.AddListener(() => GeolocationService.Instance.TriggerLocate());

            toggle3DButton.onClick.AddListener(ToggleView);

            hamburgerButton.onClick.AddListener(() => sideMenuPanel.SetActive(!sideMenuPanel.activeSelf));
            closeMenuButton.onClick.AddListener(() => sideMenuPanel.SetActive(false));

            openInformationButton.onClick.AddListener(() => Pilgrimage.UI.AppShellController.Instance.ShowInformation());
            openRegisterButton.onClick.AddListener(() => Pilgrimage.UI.AppShellController.Instance.ShowRegisterSpot());
            openGuideButton.onClick.AddListener(() => Debug.Log("操作ガイド画面は今後実装予定です"));

            clearTempMarkerButton.onClick.AddListener(ClearTempMarker);
            clearTempMarkerButton.gameObject.SetActive(false);

            MapView.OnMapTapped += HandleMapTapped;
            MapView.OnMapLongPressed += HandleMapTapped;

            GeolocationService.Instance.OnPositionUpdated += HandleLocationUpdated;
            GeolocationService.Instance.OnError += message => Debug.LogWarning($"現在地の取得に失敗: {message}");
        }

        private void ToggleView()
        {
            is3D = !is3D;

            if (is3D)
            {
                MapView.SetPitchAndBearing(70f, -20f);
                toggle3DButtonLabel.text = "🔄 2D";
            }
            else
            {
                MapView.SetPitchAndBearing(0f, 0f);
                toggle3DButtonLabel.text = "🔄 3D";
            }
        }

        private void HandleLocationUpdated(GeoPosition position)
        {
            MapView.FlyTo(position.Latitude, position.Longitude, Mathf.Max(MapView.Zoom, 15f));
        }

        private void HandleMapTapped(double latitude, double longitude)
        {
            latitudeText.text = latitude.ToString("F6");
            longitudeText.text = longitude.ToString("F6");

            ClearTempMarker();

            tempMarker = MapView.AddMarker(latitude, longitude, "選択した場所", Color.red);
            clearTempMarkerButton.gameObject.SetActive(true);
        }

        private void ClearTempMarker()
        {
            if (tempMarker != null)
            {
                tempMarker.Remove();
                tempMarker = null;
            }

            clearTempMarkerButton.gameObject.SetActive(false);
        }
    }
}
