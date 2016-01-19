using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Excellent.DataConverter
{
    /// <summary>
    /// ローカライズ文字列の各項目データを保持するクラス
    /// </summary>
    public class LocalizationItem
    {
        public string Namespace { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public LocalizationItem(string ns, string key, string value)
        {
            this.Namespace = ns;
            this.Key = key;
            this.Value = value;
        }
    }


    /// <summary>
    /// ひとつの言語に対応するローカライズデータをまとめて持つクラス
    /// </summary>
    /// <remarks>
    /// JSONファイルと1対1で対応する情報を保持します。
    /// </remarks>
    public class LanguageData : List<LocalizationItem>
    {
        /// <summary>
        /// ロケール情報を取得または設定します。
        /// </summary>
        public string Locale { get; set; }

        public LanguageData(string locale)
        {
            this.Locale = locale;
        }

        public LanguageData(string locale, IEnumerable<LocalizationItem> list)
            : base(list)
        {
            this.Locale = locale;
        }
    }


    /// <summary>
    /// 全言語分のローカライズデータを保持するクラス
    /// </summary>
    public class TranslationData : List<LanguageData>
    {
        public TranslationData()
        {
        }

        public TranslationData(IEnumerable<LanguageData> list)
            : base(list)
        {
        }
    }
}
