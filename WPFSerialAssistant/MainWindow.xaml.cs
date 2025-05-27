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

        private List<List<CommandItem>> allPagesCommands = new List<List<CommandItem>>();
        private CommandConfig commandConfig = new CommandConfig();
        // 保存命令列表到JSON文件
        private void SaveCommands()
        {
            try
            {
                // 确保文件夹存在
                Directory.CreateDirectory(ConfigFolder);

                // 将命令配置序列化为JSON字符串
                string json = JsonConvert.SerializeObject(commandConfig, Formatting.Indented);

                // 将JSON字符串写入文件
                File.WriteAllText(CommandsFilePath, json);
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show($"保存命令失败: 文件夹 {ConfigFolder} 不存在。{ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"保存命令失败: 没有权限访问文件 {CommandsFilePath}。{ex.Message}");
            }
            catch (IOException ex)
            {
                MessageBox.Show($"保存命令失败: 文件读写错误。{ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存命令失败: {ex.Message}");
            }
        }
        private void LoadCommands()
        {
            try
            {
                if (File.Exists(CommandsFilePath))
                {
                    string json = File.ReadAllText(CommandsFilePath);
                    commandConfig = JsonConvert.DeserializeObject<CommandConfig>(json) ?? new CommandConfig();

                    // 加载当前选中页面的命令
                    LoadCurrentPageCommands();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载命令失败: {ex.Message}");
            }
        }
        // 从JSON文件加载命令列表
        private void LoadCurrentPageCommands()
        {
            cmdListBox.Items.Clear();

            if (commandConfig.AllPagesCommands != null &&
                commandConfig.AllPagesCommands.Count > commandConfig.CurrentPageIndex)
            {
                var currentCommands = commandConfig.AllPagesCommands[commandConfig.CurrentPageIndex];
                foreach (var cmd in currentCommands)
                {
                    cmdListBox.Items.Add(cmd);
                }
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

        // 使用命令
        private void CmdListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmdListBox.SelectedItem is CommandItem selectedCmd)
            {
                sendDataTextBox.Text = selectedCmd.Value;
                sendDataButton.Focus();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 确保命令配置已初始化
            if (commandConfig.AllPagesCommands == null)
                commandConfig.AllPagesCommands = new List<List<CommandItem>>();

            var settingsWindow = new CommandSettingsWindow(commandConfig);
            if (settingsWindow.ShowDialog() == true)
            {
                // 更新命令配置
                commandConfig.AllPagesCommands = settingsWindow.AllPagesCommands;
                commandConfig.CurrentPageIndex = settingsWindow.CurrentPageIndex;

                // 加载当前选中页面的命令
                LoadCurrentPageCommands();

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
