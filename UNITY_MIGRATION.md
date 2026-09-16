# 聖地巡礼Webアプリ → Unity 移行ガイド

`map画面/` にあるWebプロトタイプ（Mapbox GL JS + Firebase Firestore）の機能を
Unityへ移植するための土台を `unity/` フォルダに用意した。

このドキュメントは「何を」「どう対応させたか」をまとめたもので、
Unity Editorが無い環境で書いたため **Unity上でのコンパイル確認や動作確認は
まだ行っていない**。プロジェクトに取り込んだ後、まずコンパイルエラーが
出ないか確認し、シーン上でオブジェクトを配線（Inspectorでの参照設定）する
作業が必要になる。

## 1. 元のWebアプリの機能一覧

| 画面/機能 | ファイル |
|---|---|
| ログイン | `map画面/html/login.html`, `js/login.js` |
| 新規登録・確認・完了 | `newaccount.html`, `confirm.html`, `js/newaccount.js`, `js/confirm.js` |
| パスワード再発行 | `forgot_password.html`, `js/forgot_password.js` |
| 地図（マーカー・検索・絞り込み・経路・2D/3D） | `map.html`, `js/Script.js` |
| GPSスタンプ取得 | `js/stampsyori.js` |
| マイページ（プロフィール編集） | `mypage.html`, `js/mypage.js` |
| 履歴・ルート案内（モック） | `history.html`, `js/history.js` |
| 獲得スタンプ一覧 | `stamp.html`, `js/stamp.js` |
| 聖地登録申請 | `register.html`, `js/register.js` |
| お問い合わせ | `information.html`, `js/information.js` |

バックエンドはFirebaseプロジェクト `sisukai-121cf` で、Firestoreに
`spot`（スポット情報）と `stampUsers`（匿名ログインユーザーごとのスタンプ履歴）
の2コレクションを持つ。Unity版も同じFirebaseプロジェクトを共有する設計。

## 2. Unity側の構成

```
unity/Assets/
  Scripts/
    Data/      SpotData.cs, StampRecord.cs           … Firestoreのデータモデル
    Services/  FirebaseBootstrapper.cs                … Firebase初期化
               SpotRepository.cs                      … spotコレクション読込 (loadData()相当)
               StampService.cs                        … GPSスタンプ判定+匿名ログイン+同期 (stampsyori.js相当)
               LocalStampStore.cs                      … 端末保存 (localStorage相当)
               GeolocationService.cs                   … Unity Input.locationのラッパー
    Map/       IMapView.cs / FlatMapView.cs            … 地図描画の抽象化 + 既定実装
               SpotMarkerFactory.cs                    … マーカー描画・geohashクラスタリング
               SpotPopupPanel.cs                        … マーカータップ時のポップアップ
               SearchController.cs                      … あいまい検索 (normalizeText相当)
               AnimeFilterPanel.cs                      … 「アニメで絞り込み」モーダル
               RoutePanelController.cs                  … 経路パネル + Googleマップ連携
               MapHudController.cs                      … 現在地/2D3D/ハンバーガーメニュー等のHUD
               PilgrimageMapController.cs                … 上記すべてを束ねる地図画面の中心
    UI/        AppShellController.cs                    … 画面遷移の管理（Web版のページ遷移に相当）
               LoginScreen.cs / NewAccountScreen.cs / ConfirmScreen.cs /
               ForgotPasswordScreen.cs / MyPageScreen.cs / HistoryScreen.cs /
               StampScreen.cs / RegisterSpotScreen.cs / InformationScreen.cs
  Editor/
    FirestoreCsvImporter.cs   … CSV→Firestore一括登録 (チームの既存スクリプトを更新)
  StreamingAssets/
    (spot.csv をここに置く)
```

### 地図描画について

Web版はMapbox GL JSに直結していたが、Unity側でどの地図プラグイン
（Mapbox Maps SDK for Unity / Google Maps Platform / 自前タイル 等）を
使うかチームでまだ決めていない前提で、`IMapView` という抽象化を挟んだ。

同梱している `FlatMapView` は追加パッケージ不要で動く簡易2D地図
（緯度経度を単純な正距円筒図法でUI座標に変換するだけのもの）。
これによりマーカー表示・検索・絞り込み・経路・スタンプ判定など
「地図描画エンジンに依存しない部分」はそのまま動作確認できる。

本番でMapbox Maps SDK for Unity等を導入する場合は、`IMapView` を実装する
アダプタ（例: `MapboxMapView`）を新規作成し、`PilgrimageMapController` /
`MapHudController` のInspectorで参照を差し替えるだけでよい。

## 3. Unity側で追加インストールが必要なもの

1. **Firebase Unity SDK**
   - `FirebaseAuth.unitypackage`（匿名ログイン用）
   - `FirebaseFirestore.unitypackage`
   - Firebaseコンソール（プロジェクト: sisukai-121cf）でUnityアプリを登録し、
     `google-services.json` / `GoogleService-Info.plist` を配置する。
2. （任意）実際の地図タイルを使う場合は Mapbox Maps SDK for Unity 等。
3. Unity 2021.3 LTS以降を推奨（`is not` パターン等 C# 9 構文を使用しているため）。

## 4. Firestoreスキーマ対応表

Web版 `Script.js` の `loadData()` が読んでいるフィールドと、
`SpotRepository.cs` が読むフィールドは同一。

| Firestoreフィールド | 型 | Unity `SpotData` |
|---|---|---|
| `coord` | GeoPoint | `Latitude` / `Longitude` |
| `title_name` | string | `TitleName` |
| `spot_name` | string | `SpotName` |
| `scene` or `spot_info` | string | `SpotInfo` |
| `image_url` | string ("not_image"可) | `ImageUrl` |
| `title_url` | string | `TitleUrl` |
| `geo_hash` | string | `GeoHash` |
| `stamp_radius` | number (省略時50) | `StampRadiusMeters` |

`stampUsers/{uid}` の `stampHistory` 配列 (`spotId` / `stampedAt`) は
`StampService` がそのまま読み書きする。

## 5. 未移植・簡略化した部分（次にやること）

- **経路探索**: Web版と同じくGoogleマップアプリへ座標を渡すだけ
  （`RoutePanelController.OpenInGoogleMaps`）。Unity内蔵の経路案内は未実装。
- **履歴画面**: Web版が静的モックのままなので、Unity版 (`HistoryScreen`) も
  表示専用のプレースホルダー。実データ化は今後の課題。
- **住所・駅名検索**: Web版はMapbox Geocoding APIに委譲していた部分。
  `SearchController` は自前スポットのあいまい検索のみ対応。一般住所検索が
  必要ならGeocoding REST APIを`UnityWebRequest`で呼ぶアダプタを追加する。
- **ログイン/新規登録**: Web版同様、実際の認証チェックは行わないプロトタイプ。
  本実装時はFirebase Authenticationのメール/パスワード認証に置き換える。
- **クラスタのポップアップ**: Web版は複数件をホバーポップアップで一覧表示するが、
  Unity版は簡略化してクラスタタップ時にズームインするだけにしている。

## 6. 動作確認の進め方（Unity Editorで）

1. `unity/Assets` を既存のUnityプロジェクトの `Assets` 配下にコピーする
   （または新規3D/2Dプロジェクトを作りコピーする）。
2. 上記のFirebase Unity SDKパッケージをインポートする。
3. コンパイルエラーが出ないことを確認する。
4. シーンを1つ作成し、`AppShellController` をアタッチしたGameObjectと、
   各画面のCanvas/Panel（Login, Map, MyPageなど）を配置してInspectorで
   参照を配線する。地図画面には `FlatMapView` を貼ったCanvas配下に
   マーカーPrefab・ポップアップPanel等を用意し、`PilgrimageMapController`
   と `MapHudController` に参照を設定する。
5. Play Modeで、スポットがFirestoreから読み込まれてマーカーが表示されるか、
   マーカータップでポップアップが出るか、GPSスタンプ判定が動くかを確認する。
