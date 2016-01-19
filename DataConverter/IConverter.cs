using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Excellent.DataConverter
{
    /// <summary>
    /// すべてのコンバーターで共通で提供するメソッドの定義
    /// </summary>
    public interface IConverter
    {
        TranslationData Read(string srcPath);

        void Write(TranslationData src, string dstPath);
    }
}
