using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Pilgrimage.Data;
using UnityEngine;

namespace Pilgrimage.Services
{
    public enum StampAvailability
    {
        Cooldown,
        WaitingForLocation,
        LowAccuracy,
        TooFar,
        Available
    }

    public readonly struct StampState
    {
        public readonly StampAvailability Availability;
        public readonly string Label;
        public readonly string Message;
        public bool IsCollectible => Availability == StampAvailability.Available;

        public StampState(StampAvailability availability, string label, string message)
        {
            Availability = availability;
            Label = label;
            Message = message;
        }
    }

    /// <summary>
    /// GPS連動のスタンプ取得ロジック。Web版 stampsyori.js を移植したもの。
    /// ・匿名ログイン + Firestore(stampUsers/{uid}) 同期
    /// ・取得済み判定のクールダウン (デバッグ時15秒 / 本番24時間)
    /// ・現在地とスポットの距離判定 (半径 stamp_radius メートル以内)
    /// </summary>
    public class StampService
    {
        private const string StampCollectionName = "stampUsers";
        private const float DefaultStampRadiusMeters = 50f;
        private const float MaxStampAccuracyMeters = 100f;

        // Web版と同じく開発中はtrueにして15秒クールダウンで動作確認する
        public bool DebugMode = true;
        private static readonly TimeSpan DebugCooldown = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan NormalCooldown = TimeSpan.FromHours(24);

        private readonly LocalStampStore localStore;
        private DocumentReference userDocument;
        private GeoPosition? currentPosition;

        public event Action OnStampStateChanged;

        public StampService(LocalStampStore localStore)
        {
            this.localStore = localStore;
        }

        private TimeSpan Cooldown => DebugMode ? DebugCooldown : NormalCooldown;

        public void UpdatePosition(GeoPosition position)
        {
            currentPosition = position;
            OnStampStateChanged?.Invoke();
        }

        /// <summary>
        /// 匿名ログインしてFirestoreの履歴と端末の履歴をマージする。
        /// Web版 initializeStampStorage() に相当。
        /// </summary>
        public async Task<bool> InitializeAsync(Func<string, SpotData> spotLookup)
        {
            try
            {
                await FirebaseBootstrapper.InitializeAsync();

                var auth = FirebaseAuth.DefaultInstance;
                var user = auth.CurrentUser;

                if (user == null)
                {
                    var result = await auth.SignInAnonymouslyAsync();
                    user = result.User;
                }

                var db = FirebaseFirestore.DefaultInstance;
                userDocument = db.Collection(StampCollectionName).Document(user.UserId);

                var snapshot = await userDocument.GetSnapshotAsync();
                var serverHistory = new List<StampEntryDto>();

                if (snapshot.Exists && snapshot.TryGetValue<List<object>>("stampHistory", out var rawList))
                {
                    foreach (var item in rawList)
                    {
                        if (item is not Dictionary<string, object> map)
                        {
                            continue;
                        }

                        serverHistory.Add(new StampEntryDto
                        {
                            spotId = map.TryGetValue("spotId", out var idObj) ? idObj?.ToString() : null,
                            stampedAt = map.TryGetValue("stampedAt", out var atObj) ? Convert.ToInt64(atObj) : 0L
                        });
                    }
                }

                localStore.MergeFromServer(serverHistory, spotLookup);

                // 端末側の履歴の方が新しい(またはサーバーに無い)場合はサーバーへ書き戻す
                var needsSync = localStore.Records.Values.Any(local =>
                {
                    var serverEntry = serverHistory.Find(s => s.spotId == local.SpotId);
                    return serverEntry == null || local.StampedAtUnixMs > serverEntry.stampedAt;
                });

                if (needsSync)
                {
                    await WriteHistoryToFirestoreAsync();
                }

                Debug.Log("スタンプ：Firestore同期完了");
                OnStampStateChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"スタンプのFirestore同期に失敗しました: {e}");
                OnStampStateChanged?.Invoke();
                return false;
            }
        }

        public StampState GetStampState(SpotData spot)
        {
            var remaining = GetRemainingCooldown(spot.Id);

            if (remaining > TimeSpan.Zero)
            {
                var remainingText = FormatRemaining(remaining);
                return new StampState(StampAvailability.Cooldown, $"⏳ あと{remainingText}", $"{remainingText}後にもう一度取得できます");
            }

            var radius = spot.StampRadiusMeters > 0 ? spot.StampRadiusMeters : DefaultStampRadiusMeters;

            if (currentPosition == null)
            {
                return new StampState(StampAvailability.WaitingForLocation, "📍 現在地を取得してください", $"半径{radius}m以内で取得できます");
            }

            var position = currentPosition.Value;

            if (position.AccuracyMeters > MaxStampAccuracyMeters)
            {
                return new StampState(StampAvailability.LowAccuracy, "現在地から離れています。", $"GPS誤差 ±{Mathf.RoundToInt(position.AccuracyMeters)}m");
            }

            var distance = CalculateDistanceMeters(position.Latitude, position.Longitude, spot.Latitude, spot.Longitude);

            if (distance <= radius)
            {
                return new StampState(StampAvailability.Available, "🎫 スタンプを取得", $"スポットまで{FormatDistance(distance)}・取得可能");
            }

            var remainingDistance = Math.Max(0, distance - radius);
            return new StampState(StampAvailability.TooFar, $"あと{FormatDistance(remainingDistance)}", $"スポットまで{FormatDistance(distance)}・半径{radius}m以内で取得できます");
        }

        public bool IsSpotStamped(string spotId) => GetRemainingCooldown(spotId) > TimeSpan.Zero;

        /// <summary>
        /// スタンプ取得ボタン押下時の処理。Web版 collectStamp() に相当。
        /// </summary>
        public async Task<(bool success, string message)> CollectStampAsync(SpotData spot)
        {
            var state = GetStampState(spot);

            if (!state.IsCollectible)
            {
                return (false, state.Message);
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // 通信より先に端末へ保存 (Web版と同じ順序)
            localStore.SetStamp(spot, nowMs);
            OnStampStateChanged?.Invoke();

            var nextText = DebugMode ? "15秒後に再取得できます" : "24時間後に再取得できます";

            try
            {
                await WriteHistoryToFirestoreAsync();
                return (true, $"{spot.SpotName}のスタンプを取得しました！\n{nextText}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Firestoreへのスタンプ保存に失敗しました: {e}");
                return (true, $"{spot.SpotName}のスタンプを端末に保存しました。\nFirestoreへの保存には失敗しました。\n{nextText}");
            }
        }

        public async Task ResetStampAsync(string spotId)
        {
            localStore.RemoveStamp(spotId);
            OnStampStateChanged?.Invoke();

            try
            {
                await WriteHistoryToFirestoreAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"リセットのFirestore同期に失敗しました: {e}");
            }
        }

        private async Task WriteHistoryToFirestoreAsync()
        {
            if (userDocument == null)
            {
                throw new InvalidOperationException("Firestoreの保存先がありません");
            }

            var data = new Dictionary<string, object>
            {
                { "stampHistory", localStore.ToServerHistory().Select(e => new Dictionary<string, object>
                    {
                        { "spotId", e.spotId },
                        { "stampedAt", e.stampedAt }
                    }).ToList<object>()
                },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            await userDocument.SetAsync(data, SetOptions.MergeAll);
        }

        private TimeSpan GetRemainingCooldown(string spotId)
        {
            var stampedAtMs = localStore.GetStampedAtUnixMs(spotId);
            if (stampedAtMs <= 0)
            {
                return TimeSpan.Zero;
            }

            var stampedAt = DateTimeOffset.FromUnixTimeMilliseconds(stampedAtMs);
            var elapsed = DateTimeOffset.UtcNow - stampedAt;
            var remaining = Cooldown - elapsed;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining.TotalHours >= 1)
            {
                return $"{(int)remaining.TotalHours}時間{remaining.Minutes}分";
            }

            if (remaining.TotalMinutes >= 1)
            {
                return $"{(int)remaining.TotalMinutes}分{remaining.Seconds}秒";
            }

            return $"{Math.Max(0, remaining.Seconds)}秒";
        }

        private static string FormatDistance(double meters)
        {
            return meters < 1000
                ? $"{Math.Round(meters)}m"
                : $"{(meters / 1000).ToString("0.0")}km";
        }

        private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371000d;
            double ToRad(double deg) => deg * Math.PI / 180d;

            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);

            var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);

            return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
