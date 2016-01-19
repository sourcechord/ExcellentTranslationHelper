using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClosedXML.Excel;

namespace Excellent.DataConverter
{
    public class ExcelConverter : IConverter
    {
        // シートの名前
        private static string SheetName = "translation";


        private static int OffsetRow = 1;
        private static int OffsetColumn = 1;
        // エクセル・シート上の、各データを配置する座標などを定義
        private static int LanguageTitleRow = OffsetRow + 1;
        private static int DataRow = OffsetRow + 2;
        private static int DataColumn = OffsetColumn + 2;

        // シートのデザイン用
        private static XLBorderStyleValues MidBorder = XLBorderStyleValues.Medium;
        private static XLBorderStyleValues DottedBorder = XLBorderStyleValues.Dotted;
        private static XLBorderStyleValues NamespaceSeparatorBorder = XLBorderStyleValues.Thin;

        public TranslationData Read(string srcPath)
        {
            // ワークブックの作成
            using (var workbook = new XLWorkbook(srcPath))
            {
                // ワークブック指定の名前のシートを取得
                var worksheet = workbook.Worksheet(ExcelConverter.SheetName);


                var dataRange = worksheet.Range(LanguageTitleRow,
                                                DataColumn,
                                                worksheet.LastRowUsed().RowNumber(),
                                                worksheet.LastColumnUsed().ColumnNumber());

                var dataTable = dataRange.AsTable();
                var langList = dataTable.DataRange.Columns()
                                                  .Select(o => new
                                                  {
                                                      Locale = o.Cell(0).GetString(),
                                                      Values = o.Cells().Select(i => new LocalizationItem(worksheet.Cell(i.Address.RowNumber, OffsetColumn).GetString(),
                                                                                                          worksheet.Cell(i.Address.RowNumber, OffsetColumn + 1).GetString(),
                                                                                                          i.GetString()))
                                                  });


                var result = langList.Select(o => new LanguageData(o.Locale, o.Values));

                return new TranslationData(result);
            }
        }

        public void Write(TranslationData src, string dstPath)
        {
            // TODO: 出力先の拡張子チェック
            var ext = System.IO.Path.GetExtension(dstPath);
            if (ext != ".xlsx")
            {
                throw new ArgumentException("Invalid extension.");
            }

            try
            {
                // ワークブックの作成
                using (var workbook = new XLWorkbook())
                {
                    // ワークブックにシートを追加
                    var worksheet = workbook.Worksheets.Add(ExcelConverter.SheetName);

                    // 「dev」ロケールを先頭に持ってくる
                    var sorted = src.OrderBy(o => o.Locale != "dev");

                    // 全キーのリストを作る
                    var allKeys = sorted.SelectMany(o => o.Select(item => Tuple.Create(item.Namespace, item.Key)))
                                     .Distinct()
                                     .OrderBy(o => o);

                    // 全体のヘッダー領域を作成
                    this.CreateHeader(worksheet, sorted.Count());


                    // プロパティ名などの列を作成
                    this.CreateKeyColumns(worksheet, allKeys);

                    var columnPos = DataRow;


                    // 各言語用のデータを書き込み
                    foreach (var lang in sorted)
                    {
                        // 言語名を書く
                        worksheet.Cell(LanguageTitleRow, columnPos).Value = lang.Locale;


                        // 各言語の全項目を書く
                        var temp = allKeys.GroupJoin(lang,
                                                     o => o,
                                                     i => Tuple.Create(i.Namespace, i.Key),
                                                     (o, i) => new { Key = o, Value = i.Select(e => e.Value).FirstOrDefault() ?? "" });

                        // ↓コイツを書き込めばOK
                        var result = temp.Select(o => o.Value).AsEnumerable();

                        var startCell = worksheet.Cell(DataRow, columnPos);
                        startCell.Value = result;
                        columnPos++;
                    }


                    var langCount = sorted.Count();

                    // 罫線を描画
                    this.DrawBorder(worksheet, allKeys, langCount);


                    worksheet.Range(OffsetRow, OffsetColumn, DataRow + allKeys.Count() - 1, DataColumn + langCount - 1)
                             .Style.Border.SetOutsideBorder(MidBorder);


                    // カラム幅を自動調整
                    worksheet.Columns().AdjustToContents();

                    // ワークブックの保存
                    workbook.SaveAs(dstPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                throw;
            }

        }


        #region シートの装飾を行うためのメソッド
        
        private void CreateHeader(IXLWorksheet ws, int langCount)
        {
            // Namespace/Keyのヘッダを装飾
            var nsRange = ws.Range(OffsetRow, OffsetColumn, OffsetRow + 1, OffsetColumn + 1);
            nsRange.Style.Border.SetBottomBorder(MidBorder);

            ws.Cell(OffsetRow + 1, OffsetRow).Value = "Namespace";
            ws.Cell(OffsetRow + 1, OffsetColumn + 1).Value = "Key";

            // 言語のヘッダを装飾
            var langRange = ws.Range(OffsetRow, DataColumn, OffsetRow + 1, DataColumn + langCount - 1);
            langRange.Style.Border.SetBottomBorder(MidBorder)
                           .Border.SetOutsideBorder(MidBorder);

            ws.Range(OffsetRow, DataColumn, OffsetRow, DataColumn + langCount - 1)
              .Merge()
              .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(OffsetRow, DataColumn).Value = "Languages";

            ws.Range(LanguageTitleRow, DataColumn, LanguageTitleRow, DataColumn + langCount - 1)
              .Style.Border.SetRightBorder(DottedBorder);


            // ヘッダ全体を装飾
            var range = ws.Range(OffsetRow, OffsetColumn, LanguageTitleRow, DataColumn + langCount - 1);
            range.Style.Fill.SetBackgroundColor(XLColor.LightBlue)
                       .Border.SetBottomBorder(MidBorder)
                       .Border.SetOutsideBorder(MidBorder)
                       .Font.SetBold()
                       .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        private void CreateKeyColumns(IXLWorksheet ws, IEnumerable<Tuple<string, string>> allKeys)
        {
            // プロパティ名などの列を作成
            var keysColumn = ws.Cell(DataRow, OffsetColumn);
            keysColumn.Value = allKeys.Select(o => new { o.Item1, o.Item2 });

            // プロパティ名の装飾
            var propRange = ws.Range(DataRow, OffsetColumn, DataRow + allKeys.Count() - 1, 2);
            propRange.Style.Fill.SetBackgroundColor(XLColor.LightYellow)
                            .Border.SetOutsideBorder(MidBorder);
        }

        private void DrawBorder(IXLWorksheet ws, IEnumerable<Tuple<string, string>> allKeys, int langCount)
        {
            var range = ws.Range(DataRow, DataColumn, DataRow + allKeys.Count() - 1, DataColumn + langCount - 1);
            range.Style.Border.SetRightBorder(DottedBorder)
                       .Border.SetBottomBorder(DottedBorder);


            // Namespaceの区切りごとに直線を引く
            // プロパティ名完全一致の項目同士でグルーピング
            var groupedByNS = allKeys.GroupBy(o => o.Item1);

            var pos = DataRow;
            foreach (var item in groupedByNS)
            {
                var count = item.Count();
                pos += count;
                ws.Range(pos, OffsetColumn, pos, OffsetColumn + 2 + langCount - 1)
                  .Style.Border.SetTopBorder(NamespaceSeparatorBorder);
            }
        }

        #endregion
    }
}
