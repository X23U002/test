using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 追加パッケージなしで動く簡易2D地図。IMapViewの既定実装。
    ///
    /// 背景画像 (mapBackgroundImage) が表す緯度経度の範囲
    /// (northWestLatitude/Longitude 〜 southEastLatitude/Longitude) を
    /// 指定しておくことで、緯度経度⇔画面座標を単純な正距円筒図法で変換する。
    /// 本番で実際の地図タイルを使いたくなったら、このクラスと同じ
    /// IMapViewを実装するアダプタ (例: MapboxMapView) に差し替える。
    /// </summary>
    public class FlatMapView : MonoBehaviour, IMapView, IDragHandler, IBeginDragHandler, IPointerClickHandler, IScrollHandler
    {
        [Header("参照")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform mapContent; // 背景画像とマーカーの親。ズームはこのlocalScaleで表現する
        [SerializeField] private GameObject markerPrefab;

        [Header("背景画像が表す範囲")]
        [SerializeField] private double northWestLatitude = 35.9;
        [SerializeField] private double northWestLongitude = 139.4;
        [SerializeField] private double southEastLatitude = 35.4;
        [SerializeField] private double southEastLongitude = 140.1;

        [Header("初期表示 / ズーム")]
        [SerializeField] private double initialCenterLatitude = 35.6812;
        [SerializeField] private double initialCenterLongitude = 139.7671;
        [SerializeField] private float initialZoom = 12f;
        [SerializeField] private float minZoom = 6f;
        [SerializeField] private float maxZoom = 18f;

        [SerializeField] private float longPressSeconds = 0.6f;
        [SerializeField] private float longPressMoveToleranceScreenPx = 12f;

        public double CenterLatitude { get; private set; }
        public double CenterLongitude { get; private set; }
        public float Zoom { get; private set; }
        public float Pitch { get; private set; }
        public float Bearing { get; private set; }

        public event Action OnZoomChanged;
        public event Action<double, double> OnMapTapped;
        public event Action<double, double> OnMapLongPressed;

        private readonly List<FlatMapMarkerHandle> markers = new List<FlatMapMarkerHandle>();

        private Vector2 pointerDownScreenPos;
        private float pointerDownTime;
        private bool longPressFired;
        private bool pointerIsDown;

        private void Awake()
        {
            CenterLatitude = initialCenterLatitude;
            CenterLongitude = initialCenterLongitude;
            Zoom = Mathf.Clamp(initialZoom, minZoom, maxZoom);
            ApplyZoomScale();
            CenterContentOn(CenterLatitude, CenterLongitude);
        }

        private void Update()
        {
            HandlePinchZoom();
            HandleLongPress();
        }

        // =====================================================
        // 緯度経度 ⇔ mapContentローカル座標 (正距円筒図法)
        // =====================================================
        private Vector2 LatLngToLocalPoint(double latitude, double longitude)
        {
            var rect = mapContent.rect;

            var u = (float)((longitude - northWestLongitude) / (southEastLongitude - northWestLongitude));
            var v = (float)((latitude - northWestLatitude) / (southEastLatitude - northWestLatitude));

            var x = (u - 0.5f) * rect.width;
            var y = (v - 0.5f) * rect.height;

            return new Vector2(x, y);
        }

        private (double latitude, double longitude) LocalPointToLatLng(Vector2 localPoint)
        {
            var rect = mapContent.rect;

            var u = localPoint.x / rect.width + 0.5f;
            var v = localPoint.y / rect.height + 0.5f;

            var longitude = northWestLongitude + u * (southEastLongitude - northWestLongitude);
            var latitude = northWestLatitude + v * (southEastLatitude - northWestLatitude);

            return (latitude, longitude);
        }

        private void CenterContentOn(double latitude, double longitude)
        {
            var point = LatLngToLocalPoint(latitude, longitude);
            mapContent.anchoredPosition = -point * mapContent.localScale.x;
        }

        private void ApplyZoomScale()
        {
            // ズーム1段階ごとに2倍のスケール、というありがちな近似
            var scale = Mathf.Pow(2f, Zoom - minZoom) / Mathf.Pow(2f, maxZoom - minZoom) * 8f + 1f;
            mapContent.localScale = new Vector3(scale, scale, 1f);
        }

        // =====================================================
        // IMapView実装
        // =====================================================
        public void SetCenter(double latitude, double longitude)
        {
            CenterLatitude = latitude;
            CenterLongitude = longitude;
            CenterContentOn(latitude, longitude);
        }

        public void FlyTo(double latitude, double longitude, float zoom, float durationSeconds = 1.2f)
        {
            // 簡易実装ではアニメーションせず即座に移動する
            SetZoom(zoom);
            SetCenter(latitude, longitude);
        }

        public void SetZoom(float zoom)
        {
            Zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            ApplyZoomScale();
            CenterContentOn(CenterLatitude, CenterLongitude);
            OnZoomChanged?.Invoke();
        }

        public void SetPitchAndBearing(float pitch, float bearing, float durationSeconds = 1.5f)
        {
            // 2D地図なので実際の傾き表現はできないが、状態としては保持する。
            // 3D表示が必要な場合はMapboxMapView等、真の3D地図アダプタに差し替えること。
            Pitch = pitch;
            Bearing = bearing;
        }

        public IMapMarkerHandle AddMarker(double latitude, double longitude, string label, Color color)
        {
            var go = Instantiate(markerPrefab, mapContent);
            var handle = new FlatMapMarkerHandle(go, latitude, longitude);
            handle.SetLabel(label);
            handle.SetColor(color);

            var localPoint = LatLngToLocalPoint(latitude, longitude);
            go.GetComponent<RectTransform>().anchoredPosition = localPoint;

            markers.Add(handle);
            return handle;
        }

        public void RemoveMarker(IMapMarkerHandle handle)
        {
            if (handle is FlatMapMarkerHandle flatHandle)
            {
                markers.Remove(flatHandle);
                flatHandle.DestroyGameObject();
            }
        }

        public void ClearMarkers()
        {
            foreach (var marker in markers)
            {
                marker.DestroyGameObject();
            }

            markers.Clear();
        }

        // =====================================================
        // 入力操作: パン / タップ
        // =====================================================
        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            mapContent.anchoredPosition += eventData.delta;
            longPressFired = true; // ドラッグしたら長押し判定はキャンセル (Web版のtouchmoveと同じ)
            var (lat, lng) = LocalPointToLatLng(mapContent.anchoredPosition / -mapContent.localScale.x);
            CenterLatitude = lat;
            CenterLongitude = lng;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapContent, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                var (lat, lng) = LocalPointToLatLng(localPoint);
                OnMapTapped?.Invoke(lat, lng);
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            SetZoom(Zoom + eventData.scrollDelta.y * 0.5f);
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount != 2)
            {
                return;
            }

            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            if (t0.phase != TouchPhase.Moved && t1.phase != TouchPhase.Moved)
            {
                return;
            }

            var prevDistance = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
            var currentDistance = (t0.position - t1.position).magnitude;
            var delta = (currentDistance - prevDistance) * 0.02f;

            if (Mathf.Abs(delta) > 0.001f)
            {
                SetZoom(Zoom + delta);
            }
        }

        private void HandleLongPress()
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                pointerDownScreenPos = Input.mousePosition;
                pointerDownTime = Time.time;
                longPressFired = false;
                pointerIsDown = true;
            }

            if (!pointerIsDown)
            {
                return;
            }

            var currentPos = (Vector2)Input.mousePosition;
            if (Vector2.Distance(currentPos, pointerDownScreenPos) > longPressMoveToleranceScreenPx)
            {
                longPressFired = true; // 移動しすぎたら長押し扱いにしない (Web版touchmoveと同じ)
            }

            if (!longPressFired && Time.time - pointerDownTime >= longPressSeconds)
            {
                longPressFired = true;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapContent, currentPos, null, out var localPoint))
                {
                    var (lat, lng) = LocalPointToLatLng(localPoint);
                    OnMapLongPressed?.Invoke(lat, lng);
                }
            }

            if (Input.GetMouseButtonUp(0) || Input.touchCount == 0)
            {
                pointerIsDown = false;
            }
        }
    }

    /// <summary>FlatMapView用のマーカーハンドル実装。</summary>
    internal class FlatMapMarkerHandle : IMapMarkerHandle
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public event Action OnClicked;

        private readonly GameObject gameObject;
        private readonly Image iconImage;
        private readonly Text labelText;

        public FlatMapMarkerHandle(GameObject gameObject, double latitude, double longitude)
        {
            this.gameObject = gameObject;
            Latitude = latitude;
            Longitude = longitude;

            iconImage = gameObject.GetComponentInChildren<Image>();
            labelText = gameObject.GetComponentInChildren<Text>();

            var button = gameObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnClicked?.Invoke());
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (iconImage != null)
            {
                iconImage.transform.localScale = highlighted ? Vector3.one * 1.3f : Vector3.one;
            }
        }

        public void SetLabel(string label)
        {
            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        public void SetColor(Color color)
        {
            if (iconImage != null)
            {
                iconImage.color = color;
            }
        }

        public void Remove() => DestroyGameObject();

        public void DestroyGameObject()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}
