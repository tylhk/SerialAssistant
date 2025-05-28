using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace WPFSerialAssistant
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// SerialPort对象
        /// </summary>
        private SerialPort serialPort = new SerialPort();

        // 需要一个定时器用来，用来保证即使缓冲区没满也能够及时将数据处理掉，防止一直没有到达
        // 阈值，而导致数据在缓冲区中一直得不到合适的处理。
        private DispatcherTimer checkTimer = new DispatcherTimer();

        private void InitSerialPort()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
            InitCheckTimer();
        }

        /// <summary>
        /// 查找端口
        /// </summary>
        private void FindPorts()
        {
            portsComboBox.ItemsSource = SerialPort.GetPortNames();
            if (portsComboBox.Items.Count > 0)
            {
                portsComboBox.SelectedIndex = 0;
                portsComboBox.IsEnabled = true;
                Information(string.Format("查找到可以使用的端口{0}个。", portsComboBox.Items.Count.ToString()));
            }
            else
            {
                portsComboBox.IsEnabled = false;
                Alert("Oops，没有查找到可用端口；您可以点击“查找”按钮手动查找。");
            }
        }

        private bool OpenPort()
        {
            bool flag = false;
            ConfigurePort();

            try
            {
                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                Information(string.Format("成功打开端口{0}, 波特率{1}。", serialPort.PortName, serialPort.BaudRate.ToString()));
                flag = true;
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private bool ClosePort()
        {
            bool flag = false;

            try
            {
                StopAutoSendDataTimer();
                progressBar.Visibility = Visibility.Collapsed;
                serialPort.Close();
                Information(string.Format("成功关闭端口{0}。", serialPort.PortName));
                flag = true;
            }
            catch (Exception ex)
            {
                Alert(ex.Message);
            }

            return flag;
        }

        private void ConfigurePort()
        {
            serialPort.PortName = GetSelectedPortName();
            serialPort.BaudRate = GetSelectedBaudRate();
            serialPort.Parity = GetSelectedParity();
            serialPort.DataBits = GetSelectedDataBits();
            serialPort.StopBits = GetSelectedStopBits();
            serialPort.Encoding = GetSelectedEncoding();
        }

        private string GetSelectedPortName()
        {
            return portsComboBox.Text;
        }

        private int GetSelectedBaudRate()
        {
            int baudRate = 9600;
            //string conv = baudRateComboBox.Text;
            int.TryParse(baudRateComboBox.Text, out baudRate);
            return baudRate;
        }

        private Parity GetSelectedParity()
        {
            string select = parityComboBox.Text;

            Parity p = Parity.None;       
            if (select.Contains("Odd"))
            {
                p = Parity.Odd;
            }
            else if (select.Contains("Even"))
            {
                p = Parity.Even;
            }
            else if (select.Contains("Space"))
            {
                p = Parity.Space;
            }
            else if (select.Contains("Mark"))
            {
                p = Parity.Mark;
            }
           
            return p;
        }

        private int GetSelectedDataBits()
        {
            int dataBits = 8;
            int.TryParse(dataBitsComboBox.Text, out dataBits);

            return dataBits;
        }

        private StopBits GetSelectedStopBits()
        {
            StopBits stopBits = StopBits.None;
            string select = stopBitsComboBox.Text.Trim();

            if (select.Equals("1"))
            {
                stopBits = StopBits.One;
            }
            else if (select.Equals("1.5"))
            {
                stopBits = StopBits.OnePointFive;
            }
            else if (select.Equals("2"))
            {
                stopBits = StopBits.Two;
            }

            return stopBits;
        }

        private Encoding GetSelectedEncoding()
        {
            string select = encodingComboBox.Text;
            Encoding enc = Encoding.Default;

            if (select.Contains("UTF-8"))
            {
                enc = Encoding.UTF8;
            }
            else if (select.Contains("ASCII"))
            {
                enc = Encoding.ASCII;
            }
            else if (select.Contains("Unicode"))
            {
                enc = Encoding.Unicode;
            }

            return enc;
        }

        private string appendContent = "\n";
        /// <summary>
        /// 串口写入数据。可选回显（用于手动输入/单条命令）。
        /// </summary>
        private bool SerialPortWrite(string textData, bool echoToUI = true)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                Alert("串口未打开，无法发送数据。");
                return false;
            }

            try
            {
                if (sendMode == SendMode.Character)
                {
                    serialPort.Write(textData + appendContent);
                }
                else if (sendMode == SendMode.Hex)
                {
                    string[] grp = textData.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<byte> list = new List<byte>();
                    foreach (var item in grp)
                        list.Add(Convert.ToByte(item, 16));
                    serialPort.Write(list.ToArray(), 0, list.Count);
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => Alert(ex.Message));
                return false;
            }

            // 回显，只能在UI线程
            if (echoToUI && sendEchoCheckBox != null && sendEchoCheckBox.IsChecked == true)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    string timeStr = DateTime.Now.ToString("[发送 HH:mm:ss.fff]");
                    Paragraph para = new Paragraph();
                    Run timeRun = new Run(timeStr + "\n") { Foreground = Brushes.Red };
                    Run contentRun = new Run(textData + "\n") { Foreground = Brushes.Red };
                    para.Inlines.Add(timeRun);
                    para.Inlines.Add(contentRun);
                    recvDataRichTextBox.Document.Blocks.Add(para);
                    recvDataRichTextBox.ScrollToEnd();
                });
            }

            return true;
        }

        // 兼容你的老接口，只保留，不建议再直接用
        private bool SerialPortWrite(string textData)
        {
            // 默认回显
            return SerialPortWrite(textData, true);
        }


        #region 定时器
        /// <summary>
        /// 超时时间为50ms
        /// </summary>
        private const int TIMEOUT = 50;
        private void InitCheckTimer()
        {
            // 如果缓冲区中有数据，并且定时时间达到前依然没有得到处理，将会自动触发处理函数。
            checkTimer.Interval = new TimeSpan(0, 0, 0, 0, TIMEOUT);
            checkTimer.IsEnabled = false;
            checkTimer.Tick += CheckTimer_Tick;
        }

        private void StartCheckTimer()
        {
            checkTimer.IsEnabled = true;
            checkTimer.Start();
        }

        private void StopCheckTimer()
        {
            checkTimer.IsEnabled = false;
            checkTimer.Stop();
        }
        #endregion
    }
}
