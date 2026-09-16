using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Pilgrimage.Data;
using UnityEngine;

namespace Pilgrimage.Services
{
    /// <summary>
    /// Firestoreの "spot" コレクションを取得する。
    /// Web版 Script.js の loadData() に相当。
    /// </summary>
    public class SpotRepository
    {
        private const string SpotCollectionName = "spot";

        public async Task<List<SpotData>> LoadSpotsAsync()
        {
            await FirebaseBootstrapper.InitializeAsync();

            var db = FirebaseFirestore.DefaultInstance;
            var snapshot = await db.Collection(SpotCollectionName).GetSnapshotAsync();

            var spots = new List<SpotData>();

            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();

                if (!data.TryGetValue("coord", out var coordObj) || coordObj is not GeoPoint coord)
                {
                    // Web版と同じく座標がないスポットはスキップする
                    continue;
                }

                var spot = new SpotData
                {
                    Id = doc.Id,
                    Latitude = coord.Latitude,
                    Longitude = coord.Longitude,
                    TitleName = GetString(data, "title_name", "その他"),
                    SpotName = GetString(data, "spot_name", "無題のスポット"),
                    SpotInfo = GetString(data, "scene", GetString(data, "spot_info", "情報がありません")),
                    ImageUrl = GetString(data, "image_url", "not_image"),
                    TitleUrl = GetString(data, "title_url", string.Empty),
                    GeoHash = GetString(data, "geo_hash", string.Empty),
                    StampRadiusMeters = GetFloat(data, "stamp_radius", 50f)
                };

                spots.Add(spot);
            }

            return spots;
        }

        private static string GetString(IDictionary<string, object> data, string key, string fallback)
        {
            if (data.TryGetValue(key, out var value) && value is string str && !string.IsNullOrEmpty(str))
            {
                return str;
            }

            return fallback;
        }

        private static float GetFloat(IDictionary<string, object> data, string key, float fallback)
        {
            if (!data.TryGetValue(key, out var value))
            {
                return fallback;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
