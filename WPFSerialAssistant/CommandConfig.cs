// CommandConfig.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFSerialAssistant
{
    public class CommandConfig
    {
        public List<List<MainWindow.CommandItem>> AllPagesCommands { get; set; }
        public int CurrentPageIndex { get; set; } = 0; // 默认显示第一页
        public List<string> PageNames { get; set; } = new List<string>(); // 新增属性存储页面名称
    }
}
