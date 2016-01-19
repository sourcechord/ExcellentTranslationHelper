using BaseLibrary.Common;
using Excellent.DataConverter;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Excellent.TranslationHelper
{
    public enum ConvertType
    {
        JsonToXlsx,
        XlsxToJson,
    }

    class MainWindowViewModel : BindableBase
    {
        #region 各種ダイアログ用のメッセージ定義

        #endregion

        private string sourcePath;
        /// <summary>
        /// 変換元のパスを取得または設定します。
        /// </summary>
        public string SourcePath
        {
            get { return sourcePath; }
            set { this.SetProperty(ref this.sourcePath, value); OnSourcePathChanged(); }
        }

        /// <summary>
        /// SourcePathプロパティ更新時の処理
        /// </summary>
        private void OnSourcePathChanged()
        {
            this.ConvertCommand.RaiseCanExecuteChanged();
        }


        private ConvertType selectedType;
        /// <summary>
        /// 変換処理のタイプを取得または設定します。
        /// </summary>
        public ConvertType SelectedType
        {
            get { return selectedType; }
            set { this.SetProperty(ref this.selectedType, value); OnSelectedTypeChanged(); }
        }

        private void OnSelectedTypeChanged()
        {
            // 変換タイプを変更されたので、
            // 変換元のパスをクリアする。
            this.SourcePath = string.Empty;

        }




        private RelayCommand selectSourceCommand;
        public RelayCommand SelectSourceCommand
        {
            get { return selectSourceCommand = selectSourceCommand ?? new RelayCommand(SelectSource); }
        }
        private void SelectSource()
        {
            // コンボボックスの選択状態に合わせて
            // 変換元を、フォルダ選択/xlsxファイル選択、のどちらかに切り替える
            switch (this.SelectedType)
            {
                case ConvertType.JsonToXlsx:
                    this.SelectDirectory();
                    break;
                case ConvertType.XlsxToJson:
                    this.SelectXlsxFile();
                    break;
                default:
                    break;
            }
        }


        private void SelectDirectory()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.AllowNonFileSystemItems = false;

            var result = dlg.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
            {
                return;
            }

            this.SourcePath = dlg.FileName;
        }

        private void SelectXlsxFile()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.AllowNonFileSystemItems = false;
            dlg.Filters.Add(new CommonFileDialogFilter("xlsx files", "*.xlsx"));
            var result = dlg.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
            {
                return;
            }

            this.SourcePath = dlg.FileName;
        }


        private RelayCommand convertCommand;
        public RelayCommand ConvertCommand
        {
            get { return convertCommand = convertCommand ?? new RelayCommand(Convert, CanConvert); }
        }
        private void Convert()
        {
            // コンボボックスの選択状態に合わせて
            // 変換先を、フォルダ選択/xlsxファイル選択、のどちらかに切り替える
            switch (this.SelectedType)
            {
                case ConvertType.JsonToXlsx:
                    this.ConvertToXlsx();
                    break;
                case ConvertType.XlsxToJson:
                    this.ConvertToJson();
                    break;
                default:
                    break;
            }

        }
        private bool CanConvert()
        {
            return !string.IsNullOrEmpty(this.sourcePath); 
        }


        private void ConvertToXlsx()
        {
            var dlg = new CommonSaveFileDialog();
            dlg.EnsureReadOnly = false;
            dlg.Filters.Add(new CommonFileDialogFilter("xlsx files", "*.xlsx"));
            dlg.DefaultExtension = ".xlsx";
            dlg.AlwaysAppendDefaultExtension = true;    // 必ずデフォルトの拡張子をつけるように制限

            var result = dlg.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
            {
                return;
            }

            var path = dlg.FileName;

            // コンバーターを作り、変換する
            var srcConverter = new JsonConverter();
            var data = srcConverter.Read(this.SourcePath);


            var converter = new ExcelConverter();
            converter.Write(data, path);
        }

        private void ConvertToJson()
        {
            // JSON出力時はフォルダを選択して、そこにlocalesというフォルダを書き出す。
            // ⇒保存だけど、CommonOpenFileDialogを使用する。
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;
            dlg.AllowNonFileSystemItems = false;

            var result = dlg.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
            {
                return;
            }

            var path = dlg.FileName;

            // コンバーターを作り、変換する
            var srcConverter = new ExcelConverter();
            var data = srcConverter.Read(this.SourcePath);


            var converter = new JsonConverter();
            converter.Write(data, path);
        }
    }
}
