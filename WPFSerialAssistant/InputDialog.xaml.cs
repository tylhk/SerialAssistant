using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFSerialAssistant
{
    public partial class InputDialog : Window
    {
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }

        public InputDialog(string title, string prompt1, string prompt2)
        {
            InitializeComponent();
            Title = title;
            promptText1.Text = prompt1;
            promptText2.Text = prompt2;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Answer1 = answerTextBox1.Text;
            Answer2 = answerTextBox2.Text;
            DialogResult = true; // 关闭对话框并返回true
        }
    }
}
