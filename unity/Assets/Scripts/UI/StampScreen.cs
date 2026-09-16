using System.Collections.Generic;
using System.Linq;
using Pilgrimage.Data;
using Pilgrimage.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>
    /// 獲得スタンプ一覧画面。Web版 stamp.js を移植。
    /// 端末に保存されたスタンプ履歴 (LocalStampStore) を作品名で絞り込んで
    /// グリッド表示し、タップすると詳細モーダルを開く。
    /// </summary>
    public class StampScreen : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button filterToggleButton;
        [SerializeField] private GameObject filterDropdown;
        [SerializeField] private RectTransform filterCheckboxContainer;
        [SerializeField] private GameObject filterCheckboxPrefab; // Toggle + Text
        [SerializeField] private Button filterApplyButton;
        [SerializeField] private Button filterResetButton;

        [SerializeField] private RectTransform stampGridContainer;
        [SerializeField] private GameObject stampItemPrefab; // Text (絵文字丸) + Text (名前) + Button

        [Header("詳細モーダル")]
        [SerializeField] private GameObject detailModal;
        [SerializeField] private Text modalTitleText;
        [SerializeField] private Text modalDateText;
        [SerializeField] private Text modalAnimeText;
        [SerializeField] private Text modalSceneText;
        [SerializeField] private Text modalAddressText;
        [SerializeField] private Button closeModalButton;

        private LocalStampStore stampStore;
        private List<StampRecord> allStamps = new List<StampRecord>();
        private readonly List<Toggle> filterToggles = new List<Toggle>();

        private void Awake()
        {
            backButton.onClick.AddListener(() => AppShellController.Instance.ShowMyPage());
            filterToggleButton.onClick.AddListener(() => filterDropdown.SetActive(!filterDropdown.activeSelf));
            closeModalButton.onClick.AddListener(() => detailModal.SetActive(false));

            filterResetButton.onClick.AddListener(() =>
            {
                foreach (var toggle in filterToggles) toggle.isOn = false;
                RenderStamps(null);
                filterDropdown.SetActive(false);
            });

            filterApplyButton.onClick.AddListener(() =>
            {
                var selected = filterToggles.Where(t => t.isOn).Select(t => t.GetComponentInChildren<Text>().text).ToList();
                RenderStamps(selected.Count > 0 ? selected : null);
                filterDropdown.SetActive(false);
            });

            detailModal.SetActive(false);
            filterDropdown.SetActive(false);
        }

        private void OnEnable()
        {
            // Web版が画面を開くたびlocalStorageを読み直すのと同様、
            // 表示のたびに最新の端末データを読み込む。
            stampStore = new LocalStampStore();
            allStamps = stampStore.Records.Values.OrderByDescending(r => r.StampedAtUnixMs).ToList();

            BuildFilterMenu();
            RenderStamps(null);
        }

        private void BuildFilterMenu()
        {
            foreach (Transform child in filterCheckboxContainer)
            {
                Destroy(child.gameObject);
            }
            filterToggles.Clear();

            var animeNames = allStamps.Select(s => s.TitleName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

            foreach (var anime in animeNames)
            {
                var go = Instantiate(filterCheckboxPrefab, filterCheckboxContainer);
                var toggle = go.GetComponentInChildren<Toggle>();
                var label = go.GetComponentInChildren<Text>();
                label.text = anime;
                filterToggles.Add(toggle);
            }
        }

        private void RenderStamps(List<string> filterAnimeNames)
        {
            foreach (Transform child in stampGridContainer)
            {
                Destroy(child.gameObject);
            }

            var visible = filterAnimeNames == null
                ? allStamps
                : allStamps.Where(s => filterAnimeNames.Contains(s.TitleName)).ToList();

            foreach (var stamp in visible)
            {
                var go = Instantiate(stampItemPrefab, stampGridContainer);
                var texts = go.GetComponentsInChildren<Text>();
                if (texts.Length > 1)
                {
                    texts[1].text = stamp.SpotName;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OpenDetailModal(stamp));
                }
            }
        }

        private void OpenDetailModal(StampRecord stamp)
        {
            modalTitleText.text = stamp.SpotName;
            modalDateText.text = $"訪問日時：{stamp.DateLabel}";
            modalAnimeText.text = stamp.TitleName;
            modalSceneText.text = stamp.SceneText;
            modalAddressText.text = string.IsNullOrEmpty(stamp.Address) ? "住所未登録" : stamp.Address;

            detailModal.SetActive(true);
        }
    }
}
