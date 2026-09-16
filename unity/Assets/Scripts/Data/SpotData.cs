using System.Collections.Generic;

namespace Pilgrimage.Data
{
    /// <summary>
    /// Firestoreの "spot" コレクション1件分のデータ。
    /// Web版 Script.js の loadData() / drawMarkers() が読んでいたフィールドと1:1で対応させている。
    /// </summary>
    [System.Serializable]
    public class SpotData
    {
        public string Id;
        public string TitleName;     // 作品名 (旧: title経由 → 現在はspotに直接titleNameを持つ)
        public string SpotName;      // スポット名
        public string SpotInfo;      // 登場シーン説明 (旧spot_info / scene)
        public string ImageUrl;      // "not_image" の場合はプレースホルダー画像を使う
        public string TitleUrl;      // 公式サイト等の外部リンク
        public string GeoHash;       // 低ズーム時のクラスタリングに使用
        public double Latitude;
        public double Longitude;
        public float StampRadiusMeters = 50f; // stamp_radius未設定時はデフォルト値を使う

        public bool HasValidCoordinate =>
            !double.IsNaN(Latitude) && !double.IsNaN(Longitude) &&
            (Latitude != 0d || Longitude != 0d);
    }

    /// <summary>
    /// スタンプ取得済み履歴1件。localStorageの stampHistory / collectedStamps に相当。
    /// </summary>
    [System.Serializable]
    public class StampRecord
    {
        public string SpotId;
        public long StampedAtUnixMs;
        public string SpotName;
        public string TitleName;
        public string SceneText;
        public string Address;
        public string DateLabel; // "2026年05月15日" 形式の表示用文字列
    }

    [System.Serializable]
    public class StampHistoryDto
    {
        public List<StampEntryDto> stampHistory = new List<StampEntryDto>();
    }

    [System.Serializable]
    public class StampEntryDto
    {
        public string spotId;
        public long stampedAt;
    }
}
