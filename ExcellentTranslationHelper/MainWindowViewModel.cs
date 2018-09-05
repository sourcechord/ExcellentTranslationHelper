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

    public enum InputType
    {
        Json,
        Xlsx,
        Resx,
    }

    public enum OutputType
    {
        Json,
        Xlsx,
        Resx,
    }

    class MainWindowViewModel : BindableBase
    {
        #region 各種ダイアログ用のメッセージ定義

        #endregion

        private TranslationData _translationData;

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


        private InputType inputType;
        public InputType InputType
        {
            get { return inputType; }
            set { this.SetProperty(ref this.inputType, value); OnInputTypeChanged(); }
        }

        /// <summary>
        /// InputTypeプロパティ更新時の処理
        /// </summary>
        private void OnInputTypeChanged()
        {
            this.SourcePath = string.Empty;
        }


        private OutputType outputType;
        public OutputType OutputType
        {
            get { return outputType; }
            set { this.SetProperty(ref this.outputType, value); }
        }


        private RelayCommand selectSourceCommand;
        public RelayCommand SelectSourceCommand
        {
            get { return selectSourceCommand = selectSourceCommand ?? new RelayCommand(SelectSource); }
        }
        private void SelectSource()
        {
            // コンボボックスの選択状態に合わせて
            // 変換元選択処理を、フォルダ選択/xlsxファイル選択、のどちらかに切り替える
            switch (this.InputType)
            {
                case InputType.Json:
                case InputType.Resx:
                    this.SelectDirectory();
                    break;
                case InputType.Xlsx:
                    this.SelectXlsxFile();
                    break;
                default:
                    break;
            }

            // 指定されたソースの情報を読み込む
            IConverter converter = null;
            switch (this.InputType)
            {
                case InputType.Json:
                    converter = new JsonConverter();
                    break;
                case InputType.Xlsx:
                    converter = new ExcelConverter();
                    break;
                case InputType.Resx:
                    converter = new ResxConverter();
                    break;
                default:
                    break;
            }

            this._translationData = converter.Read(this.SourcePath);
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
            var path = string.Empty;
            switch (this.OutputType)
            {
                case OutputType.Xlsx:
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

                    path = dlg.FileName;
                    break;
                }
                case OutputType.Json:
                case OutputType.Resx:
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

                    path = dlg.FileName;
                    break;
                }
                default:
                    break;
            }

            // 変換処理
            IConverter converter = null;
            switch (this.OutputType)
            {
                case OutputType.Json:
                    converter = new JsonConverter();
                    break;
                case OutputType.Xlsx:
                    converter = new ExcelConverter();
                    break;
                case OutputType.Resx:
                    converter = new ResxConverter();
                    break;
                default:
                    break;
            }

            converter.Write(this._translationData, path);

        }
        private bool CanConvert()
        {
            return !string.IsNullOrEmpty(this.sourcePath); 
        }
    }
}
