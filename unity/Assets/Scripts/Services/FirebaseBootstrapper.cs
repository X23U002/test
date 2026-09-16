using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;

namespace Pilgrimage.Services
{
    /// <summary>
    /// Firebase Unity SDK の初期化を一箇所にまとめる。
    /// Web版 (Script.js / stampsyori.js) は同じ firebaseConfig を2つのSDK
    /// (モジュラー / compat) で別々に initializeApp していたが、
    /// Unity側はアプリ起動時に一度だけ初期化すればよい。
    ///
    /// 事前準備 (Firebase Unity SDK公式手順):
    ///   1. Firebaseコンソールで sisukai-121cf プロジェクトに Unityアプリを追加し、
    ///      google-services.json (Android) / GoogleService-Info.plist (iOS) を
    ///      Assets直下に配置する。
    ///   2. Firebase Unity SDK の FirebaseAuth.unitypackage と
    ///      FirebaseFirestore.unitypackage をインポートする。
    /// </summary>
    public static class FirebaseBootstrapper
    {
        public static bool IsReady { get; private set; }
        private static TaskCompletionSource<bool> readyTcs;

        public static Task<bool> InitializeAsync()
        {
            if (IsReady)
            {
                return Task.FromResult(true);
            }

            if (readyTcs != null)
            {
                return readyTcs.Task;
            }

            readyTcs = new TaskCompletionSource<bool>();

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var status = task.Result;

                if (status == DependencyStatus.Available)
                {
                    IsReady = true;
                    Debug.Log("Firebase接続成功！");
                    readyTcs.SetResult(true);
                }
                else
                {
                    Debug.LogError($"Firebaseの初期化に失敗しました: {status}");
                    readyTcs.SetResult(false);
                }
            });

            return readyTcs.Task;
        }
    }
}
