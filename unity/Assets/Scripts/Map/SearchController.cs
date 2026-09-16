using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pilgrimage.Data;

namespace Pilgrimage.Map
{
    /// <summary>
    /// スポット名・作品名・シーン説明のあいまい検索。
    /// Web版 Script.js の normalizeText() / searchCustomSpots() を移植したもの。
    /// 全角英数→半角、カタカナ→ひらがな、記号除去をしてから部分一致で検索する。
    ///
    /// 住所や駅名など、Firestoreに無い一般的な場所の検索については
    /// Web版はMapbox Geocoding APIに委譲していた。Unity側で同等の検索を
    /// 行いたい場合は Mapbox/Google の Geocoding REST API を
    /// UnityWebRequestで呼び出すアダプタを別途追加すること。
    /// </summary>
    public static class SearchController
    {
        public static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);

            foreach (var ch in text.ToLowerInvariant())
            {
                var c = ch;

                // 全角英数字・記号 -> 半角
                if (c >= 0xFF01 && c <= 0xFF5E)
                {
                    c = (char)(c - 0xFEE0);
                }

                // 全角カタカナ -> ひらがな
                if (c >= 'ァ' && c <= 'ヶ')
                {
                    c = (char)(c - 0x60);
                }

                // 記号・空白の除去 (Web版と同じ対象文字)
                if (" 　・！!？?△○〇-ー".IndexOf(c) >= 0)
                {
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        public static List<SpotData> Search(string query, IReadOnlyList<SpotData> allSpots)
        {
            var keyword = NormalizeText(query);

            if (string.IsNullOrEmpty(keyword))
            {
                return new List<SpotData>();
            }

            return allSpots.Where(spot =>
                NormalizeText(spot.SpotName).Contains(keyword) ||
                NormalizeText(spot.TitleName).Contains(keyword) ||
                NormalizeText(spot.SpotInfo).Contains(keyword)
            ).ToList();
        }
    }
}
