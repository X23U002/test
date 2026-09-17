# UI Toolkit で Web版UIを移行する手順

`map画面/css/` の約2,700行のCSSと `html/` の10画面を、Unityの **UI Toolkit**
（UXML + USS）へ移した記録と手引き。**全10画面を変換済み**で、
ファイルは `unity/Assets/UI/`（UXML/USS）と
`unity/Assets/Scripts/UI/Toolkit/`（C#）にある。

| Web版 | UXML | USS | C# |
|---|---|---|---|
| login.html | `Login.uxml` | `Login.uss` | `LoginScreenView.cs` |
| newaccount.html | `NewAccount.uxml` | (Common) | `NewAccountScreenView.cs` |
| confirm.html | `Confirm.uxml` | `Confirm.uss` | `ConfirmScreenView.cs` |
| forgot_password.html | `ForgotPassword.uxml` | (Common) | `ForgotPasswordScreenView.cs` |
| mypage.html | `MyPage.uxml` | `MyPage.uss` | `MyPageScreenView.cs` |
| history.html | `History.uxml` | `History.uss` | `HistoryScreenView.cs` |
| stamp.html | `Stamp.uxml` + `StampItem.uxml` | `Stamp.uss` | `StampScreenView.cs` |
| register.html | `RegisterSpot.uxml` | `Sheet.uss` | `RegisterSpotScreenView.cs` |
| information.html | `Information.uxml` | `Sheet.uss` | `InformationScreenView.cs` |
| map.html (HUD部分) | `MapHud.uxml` | `MapHud.uss` | `MapHudView.cs` |

全画面で共通の色・ヘッダー・フォーム・モーダルは `Common.uss` に集約している。

## 1. UI Toolkit とは

UnityのUIシステムは2種類ある。

| | uGUI (従来) | UI Toolkit |
|---|---|---|
| 作り方 | Hierarchyに Image/Text/Button を並べる | UXML(≒HTML) + USS(≒CSS) を書く |
| レイアウト | アンカーとRectTransform | Flexbox |
| スタイル | 各コンポーネントのInspector | セレクタとクラスで一括指定 |
| Web移行 | CSSは流用できない | CSSの考え方がほぼそのまま通じる |

**HTML/CSSで作ったUIがある今回はUI Toolkitが有利**。`.login-container` のような
クラス名も、flexboxの考え方も、そのまま持ち込める。

- **UXML** = HTML相当。画面の構造。`<div>` が `<ui:VisualElement>`、
  `<h1>/<p>/<span>` が `<ui:Label>`、`<input>` が `<ui:TextField>`、
  `<button>` が `<ui:Button>`。
- **USS** = CSS相当。`.クラス名 { ... }` の書式・セレクタ・継承はCSSと同じ。
- **C#** = JS相当。`root.Q<Button>("login-button").clicked += ...` で
  `addEventListener` と同じことをする。

Unity 2021.3以降なら追加インストール不要（標準機能）。
**UI Builder**（Window > UI Toolkit > UI Builder）を使えば、UXMLをGUIで
編集しながらUSSの効き具合をプレビューできる。

## 2. セットアップ手順

1. `Assets/UI/` に `.uxml` と `.uss` を置く（サンプルは配置済み）。
2. Projectウィンドウで右クリック > **Create > UI Toolkit > Panel Settings Asset**
   を作成する。これが「Canvas」に相当し、画面解像度のスケーリング設定を持つ。
   - Scale Mode: `Scale With Screen Size`、Reference Resolution: `1080 x 1920`
     などスマホ想定の値にしておく。
3. 空のGameObjectを作り、**UIDocument** コンポーネントを追加する。
   - `Panel Settings` に 2. のアセット
   - `Source Asset` に `Login.uxml`
4. 同じGameObjectに `LoginScreenView.cs` を追加する。
5. Playして表示を確認。UI Builderで開けば見た目を触りながら調整できる。

**日本語フォントは必須の準備**。UI Toolkitの既定フォントは日本語グリフを
持たないため、そのままだと文字が豆腐（□）になる。日本語TTF（Noto Sans JP等）を
インポートし、`Create > Text > Font Asset` でフォントアセットを作って、
USSで指定する。

```css
.screen {
    -unity-font-definition: url("project://database/Assets/Fonts/NotoSansJP SDF.asset");
}
```

## 3. CSS → USS 変換ルール

大半はそのまま動く。書き換えが必要なものだけ挙げる。

| CSS | USS | 備考 |
|---|---|---|
| `display: flex` | （不要） | USSは常にflex。既定は `column` |
| `display: block` | （不要） | 既定で縦積みになる |
| `display: none` | `display: none` | そのまま使える |
| `text-align: center` | `-unity-text-align: middle-center` | 縦横まとめて指定する |
| `font-weight: bold` | `-unity-font-style: bold` | |
| `border: 1px solid #ccc` | `border-width: 1px; border-color: #ccc;` | USSに `border-style` は無い |
| `border-left: 4px solid #x` | `border-left-width` / `border-left-color` | |
| `transform: translateY(2px)` | `translate: 0 2px` | `scale` / `rotate` も同様に独立プロパティ |
| `transition: transform .2s` | `transition-property: translate; transition-duration: 0.2s;` | |
| `position: fixed` | `position: absolute` | モーダル4箇所。親を全画面にすれば同じ見た目になる |
| `width: 100%` + `box-sizing` | `width: 100%` | USSは常にborder-box相当。`box-sizing` 指定は不要 |
| `height: 100vh` | `flex-grow: 1` | 親いっぱいに広げる |
| `:hover` `:active` `:focus` | 同じ | そのまま使える |

### 対応が無く、作り直しが必要なもの

実際に使われている箇所を数えたので、移行時の工数目安にしてほしい。

| CSS | 使用箇所 | 代替手段 |
|---|---|---|
| `box-shadow` | **約48箇所**（全画面） | USSに無い。①薄い `border-color` で代用（サンプルはこれ）②9スライスの影画像を `background-image` にする |
| `display: grid` | 2箇所（stamp.css のスタンプ一覧、mypage.css のアイコン選択） | `flex-direction: row; flex-wrap: wrap;` にして、子要素に `width: 33%` を与える |
| `@media (max-width: ...)` | 3箇所（style.css, register.css, information.css） | USSに無い。Panel SettingsのScale Modeで吸収するか、C#で `panel.visualTree.RegisterCallback<GeometryChangedEvent>` を使い幅に応じてクラスを付け替える |
| `::before` / `::after` | 4箇所（history.css のタイムライン線、stamp.css の「未」表示） | 擬似要素は無い。実体の `<ui:VisualElement>` / `<ui:Label>` をUXMLに足す |
| `gap` | 約8箇所 | 子要素の `margin` で表現する |

### 例: スタンプ一覧のグリッド

```css
/* Web版 (stamp.css) */
.stamp-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 25px 10px; }
```

```css
/* USS */
.stamp-grid { flex-direction: row; flex-wrap: wrap; }
.stamp-item { width: 33%; margin-bottom: 25px; }
```

## 4. C#側の書き換え

ロジックは既存の `Scripts/UI/*.cs` のまま使える。変わるのは
「UI部品をどう掴むか」だけ。

```csharp
// uGUI版 (既存)
[SerializeField] private Button loginButton;   // Inspectorで1つずつドラッグ
private void Awake() {
    loginButton.onClick.AddListener(() => AppShellController.Instance.ShowMap());
}

// UI Toolkit版 (Scripts/UI/Toolkit/LoginScreenView.cs)
private void OnEnable() {
    var root = GetComponent<UIDocument>().rootVisualElement;
    root.Q<Button>("login-button").clicked += () => AppShellController.Instance.ShowMap();
}
```

`Q<T>("name")` はUXMLの `name` 属性で検索する。`document.getElementById()` と同じ感覚。
複数取るなら `Q<Button>(className: "link-button")` や `Query<T>().ToList()`。

`AppShellController` による画面切り替えはそのまま流用できる。UI Toolkitなら
各画面のUIDocumentごとGameObjectをSetActiveするか、
1つのUIDocumentの中でルート要素の `style.display` を切り替える。

## 5. 変換時に判断したこと

元CSSをそのまま移せなかった箇所は、以下のように読み替えている。

- **タイムラインの丸と線**（history.css の `::before` / `::after`）
  → `.timeline-node__dot` / `.timeline-node__line` という実体の
  `VisualElement` をUXMLに置き、`position: absolute` で重ねた。
  最後のノードは `:last-child` が使えないため、UXML側で
  `timeline-node--last` クラスを明示的に付けている。
- **スタンプ一覧とアイコン選択の3列グリッド**（`display: grid`）
  → `flex-direction: row; flex-wrap: wrap;` ＋ 子に `width: 30%`。
- **未獲得スタンプの「未」表示**（`::after { content: "未" }`）
  → USSに `content` は無いので、C#側でLabelのテキストを差し替える方式に変更。
- **モーダル**（`position: fixed`）→ `position: absolute`。
  親を画面いっぱいにしてあるので見た目は同じ。
- **破線**（`border: 1px dashed`）→ USSに `border-style` が無いため実線。
- **`display: inline-block`**（スタンプの訪問日時バッジ、ポップアップの作品名バッジ）
  → `align-self: center` / `align-self: flex-start`。
- **`<input type="file">`**（聖地登録・お問い合わせの画像添付）
  → Unityに相当機能が無いため「画像を選択」ボタンに置き換え、
  実処理は未実装（NativeGallery等のプラグイン導入が必要）。
- **`object-fit: cover`**（ポップアップのスポット画像）
  → `-unity-background-scale-mode: scale-and-crop`。
  画像はC#で `UnityWebRequestTexture` から読み込み `style.backgroundImage` に入れる。

## 6. 残っている作業

1. **シーンへの配置と表示確認**。UIDocument・PanelSettings・日本語フォントを
   設定して、各画面が崩れずに出るか確認する（Unity Editorが必要なため未検証）。
2. **地図HUDと地図本体の接続**。`MapHudView` は検索・絞り込みをイベント
   （`OnSearchSubmitted` / `OnFilterApplied` / `OnFilterReset`）で外に出すだけなので、
   `PilgrimageMapController` 側でこれを購読する処理を書く必要がある。
   現状の `PilgrimageMapController` はuGUI版の
   `SpotPopupPanel` / `AnimeFilterPanel` / `RoutePanelController` を参照しているため、
   UI Toolkitに寄せるならこの参照を `MapHudView` 1つに差し替える。
3. **uGUI版の削除**。UI Toolkit版で問題なく動くことを確認できたら、
   `Scripts/UI/*.cs`（uGUI版）は不要になる。移行中は両方残してある。
4. **`box-shadow` の見た目**。今は薄い枠線で代用しているので、
   影を再現したい場合は9スライス画像を用意する。

注意: 地図描画そのものはUI Toolkitでは行わない。`FlatMapView` はuGUIの
RectTransformベースなので、地図とHUDは別レイヤーになる
（UIDocumentのPanel SettingsのSort OrderでuGUIのCanvasとの前後を調整する）。
