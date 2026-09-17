using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Pilgrimage.UI.Toolkit
{
    /// <summary>
    /// 獲得スタンプ一覧 (UI Toolkit版)。uGUI版 StampScreen.cs と同じ処理。
    /// 一覧の各項目は StampItem.uxml を複製して作る
    /// (uGUIでPrefabをInstantiateしていたのと同じ役割)。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StampScreenView : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset stampItemTemplate;

        private VisualElement stampGrid;
        private VisualElement filterDropdown;
        private VisualElement filterCheckboxContainer;
        private VisualElement detailModal;
        private Label modalTitle;
        private Label modalDate;
        private Label modalAnime;
        private Label modalScene;
        private Label modalAddress;

        private List<StampRecord> allStamps = new List<StampRecord>();
        private readonly List<Toggle> filterToggles = new List<Toggle>();

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            stampGrid = root.Q<VisualElement>("stamp-grid");
            filterDropdown = root.Q<VisualElement>("filter-dropdown");
            filterCheckboxContainer = root.Q<VisualElement>("filter-checkbox-container");
            detailModal = root.Q<VisualElement>("detail-modal");
            modalTitle = root.Q<Label>("modal-title");
            modalDate = root.Q<Label>("modal-date");
            modalAnime = root.Q<Label>("modal-anime");
            modalScene = root.Q<Label>("modal-scene");
            modalAddress = root.Q<Label>("modal-address");

            filterDropdown.style.display = DisplayStyle.None;
            detailModal.style.display = DisplayStyle.None;

            root.Q<Button>("back-button").clicked += () => AppShellController.Instance.ShowMyPage();
            root.Q<Button>("modal-close-button").clicked += () => detailModal.style.display = DisplayStyle.None;

            root.Q<Button>("filter-toggle-button").clicked += () =>
                filterDropdown.style.display = filterDropdown.style.display == DisplayStyle.None
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            root.Q<Button>("filter-reset-button").clicked += () =>
            {
                foreach (var toggle in filterToggles)
                {
                    toggle.value = false;
                }

                RenderStamps(null);
                filterDropdown.style.display = DisplayStyle.None;
            };

            root.Q<Button>("filter-apply-button").clicked += () =>
            {
                var selected = filterToggles.Where(t => t.value).Select(t => t.label).ToList();
                RenderStamps(selected.Count > 0 ? selected : null);
                filterDropdown.style.display = DisplayStyle.None;
            };

            // 画面を開くたびに端末の最新データを読み直す (Web版のlocalStorage読み込みと同じ)
            var store = new LocalStampStore();
            allStamps = store.Records.Values.OrderByDescending(r => r.StampedAtUnixMs).ToList();

            BuildFilterMenu();
            RenderStamps(null);
        }

        private void BuildFilterMenu()
        {
            filterCheckboxContainer.Clear();
            filterToggles.Clear();

            var animeNames = allStamps
                .Select(s => s.TitleName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct();

            foreach (var anime in animeNames)
            {
                var toggle = new Toggle(anime);
                filterCheckboxContainer.Add(toggle);
                filterToggles.Add(toggle);
            }
        }

        private void RenderStamps(List<string> filterAnimeNames)
        {
            stampGrid.Clear();

            var visible = filterAnimeNames == null
                ? allStamps
                : allStamps.Where(s => filterAnimeNames.Contains(s.TitleName)).ToList();

            foreach (var stamp in visible)
            {
                var item = stampItemTemplate.Instantiate();
                item.Q<Label>("stamp-label").text = stamp.SpotName;

                var button = item.Q<Button>("stamp-item");
                button.clicked += () => OpenDetailModal(stamp);

                stampGrid.Add(item);
            }
        }

        private void OpenDetailModal(StampRecord stamp)
        {
            modalTitle.text = stamp.SpotName;
            modalDate.text = $"訪問日時：{stamp.DateLabel}";
            modalAnime.text = stamp.TitleName;
            modalScene.text = stamp.SceneText;
            modalAddress.text = string.IsNullOrEmpty(stamp.Address) ? "住所未登録" : stamp.Address;

            detailModal.style.display = DisplayStyle.Flex;
        }
    }
}
