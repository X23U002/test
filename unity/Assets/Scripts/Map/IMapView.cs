using System;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 地図描画エンジンを差し替え可能にするための抽象化レイヤー。
    ///
    /// Web版はMapbox GL JSに直結していたが、Unity側は
    /// 「どの地図プラグインを使うか」をチーム内でまだ決め切れていない
    /// 前提で、まずこのインターフェースに対して機能(マーカー・検索・
    /// 現在地・2D/3D切替・経路)を実装する。
    ///
    /// 今回同梱している既定実装 FlatMapView は追加パッケージなしで
    /// 動くシンプルな2D地図。Mapbox Maps SDK for UnityやGoogle Maps
    /// Platformを導入した場合は、このインターフェースを実装する
    /// アダプタに差し替えるだけで他のスクリプト(SpotMarkerFactory等)
    /// は変更不要になる。
    /// </summary>
    public interface IMapView
    {
        double CenterLatitude { get; }
        double CenterLongitude { get; }
        float Zoom { get; }
        float Pitch { get; }
        float Bearing { get; }

        /// <summary>ズームが変わって再描画が必要になったときに発火 (geohashクラスタリングの粒度切替用)。</summary>
        event Action OnZoomChanged;

        /// <summary>地図上をタップ/クリックしたときに発火 (座標表示・仮マーカー設置用)。</summary>
        event Action<double, double> OnMapTapped;

        /// <summary>地図を長押ししたときに発火 (スマホの仮マーカー設置用)。</summary>
        event Action<double, double> OnMapLongPressed;

        void SetCenter(double latitude, double longitude);
        void FlyTo(double latitude, double longitude, float zoom, float durationSeconds = 1.2f);
        void SetZoom(float zoom);
        void SetPitchAndBearing(float pitch, float bearing, float durationSeconds = 1.5f);

        IMapMarkerHandle AddMarker(double latitude, double longitude, string label, UnityEngine.Color color);
        void RemoveMarker(IMapMarkerHandle handle);
        void ClearMarkers();
    }

    public interface IMapMarkerHandle
    {
        double Latitude { get; }
        double Longitude { get; }
        event Action OnClicked;
        void SetHighlighted(bool highlighted);
        void SetLabel(string label);
        void Remove();
    }
}
