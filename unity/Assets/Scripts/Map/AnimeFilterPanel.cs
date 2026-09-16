using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.Map
{
    /// <summary>
    /// 「アニメで絞り込み」モーダル。
    /// Web版 Script.js の openFilter/populateAnimeList/searchAnime/
    /// applyFilter/resetFilter を移植したもの。
    /// </summary>
    public class AnimeFilterPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InputField searchInput;
        [SerializeField] private RectTransform checkboxListContainer;
        [SerializeField] private GameObject checkboxItemPrefab; // Toggle + 子にText
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        public event Action<List<string> /*selectedTitles*/, string /*keyword*/> OnApply;
        public event Action OnReset;

        private readonly List<(Toggle toggle, Text label, string title)> items = new List<(Toggle, Text, string)>();

        private void Awake()
        {
            searchInput.onValueChanged.AddListener(FilterVisibleLabels);
            applyButton.onClick.AddListener(HandleApply);
            resetButton.onClick.AddListener(HandleReset);
            closeButton.onClick.AddListener(Close);

            panelRoot.SetActive(false);
        }

        public void Open(IEnumerable<string> availableTitles)
        {
            PopulateList(availableTitles);
            searchInput.text = string.Empty;
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private void PopulateList(IEnumerable<string> availableTitles)
        {
            foreach (Transform child in checkboxListContainer)
            {
                Destroy(child.gameObject);
            }
            items.Clear();

            foreach (var title in availableTitles.Where(t => !string.IsNullOrEmpty(t)))
            {
                var go = Instantiate(checkboxItemPrefab, checkboxListContainer);
                var toggle = go.GetComponentInChildren<Toggle>();
                var label = go.GetComponentInChildren<Text>();

                toggle.isOn = false;
                label.text = title;

                items.Add((toggle, label, title));
            }
        }

        private void FilterVisibleLabels(string rawKeyword)
        {
            var keyword = SearchController.NormalizeText(rawKeyword);

            foreach (var (_, label, title) in items)
            {
                var visible = SearchController.NormalizeText(title).Contains(keyword);
                label.transform.parent.gameObject.SetActive(visible || string.IsNullOrEmpty(keyword));
            }
        }

        private void HandleApply()
        {
            var selectedTitles = items.Where(i => i.toggle.isOn).Select(i => i.title).ToList();
            OnApply?.Invoke(selectedTitles, searchInput.text.Trim());
            Close();
        }

        private void HandleReset()
        {
            searchInput.text = string.Empty;
            foreach (var (toggle, _, _) in items)
            {
                toggle.isOn = false;
            }

            OnReset?.Invoke();
        }
    }
}
