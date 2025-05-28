using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


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
            quickTestTimer.Interval = TimeSpan.FromMilliseconds(50);
            quickTestTimer.Tick += QuickTestTimer_Tick;
        }

        private Dictionary<string, List<string>> quickTestCmdDict = new Dictionary<string, List<string>>
        {
            { "初始化", new List<string> { "AT", "AT+PRTR=1", "AT+PRTS=1", "AT+TEST=1," } },
            { "信道1", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,1" } },
            { "信道2", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,2" } },
            { "信道3", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,3" } },
            { "信道4", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,4" } },
            { "信道5", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,5" } },
            { "信道6", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,6" } },
            { "信道7", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,7" } },
            { "信道8", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,8" } },
            { "信道9", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,9" } },
            { "信道10", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,10" } },
            { "信道11", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,11" } },
            { "信道12", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,12" } },
            { "信道13", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,13" } },
            { "信道14", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,14" } },
            { "信道15", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,15" } },
            { "信道16", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,16" } },
            { "信道17", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,17" } },
            { "信道18", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,18" } },
            { "信道19", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,19" } },
            { "信道20", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,20" } },
            { "信道21", new List<string> { "AT", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+SEND=20,0302030405060708091003020304050607080910,1", "AT+TEST=1,21" } },
        };
        private List<string> currentQuickTestCmdList = null;
        private int quickTestCount = 0;
        private int quickTestTotal = 0;
        private DispatcherTimer quickTestTimer = new DispatcherTimer();
        private void QuickTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 弹出自定义选择窗口
            var dlg = new SelectDialog(quickTestCmdDict.Keys);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.SelectedType))
            {
                currentQuickTestCmdList = quickTestCmdDict[dlg.SelectedType];
                quickTestCount = 0;
                quickTestTotal = currentQuickTestCmdList.Count;
                quickTestTimer.Start();
            }
        }



        private void QuickTestTimer_Tick(object sender, EventArgs e)
        {
            if (quickTestCount < quickTestTotal)
            {
                string cmd = currentQuickTestCmdList[quickTestCount];
                SerialPortWrite(cmd);
                recvDataRichTextBox.ScrollToEnd();
                quickTestCount++;
            }
            else
            {
                quickTestTimer.Stop();
            }
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
                SerialPortWrite(selectedCmd.Value);
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

        private void FileDropBorder_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void FileDropBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    filePathTextBlock.Text = files[0];
            }
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
                filePathTextBlock.Text = dialog.FileName;
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            string path = filePathTextBlock.Text;
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                MessageBox.Show("请选择有效的文件！");
                return;
            }
            SendFileBySerial(path); // 你自己的串口发送函数
        }

        private void SendFileBySerial(string filePath)
        {
            Task.Run(() =>
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(filePath);
                    int total = lines.Length;

                    // 主线程显示进度条
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Visibility = Visibility.Visible;
                        progressBar.Minimum = 0;
                        progressBar.Maximum = 100;
                        progressBar.Value = 0;
                    });

                    for (int i = 0; i < total; i++)
                    {
                        SerialPortWrite(lines[i], false);  // 只发送，不回显

                        // 每50行刷新一次进度条
                        if (i % 50 == 0 || i == total - 1)
                        {
                            int percent = (int)((i + 1) * 100.0 / total);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = percent;
                            });
                        }

                        Thread.Sleep(1); // 视你的设备性能可调小一点
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"文件已全部发送：{System.IO.Path.GetFileName(filePath)}");
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"发送文件失败：{ex.Message}");
                    });
                }
            });
        }






    }
}
