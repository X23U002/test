using System;
using System.Collections;
using UnityEngine;

namespace Pilgrimage.Services
{
    public readonly struct GeoPosition
    {
        public readonly double Latitude;
        public readonly double Longitude;
        public readonly float AccuracyMeters;

        public GeoPosition(double latitude, double longitude, float accuracyMeters)
        {
            Latitude = latitude;
            Longitude = longitude;
            AccuracyMeters = accuracyMeters;
        }
    }

    /// <summary>
    /// 端末のGPSを扱う。Web版の mapboxgl.GeolocateControl に相当。
    /// MonoBehaviourとしてシーンに1つ配置し、位置情報が更新されるたびに
    /// OnPositionUpdated を発火する。StampService や MapView がこれを購読する。
    /// </summary>
    public class GeolocationService : MonoBehaviour
    {
        public static GeolocationService Instance { get; private set; }

        public event Action<GeoPosition> OnPositionUpdated;
        public event Action<string> OnError;

        public GeoPosition? LastPosition { get; private set; }
        public bool IsRunning { get; private set; }

        private const float DesiredAccuracyMeters = 5f;
        private const float MinDistanceMeters = 2f;
        private const float TimeoutSeconds = 20f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 現在地ボタン (#location-btn) を押したときに呼ぶ。
        /// Web版の geolocate.trigger() に相当。
        /// </summary>
        public void TriggerLocate()
        {
            if (!IsRunning)
            {
                StartCoroutine(StartLocationServiceRoutine());
            }
        }

        private IEnumerator StartLocationServiceRoutine()
        {
            if (!Input.location.isEnabledByUser)
            {
                OnError?.Invoke("位置情報サービスが端末で無効になっています。");
                yield break;
            }

            IsRunning = true;
            Input.location.Start(DesiredAccuracyMeters, MinDistanceMeters);

            var elapsed = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && elapsed < TimeoutSeconds)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                IsRunning = false;
                OnError?.Invoke($"現在地を取得できません: {Input.location.status}");
                yield break;
            }

            while (Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;
                var position = new GeoPosition(data.latitude, data.longitude, data.horizontalAccuracy);
                LastPosition = position;
                OnPositionUpdated?.Invoke(position);

                yield return new WaitForSeconds(1f);
            }

            IsRunning = false;
        }

        private void OnDestroy()
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }
        }
    }
}
