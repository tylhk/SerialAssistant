using System.Collections.Generic;
using System.Windows;

namespace WPFSerialAssistant
{
    public partial class SelectDialog : Window
    {
        public string SelectedType { get; private set; }
        public SelectDialog(IEnumerable<string> types)
        {
            InitializeComponent();
            cbTypes.ItemsSource = types;
            if (cbTypes.Items.Count > 0)
                cbTypes.SelectedIndex = 0;
        }
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = cbTypes.SelectedItem as string;
            this.DialogResult = true;
        }
    }
}
