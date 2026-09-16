using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 経路案内パネル。Web版 Script.js の
    /// addSpotToRouteById/removeSpotFromRoute/clearSelectedSpots/
    /// updateRouteList/openGoogleMapsRoute を移植したもの。
    /// 実際の経路探索はGoogleマップアプリに委譲する (Web版と同じ設計)。
    /// </summary>
    public class RoutePanelController : MonoBehaviour
    {
        private class RouteEntry
        {
            public string Id;
            public string Name;
            public double Latitude;
            public double Longitude;
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private GameObject routeItemPrefab; // Text + 削除Button
        [SerializeField] private Button openGoogleMapsButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Text emptyMessageText;

        private readonly List<RouteEntry> selectedSpots = new List<RouteEntry>();

        private void Awake()
        {
            openGoogleMapsButton.onClick.AddListener(OpenInGoogleMaps);
            clearAllButton.onClick.AddListener(ClearAll);
            panelRoot.SetActive(false);
        }

        public void AddSpot(SpotData spot)
        {
            if (selectedSpots.Any(s => s.Id == spot.Id))
            {
                Debug.LogWarning("この場所はすでに追加されています");
                return;
            }

            selectedSpots.Add(new RouteEntry
            {
                Id = spot.Id,
                Name = string.IsNullOrEmpty(spot.SpotName) ? spot.TitleName : spot.SpotName,
                Latitude = spot.Latitude,
                Longitude = spot.Longitude
            });

            panelRoot.SetActive(true);
            RefreshList();
        }

        public void RemoveSpot(string spotId)
        {
            selectedSpots.RemoveAll(s => s.Id == spotId);
            RefreshList();

            if (selectedSpots.Count == 0)
            {
                panelRoot.SetActive(false);
            }
        }

        public void ClearAll()
        {
            selectedSpots.Clear();
            RefreshList();
            panelRoot.SetActive(false);
        }

        private void RefreshList()
        {
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }

            emptyMessageText.gameObject.SetActive(selectedSpots.Count == 0);

            for (var i = 0; i < selectedSpots.Count; i++)
            {
                var entry = selectedSpots[i];
                var go = Instantiate(routeItemPrefab, listContainer);
                var label = go.GetComponentInChildren<Text>();
                var removeButton = go.GetComponentInChildren<Button>();

                label.text = $"{i + 1}. {entry.Name}";
                removeButton.onClick.AddListener(() => RemoveSpot(entry.Id));
            }
        }

        private void OpenInGoogleMaps()
        {
            if (selectedSpots.Count == 0)
            {
                Debug.LogWarning("行きたい場所を追加してください");
                return;
            }

            var locations = selectedSpots
                .Select(s => $"{s.Latitude.ToString("F6")},{s.Longitude.ToString("F6")}")
                .ToList();

            var destination = locations[^1];
            var waypoints = string.Join("|", locations.Take(locations.Count - 1));

            var url = "https://www.google.com/maps/dir/?api=1" +
                       $"&destination={UnityEngine.Networking.UnityWebRequest.EscapeURL(destination)}";

            if (!string.IsNullOrEmpty(waypoints))
            {
                url += $"&waypoints={UnityEngine.Networking.UnityWebRequest.EscapeURL(waypoints)}";
            }

            url += "&travelmode=walking";

            Application.OpenURL(url);
        }
    }
}
