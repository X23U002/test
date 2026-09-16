using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Firebase.Firestore;
using Pilgrimage.Data;
using UnityEditor;
using UnityEngine;

namespace Pilgrimage.EditorTools
{
    /// <summary>
    /// CSVからFirestoreの "spot" コレクションへ一括登録するEditor拡張。
    /// チームが元々作っていた FirestoreCsvImporter.cs (teamiriam3-art/test
    /// リポジトリの firestore/FirestoreCsvImporter.cs) をベースに、
    /// 実際に稼働しているWeb版 (map画面/js/Script.js) が読んでいる
    /// フィールド名 (title_name, spot_name, scene/spot_info, geo_hash,
    /// stamp_radius) に合わせて更新したもの。
    ///
    /// CSV列の並び:
    /// id, title_name, latitude, longitude, image_url, spot_name, spot_info, title_url, geo_hash, stamp_radius(省略可)
    /// </summary>
    public static class FirestoreCsvImporter
    {
        [MenuItem("Firestore/CSVをFirestoreへ登録 (spot)")]
        public static async void ImportCsv()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "spot.csv");

            if (!File.Exists(path))
            {
                Debug.LogError($"CSVが見つかりません：{path}");
                return;
            }

            var db = FirebaseFirestore.DefaultInstance;
            var lines = File.ReadAllLines(path);

            for (var i = 1; i < lines.Length; i++) // 1行目はヘッダーなので飛ばす
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                var cols = lines[i].Split(',');

                if (cols.Length < 9)
                {
                    Debug.LogWarning($"{i + 1}行目の列数が不足しています。");
                    continue;
                }

                var spot = new SpotData
                {
                    Id = cols[0],
                    TitleName = cols[1],
                    Latitude = double.Parse(cols[2], CultureInfo.InvariantCulture),
                    Longitude = double.Parse(cols[3], CultureInfo.InvariantCulture),
                    ImageUrl = cols[4],
                    SpotName = cols[5],
                    SpotInfo = cols[6],
                    TitleUrl = cols[7],
                    GeoHash = cols[8],
                    StampRadiusMeters = cols.Length > 9 && float.TryParse(cols[9], NumberStyles.Any, CultureInfo.InvariantCulture, out var radius)
                        ? radius
                        : 50f
                };

                var geoPoint = new GeoPoint(spot.Latitude, spot.Longitude);

                var firestoreData = new Dictionary<string, object>
                {
                    { "title_name", spot.TitleName },
                    { "coord", geoPoint },
                    { "image_url", spot.ImageUrl },
                    { "spot_name", spot.SpotName },
                    { "spot_info", spot.SpotInfo },
                    { "title_url", spot.TitleUrl },
                    { "geo_hash", spot.GeoHash },
                    { "stamp_radius", spot.StampRadiusMeters }
                };

                try
                {
                    await db.Collection("spot").Document(spot.Id).SetAsync(firestoreData);
                    Debug.Log($"{spot.Id} 登録完了");
                }
                catch (Exception e)
                {
                    Debug.LogError($"{spot.Id} の登録に失敗しました。\n{e}");
                }
            }

            Debug.Log("CSV登録完了");
        }
    }
}
