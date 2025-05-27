using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace WPFSerialAssistant
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private StringBuilder _batchBuffer = new StringBuilder(); // 合并短时间内的数据
        private DispatcherTimer _batchTimer = new DispatcherTimer(); // 批量处理定时器
        private readonly object _batchLock = new object(); // 线程锁

        private static readonly string ConfigFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WPFSerialAssistant");

        private static readonly string CommandsFilePath =
            Path.Combine(ConfigFolder, "commands.json");

        // 保存命令列表到JSON文件
        private void SaveCommands()
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder); // 确保文件夹存在
                var commands = cmdListBox.Items.Cast<CommandItem>().ToList();
                string json = JsonConvert.SerializeObject(commands, Formatting.Indented);
                File.WriteAllText(CommandsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存命令失败: {ex.Message}");
            }
        }

        // 从JSON文件加载命令列表
        private void LoadCommands()
        {
            try
            {
                if (File.Exists(CommandsFilePath))
                {
                    string json = File.ReadAllText(CommandsFilePath);
                    var commands = JsonConvert.DeserializeObject<List<CommandItem>>(json);
                    cmdListBox.Items.Clear();
                    foreach (var cmd in commands)
                    {
                        cmdListBox.Items.Add(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载命令失败: {ex.Message}");
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            InitCore();
            LoadCommands();
        }

        // 命令数据类
        public class CommandItem
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        // 控制面板可见性
        private void CommonCommandsViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            menuItem.IsChecked = !menuItem.IsChecked; // 切换勾选状态
            commonCommandsPanel.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        // 添加命令
        private void AddCommand_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("新建命令", "命令名称:", "命令内容:");
            if (inputDialog.ShowDialog() == true)
            {
                cmdListBox.Items.Add(new CommandItem
                {
                    Name = inputDialog.Answer1,
                    Value = inputDialog.Answer2
                });
                SaveCommands();
            }
        }

        // 使用命令
        private void CmdListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmdListBox.SelectedItem is CommandItem selectedCmd)
            {
                sendDataTextBox.Text = selectedCmd.Value;
                sendDataButton.Focus();
            }
        }

        // 删除命令
        private void RemoveCommand_Click(object sender, RoutedEventArgs e)
        {
            if (cmdListBox.SelectedItem != null)
            {
                cmdListBox.Items.Remove(cmdListBox.SelectedItem);
                SaveCommands();
            }
        }
        // 字号控制参数
        private const double DefaultFontSize = 12;  // 默认字号（与XAML中FlowDocument设置一致）
        private const double MinFontSize = 8;      // 最小字号
        private const double MaxFontSize = 36;     // 最大字号
        private const double FontStep = 1;         // 字号调整步长

        // 放大字号
        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (recvDataRichTextBox.FontSize + FontStep <= MaxFontSize)
            {
                recvDataRichTextBox.FontSize += FontStep;
                UpdateDocumentFontSize(recvDataRichTextBox.Document, recvDataRichTextBox.FontSize);
            }
        }

        // 缩小字号
        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (recvDataRichTextBox.FontSize - FontStep >= MinFontSize)
            {
                recvDataRichTextBox.FontSize -= FontStep;
                UpdateDocumentFontSize(recvDataRichTextBox.Document, recvDataRichTextBox.FontSize);
            }
        }

        // 重置字号
        private void ResetFontSize_Click(object sender, RoutedEventArgs e)
        {
            recvDataRichTextBox.FontSize = DefaultFontSize;
            UpdateDocumentFontSize(recvDataRichTextBox.Document, DefaultFontSize);
        }

        // 更新文档内所有文字字号（兼容已有内容）
        private void UpdateDocumentFontSize(FlowDocument document, double newSize)
        {
            foreach (var block in document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            run.FontSize = newSize;
                        }
                    }
                }
            }
        }
    }
}
