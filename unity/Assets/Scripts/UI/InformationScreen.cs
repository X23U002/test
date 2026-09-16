using UnityEngine;
using UnityEngine.UI;

namespace Pilgrimage.UI
{
    /// <summary>お問い合わせ画面。Web版 information.js を移植。</summary>
    public class InformationScreen : MonoBehaviour
    {
        [SerializeField] private GameObject formPage;
        [SerializeField] private GameObject completePage;
        [SerializeField] private InputField contactTextInput;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button backToMapButton;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => AppShellController.Instance.ShowMap());
            backToMapButton.onClick.AddListener(() => AppShellController.Instance.ShowMap());
            sendButton.onClick.AddListener(SendContact);
        }

        private void OnEnable()
        {
            formPage.SetActive(true);
            completePage.SetActive(false);
        }

        private void SendContact()
        {
            // TODO: 実際にはFirestore等へ問い合わせ内容を送信する
            formPage.SetActive(false);
            completePage.SetActive(true);
        }
    }
}
