using System;
using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using UnityEngine;

namespace Pilgrimage.Map
{
    /// <summary>
    /// スポットのマーカー描画。Web版 Script.js の drawMarkers() /
    /// groupSpotsByGeohash() を移植したもの。
    /// ズーム10以下ではgeohashの先頭何文字かでグループ化してまとめて表示し、
    /// ズーム11以上では個別スポットを表示する。
    /// </summary>
    public class SpotMarkerFactory
    {
        private static readonly Color[] Palette =
        {
            new Color32(0xE5, 0x39, 0x35, 0xFF),
            new Color32(0x1E, 0x88, 0xE5, 0xFF),
            new Color32(0x43, 0xA0, 0x47, 0xFF),
            new Color32(0xFB, 0x8C, 0x00, 0xFF),
            new Color32(0x8E, 0x24, 0xAA, 0xFF),
            new Color32(0xFD, 0xD8, 0x35, 0xFF),
            new Color32(0x00, 0x89, 0x7B, 0xFF),
            new Color32(0x6D, 0x4C, 0x41, 0xFF),
            new Color32(0xEC, 0x40, 0x7A, 0xFF),
            new Color32(0x5E, 0x35, 0xB1, 0xFF)
        };

        private readonly IMapView mapView;
        private readonly Dictionary<string, Color> titleColorMap = new Dictionary<string, Color>();
        private readonly List<IMapMarkerHandle> activeMarkers = new List<IMapMarkerHandle>();
        private int nextColorIndex;

        public event Action<SpotData> OnSpotMarkerClicked;
        public event Action<List<SpotData>> OnClusterMarkerClicked;

        public SpotMarkerFactory(IMapView mapView)
        {
            this.mapView = mapView;
        }

        public Color GetColorForTitle(string titleName)
        {
            if (string.IsNullOrEmpty(titleName))
            {
                titleName = "その他";
            }

            if (!titleColorMap.TryGetValue(titleName, out var color))
            {
                color = Palette[nextColorIndex % Palette.Length];
                nextColorIndex++;
                titleColorMap[titleName] = color;
            }

            return color;
        }

        public void Redraw(IReadOnlyList<SpotData> spots, Func<string, bool> isSpotStamped)
        {
            foreach (var marker in activeMarkers)
            {
                marker.Remove();
            }
            activeMarkers.Clear();

            var zoom = mapView.Zoom;

            if (zoom <= 10f)
            {
                DrawClusters(spots, zoom);
            }
            else
            {
                DrawIndividualMarkers(spots, isSpotStamped);
            }
        }

        private void DrawClusters(IReadOnlyList<SpotData> spots, float zoom)
        {
            var geohashLength = zoom <= 7f ? 3 : 4;

            var groups = spots
                .Where(s => !string.IsNullOrEmpty(s.GeoHash))
                .GroupBy(s => s.GeoHash.Substring(0, Math.Min(geohashLength, s.GeoHash.Length)))
                .ToList();

            foreach (var group in groups)
            {
                var representative = group.First();
                var members = group.ToList();

                var handle = mapView.AddMarker(
                    representative.Latitude,
                    representative.Longitude,
                    members.Count.ToString(),
                    Color.cyan);

                handle.OnClicked += () => OnClusterMarkerClicked?.Invoke(members);
                activeMarkers.Add(handle);
            }
        }

        private void DrawIndividualMarkers(IReadOnlyList<SpotData> spots, Func<string, bool> isSpotStamped)
        {
            foreach (var spot in spots)
            {
                if (!spot.HasValidCoordinate)
                {
                    continue;
                }

                var color = GetColorForTitle(spot.TitleName);
                var handle = mapView.AddMarker(spot.Latitude, spot.Longitude, spot.SpotName, color);
                handle.SetHighlighted(isSpotStamped != null && isSpotStamped(spot.Id));
                handle.OnClicked += () => OnSpotMarkerClicked?.Invoke(spot);

                activeMarkers.Add(handle);
            }
        }

        public void Clear()
        {
            foreach (var marker in activeMarkers)
            {
                marker.Remove();
            }
            activeMarkers.Clear();
        }
    }
}
