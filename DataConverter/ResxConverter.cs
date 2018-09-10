using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Excellent.DataConverter
{
    public class ResxConverter : IConverter
    {
        // resxファイル名の定義
        private static string ResxFilename = "Resources*.resx";


        /// <summary>
        /// 引数のパスで指定されたファイルを読み込み、ローカライズ用文字列の配列を返します。
        /// </summary>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        public TranslationData Read(string srcPath)
        {
            var translation = new TranslationData();

            // TODO: 指定フォルダ内のファイル構造をチェック
            // フォルダ指定かどうか？localeというフォルダ名か？__lang__/translation.jsonという構造になっているか？
            var fileList = Directory.GetFiles(srcPath, ResxFilename, SearchOption.AllDirectories)
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
            var name = Path.GetFileNameWithoutExtension(file.Name);
            var sp = name.Split('.');
            var lang = sp.Length > 1 ? sp[1] : "dev";
            var list = new LanguageData(lang);

            using (var resxReader = new ResXResourceReader(file.FullName))
            {
                foreach (DictionaryEntry entry in resxReader)
                {
                    var key = entry.Key as string;
                    var split = key.Split('_');
                    var ns = split.TakeWhile((item, index) => index < split.Length - 1);
                    var k = split.Last();
                    var data = ns.Count() > 0 ? new LocalizationItem(string.Join(".", ns), k, (string)entry.Value) :
                                                  new LocalizationItem(null, k, (string)entry.Value);

                    list.Add(data);
                }
            }

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
                var isDefaultLang = lang.Locale == "dev";
                var fileName = isDefaultLang ? "Resources.resx" : $"Resources.{lang.Locale}.resx";
                var filePath = Path.Combine(dstPath, fileName);

                this.WriteSingleFile(lang, filePath);
            }
        }

        private bool WriteSingleFile(LanguageData data, string dstPath)
        {
            var groups = data.GroupBy(o => o.Namespace);

            using (var resxWriter = new ResXResourceWriter(dstPath))
            {
                foreach (var item in data)
                {
                    var ns = item.Namespace.Replace('.', '_');
                    resxWriter.AddResource($"{ns}_{item.Key}", item.Value);
                }
            }

            return true;
        }
    }
}
