using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Excellent.DataConverter
{
    public class JsonConverter : IConverter
    {
        // JSONファイル名の定義
        private static string JsonFilename = "translation.json";


        /// <summary>
        /// 引数のパスで指定されたファイルを読み込み、ローカライズ用文字列の配列を返します。
        /// </summary>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        public TranslationData Read(string srcPath)
        {
            // 各種入力チェック
            // フォルダ指定かどうか？localeというフォルダ名か？__lang__/translation.jsonという構造になっているか？


            // JSONのパース時の例外チェック

            var translation = new TranslationData();


            // TODO: 指定フォルダ内のツリー構造をチェック
            // フォルダ指定かどうか？localeというフォルダ名か？__lang__/translation.jsonという構造になっているか？
            var fileList = Directory.GetFiles(srcPath, JsonFilename, SearchOption.AllDirectories)
                                    .Select(o => new FileInfo(o));

            foreach (var file in fileList)
            {
                var lang = this.ReadSingleFile(file);
                translation.Add(lang);
            }

            return translation;
        }

        /// <summary>
        /// 特定の言語のjsonファイルをパースし、LanguageData形式として読み取ります。
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private LanguageData ReadSingleFile(FileInfo file)
        {
            var dirName = file.Directory.Name;
            var list = new LanguageData(dirName);

            var obj = JObject.Parse(File.ReadAllText(file.FullName));
            Traverse(obj, ref list);

            return list;
        }

        public void Write(TranslationData src, string dstPath)
        {
            // 全項目を、ロケールを含んだアイテムに射影
            var edited = src.SelectMany(o => o.Select(item => new { o.Locale, item.Namespace, item.Key, item.Value }));

            // プロパティ名完全一致の項目同市でグルーピング
            var groupedByNS = edited.GroupBy(o => Tuple.Create(o.Namespace, o.Key),
                                             o => new { o.Locale, o.Value });

            // TODO: ★出力先ディレクトリに、すでに同名フォルダがあったりしないかチェック。

            foreach (var lang in src)
            {
                var langPath = Path.Combine(dstPath, lang.Locale);
                Directory.CreateDirectory(langPath);
                langPath = Path.Combine(langPath, JsonFilename);

                this.WriteSingleFile(lang, langPath);
            }

        }

        private bool WriteSingleFile(LanguageData data, string dstPath)
        {
            var root = new JObject();

            var groups = data.GroupBy(o => o.Namespace);
            foreach (var g in groups)
            {
                var namespaces = g.Key.Split('.');

                var prev = new JObject(g.Select(o => new JProperty(o.Key, o.Value)));
                foreach (var ns in namespaces.Reverse())
                {
                    var temp = new JObject(new JProperty(ns, prev));
                    prev = temp;
                }

                // 各グループ要素を、全体のJSONオブジェクトにマージ
                root.Merge(prev);
            }

            File.WriteAllText(dstPath, root.ToString());

            return true;
        }


        #region JSONファイル解析で使用する各種ヘルパーメソッド
        /// <summary>
        /// JSONのツリーを探索して、一つの言語分のデータをLangageData型のインスタンスとして作成します。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="list"></param>
        private static void Traverse(JObject obj, ref LanguageData list)
        {
            foreach (var item in obj)
            {
                var value = item.Value;
                // itemがツリー構造の枝/葉のどちらか判定
                var isLeaf = value.Type != JTokenType.Object;

                if (isLeaf)
                {
                    var ns = TrimEnd(value.Path, "." + item.Key);

                    var str = value.Type != JTokenType.Array ? value.ToString() : value.ToString(Formatting.None);
                    var data = new LocalizationItem(ns, item.Key, str);
                    list.Add(data);
                }
                else
                {
                    Traverse((JObject)value, ref list);
                }
            }
        }


        /// <summary>
        /// string型から、文字列を指定して終端からTrimします。
        /// </summary>
        /// <param name="target"></param>
        /// <param name="trimString"></param>
        /// <returns></returns>
        private static string TrimEnd(string target, string trimString)
        {
            string result = target;
            while (result.EndsWith(trimString))
            {
                result = result.Substring(0, result.Length - trimString.Length);
            }

            return result;
        }

        #endregion
    }
}
