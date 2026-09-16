using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pilgrimage.Data;
using UnityEngine;

namespace Pilgrimage.Services
{
    /// <summary>
    /// 端末側のスタンプ履歴保存。Web版の localStorage
    /// ("osikatumap-stamp-history-v2" / "collectedStamps") に相当する。
    /// PlayerPrefsではなくJSONファイルを使うのは、配列を含む構造化データを
    /// そのまま保存でき、Web版のデータ構造ともほぼ1:1で対応させられるため。
    /// </summary>
    public class LocalStampStore
    {
        private const string FileName = "pilgrimage_stamp_history.json";
        private readonly string filePath;

        private Dictionary<string, StampRecord> records = new Dictionary<string, StampRecord>();

        public LocalStampStore()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
            Load();
        }

        public IReadOnlyDictionary<string, StampRecord> Records => records;

        public long GetStampedAtUnixMs(string spotId)
        {
            return records.TryGetValue(spotId, out var record) ? record.StampedAtUnixMs : 0L;
        }

        public void SetStamp(SpotData spot, long stampedAtUnixMs)
        {
            var dateLabel = DateTimeOffset
                .FromUnixTimeMilliseconds(stampedAtUnixMs)
                .ToLocalTime()
                .ToString("yyyy年MM月dd日");

            records[spot.Id] = new StampRecord
            {
                SpotId = spot.Id,
                StampedAtUnixMs = stampedAtUnixMs,
                SpotName = spot.SpotName,
                TitleName = spot.TitleName,
                SceneText = spot.SpotInfo,
                Address = string.Empty,
                DateLabel = dateLabel
            };

            Save();
        }

        public void MergeFromServer(IEnumerable<StampEntryDto> serverHistory, Func<string, SpotData> spotLookup)
        {
            var changed = false;

            foreach (var entry in serverHistory)
            {
                if (string.IsNullOrEmpty(entry.spotId))
                {
                    continue;
                }

                var existing = GetStampedAtUnixMs(entry.spotId);
                if (entry.stampedAt <= existing)
                {
                    continue;
                }

                var spot = spotLookup?.Invoke(entry.spotId);
                if (spot != null)
                {
                    SetStamp(spot, entry.stampedAt);
                }
                else if (!records.ContainsKey(entry.spotId))
                {
                    records[entry.spotId] = new StampRecord
                    {
                        SpotId = entry.spotId,
                        StampedAtUnixMs = entry.stampedAt
                    };
                }

                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        public void RemoveStamp(string spotId)
        {
            if (records.Remove(spotId))
            {
                Save();
            }
        }

        public void ClearAll()
        {
            records.Clear();
            Save();
        }

        public List<StampEntryDto> ToServerHistory()
        {
            return records.Values
                .Select(r => new StampEntryDto { spotId = r.SpotId, stampedAt = r.StampedAtUnixMs })
                .ToList();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                var json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<StampRecordListWrapper>(json);

                if (wrapper?.items == null)
                {
                    return;
                }

                records = wrapper.items.ToDictionary(r => r.SpotId, r => r);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"スタンプ履歴の読込に失敗しました: {e}");
                records = new Dictionary<string, StampRecord>();
            }
        }

        private void Save()
        {
            try
            {
                var wrapper = new StampRecordListWrapper { items = records.Values.ToList() };
                File.WriteAllText(filePath, JsonUtility.ToJson(wrapper));
            }
            catch (Exception e)
            {
                Debug.LogError($"スタンプ履歴の端末保存に失敗しました: {e}");
            }
        }

        [Serializable]
        private class StampRecordListWrapper
        {
            public List<StampRecord> items;
        }
    }
}
