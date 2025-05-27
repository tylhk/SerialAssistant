// CommandSettingsWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPFSerialAssistant
{
    public partial class CommandSettingsWindow : Window
    {
        public List<List<MainWindow.CommandItem>> AllPagesCommands => commandConfig.AllPagesCommands;
        public int CurrentPageIndex => commandConfig.CurrentPageIndex;

        private CommandConfig commandConfig;
        private int currentPageIndex = 0;

        public CommandSettingsWindow(CommandConfig config)
        {
            InitializeComponent();
            commandConfig = config;

            // 确保有至少一个页面
            if (commandConfig.AllPagesCommands == null || commandConfig.AllPagesCommands.Count == 0)
            {
                commandConfig.AllPagesCommands = new List<List<MainWindow.CommandItem>> { new List<MainWindow.CommandItem>() };
                commandConfig.PageNames.Add("页面1");
            }

            // 加载页面列表
            for (int i = 0; i < commandConfig.AllPagesCommands.Count; i++)
            {
                if (commandConfig.PageNames.Count <= i)
                {
                    commandConfig.PageNames.Add($"页面{i + 1}");
                }
                pageListBox.Items.Add(commandConfig.PageNames[i]);
            }

            // 选中当前页面
            if (commandConfig.CurrentPageIndex < pageListBox.Items.Count)
            {
                pageListBox.SelectedIndex = commandConfig.CurrentPageIndex;
            }
            else
            {
                pageListBox.SelectedIndex = 0;
            }

            commandDataGrid.DataContext = this;
        }

        private void DeletePageButton_Click(object sender, RoutedEventArgs e)
        {
            if (pageListBox.SelectedIndex >= 0 && commandConfig.AllPagesCommands.Count > 1) // 确保至少有一个页面
            {
                int selectedIndex = pageListBox.SelectedIndex;
                commandConfig.AllPagesCommands.RemoveAt(selectedIndex);
                commandConfig.PageNames.RemoveAt(selectedIndex);
                pageListBox.Items.RemoveAt(selectedIndex);

                if (pageListBox.Items.Count > 0)
                {
                    if (selectedIndex < pageListBox.Items.Count)
                    {
                        pageListBox.SelectedIndex = selectedIndex;
                    }
                    else
                    {
                        pageListBox.SelectedIndex = pageListBox.Items.Count - 1;
                    }
                }
                else
                {
                    // 如果没有页面了，添加一个新页面
                    commandConfig.AllPagesCommands.Add(new List<MainWindow.CommandItem>());
                    commandConfig.PageNames.Add("页面1");
                    pageListBox.Items.Add(commandConfig.PageNames[0]);
                    pageListBox.SelectedIndex = 0;
                }

                commandDataGrid.ItemsSource = CurrentCommands;
            }
        }

        public List<MainWindow.CommandItem> CurrentCommands
        {
            get
            {
                if (commandConfig.AllPagesCommands != null &&
                    commandConfig.AllPagesCommands.Count > currentPageIndex)
                {
                    return commandConfig.AllPagesCommands[currentPageIndex];
                }
                return new List<MainWindow.CommandItem>();
            }
        }

        private void AddPageButton_Click(object sender, RoutedEventArgs e)
        {
            commandConfig.AllPagesCommands.Add(new List<MainWindow.CommandItem>());
            string newPageName = $"页面{commandConfig.AllPagesCommands.Count}";
            commandConfig.PageNames.Add(newPageName);
            pageListBox.Items.Add(newPageName);
            pageListBox.SelectedIndex = commandConfig.AllPagesCommands.Count - 1;
        }

        private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pageListBox.SelectedIndex >= 0)
            {
                currentPageIndex = pageListBox.SelectedIndex;
                commandConfig.CurrentPageIndex = currentPageIndex; // 更新当前选中页面
                commandDataGrid.ItemsSource = CurrentCommands;
            }
        }

        private void AddCommandButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentCommands.Add(new MainWindow.CommandItem { Name = "", Value = "" });
            commandDataGrid.Items.Refresh();
        }

        private void DeleteCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (commandDataGrid.SelectedItem != null)
            {
                CurrentCommands.Remove(commandDataGrid.SelectedItem as MainWindow.CommandItem);
                commandDataGrid.Items.Refresh();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void PageListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (pageListBox.SelectedIndex >= 0)
            {
                var inputWindow = new InputWindow((string)pageListBox.SelectedItem);
                if (inputWindow.ShowDialog() == true)
                {
                    string newName = inputWindow.InputText;
                    if (!string.IsNullOrEmpty(newName))
                    {
                        int selectedIndex = pageListBox.SelectedIndex;
                        commandConfig.PageNames[selectedIndex] = newName;
                        pageListBox.Items[selectedIndex] = newName;
                    }
                }
            }
        }
    }

    public class InputWindow : Window
    {
        public string InputText { get; set; }

        public InputWindow(string initialText)
        {
            Title = "重命名页面";
            Width = 300;
            Height = 150;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var textBox = new TextBox { Text = initialText };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            var okButton = new Button { Content = "确定", Width = 75, Margin = new Thickness(5) };
            okButton.Click += (sender, e) =>
            {
                InputText = textBox.Text;
                DialogResult = true;
                Close();
            };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button { Content = "取消", Width = 75, Margin = new Thickness(5) };
            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(buttonPanel);

            Content = stackPanel;
        }
    }
}
