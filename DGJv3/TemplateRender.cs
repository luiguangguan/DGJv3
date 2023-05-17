using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    internal class TemplateRender
    {
        /// <summary>
        /// 模板
        /// </summary>
        public Template Template { get; set; }

        /// <summary>
        /// 模板输出的信息
        /// </summary>
        public string Text { get; set; }
    }
}
