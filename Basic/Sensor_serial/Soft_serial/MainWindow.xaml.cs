using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Collections;
using System.Windows.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.IO;


//using UnityEngine;
//using System.Collections;

namespace Soft_serial
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serial = new SerialPort(); //Main Serial
        SerialPort serial_UWB = new SerialPort(); //Main Serial
        SerialPort arduino = new SerialPort(); //data from arduino serial
         //Receive & Send data buffer

        private List<Byte> _SendDataList, _RecvDataList;
        private List<Byte> SendDataList
        {
            get
            {
                if (_SendDataList == null)
                    _SendDataList = new List<byte>();
                return _SendDataList;
            }
        }
        private List<Byte> RecvDataList
        {
            get
            {
                if (_RecvDataList == null)
                    _RecvDataList = new List<byte>();
                return _RecvDataList;
            }
        }
        private List<Byte> RecvDataList_UWB
        {
            get
            {
                if (_RecvDataList == null)
                    _RecvDataList = new List<byte>();
                return _RecvDataList;
            }
        }
        private String SendData {get; set;}

        //log control
        /* 로그 제어 */
        private StringBuilder _Strings;
        /// <summary>
        /// 로그 객체
        /// </summary>
        private String Strings
        {
            set
            {
                if (_Strings == null)
                    _Strings = new StringBuilder(1024);
                // 로그 길이가 1024자가 되면 이전 로그 삭제
                if (_Strings.Length >= (1024 - value.Length))
                    _Strings.Clear();
                // 로그 추가 및 화면 표시
                _Strings.AppendLine(value);
                //Tbox_Out.Text = _Strings.ToString();
                //// Caret 가장 아래로
                //Tbox_Out.SelectionStart = Tbox_Out.Text.Length;
                //Tbox_Out.ScrollToCaret();
            }
        }

        Queue COP_que = new Queue(); // COP Draw using QUEUE
        System.IO.StreamWriter file; // Data Save

        // data set up 
        string name = "test";  //파일 숫자 자동 변경
        string name2;
        int file_num = 1;
        string Path_Selected;
        string Save_Path = @"EYs-MacBook-Pro Desktop";  //default 경로: 바탕화면

        bool flag_save = false; //Start, Stop 버튼 클릭
        char[] Tempo_Data = new char[10];

        private System.Windows.Forms.Timer timer;
        Queue<TimerTask> timerTaskQueue = new Queue<TimerTask>();

        #region Initial
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(InitSerialPort);  //Serial 받기

            Thread thread = new Thread(process_Soft);
            thread.Start();

            //Thread thread_uwb = new Thread(process_UWB);
            //thread_uwb.Start();

            //Thread thread_uwb2 = new Thread(process_UWB_2);
            //thread_uwb2.Start();

            Thread thread_ui = new Thread(Update_ui);
            thread_ui.Start();

 

            timer = new System.Windows.Forms.Timer();
            timer.Tick += timer_Tick;
            timer.Interval = 100;
            timer.Start();
            //COP 계산 변수

        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (timerTaskQueue.Count == 0)
            {
                timer.Stop();
                return;
            }

            TimerTask task = timerTaskQueue.Dequeue();

            Thread.Sleep(task.WaitTime_msec);
            //timer.Tick
            //Textbox_Timer.Text = timer_100ms.ToString();
        }
        public class TimerTask
        {
            private int waitTime_msec;

            public int WaitTime_msec
            {
                get { return waitTime_msec; }
                set { waitTime_msec = value; }
            }

            private string message;

            public string Message
            {
                get { return message; }
                set { message = value; }
            }
        }
        private void Update_ui()
        {
            for(;;)
            {
                Thread.Sleep(50);
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                     // add ui
                });
            }
        }

        void InitSerialPort(object sender, EventArgs e)
        {
            serial.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                Comport_num.Items.Add(port);
                Comport_num_Ain.Items.Add(port);
            }          
        }

        private void Comport_num_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serial.IsOpen)
            {
                serial.Close();
            }
            serial.PortName = Comport_num.SelectedItem.ToString();
            OpenComPort(sender, e);
        }

        private void Comport_num_SelectionChanged_Ain(object sender, SelectionChangedEventArgs e)
        {
            if (arduino.IsOpen)
            {
                arduino.Close();
            }

            arduino.PortName = Comport_num_Ain.SelectedItem.ToString();
            OpenComPort_Ain(sender, e);
        }


        void OpenComPort(object sender, RoutedEventArgs e)
        {
            try
            {
                serial.Open();
            }
            catch (Exception ex)
            {
                Comport_num.SelectedItem = "";
            }
        }

        void OpenComPort_Ain(object sender, RoutedEventArgs e)
        {
            try
            {
                arduino.Open();
                arduino.BaudRate = 115200;
            }
            catch (Exception ex)
            {
                Comport_num.SelectedItem = "";
            }   
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] names = cbComSpeed.SelectedItem.ToString().Split(':');
            serial.BaudRate = int.Parse(names[1].ToString().Trim());
        }
        #endregion

        void serial_DataReceived(object sender, SerialDataReceivedEventArgs e) //Serial 받는 부분
        {

            int intRecSize = serial.BytesToRead;
            if (intRecSize != 0)
            {
                try
                {
                    for (int i = 0; i < serial.BytesToRead; i++)
                        RecvDataList.Add((Byte)serial.ReadByte());
                }
                catch { }
            }
        }

        protected void Button_Start(object sender, RoutedEventArgs e)  // save 시작
        {
            try
            {
                name = txtname.Text;
                file_num = Convert.ToInt16(Regex.Replace(name, @"\D", ""));
                name2 = Regex.Replace(name, @"[\d-]", string.Empty);
                file = new System.IO.StreamWriter(Save_Path + name + ".txt", append: true); // 저장 경로
                flag_save = true; //데이터 저장 시작 신호 -> get data from arduino
                if (arduino.IsOpen) //Analog input 아두이노
                {
                    arduino.Write("255;");
                }
                if (serial.IsOpen)
                {
                    string str = "*s";
                    str += name + ";";
                    serial.Write(str);
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                System.Windows.Forms.MessageBox.Show("경로를 먼저 설정 하세요.");
            }
            catch (System.IO.IOException)
            {
                System.Windows.Forms.MessageBox.Show("1번만 누르세요.");
            }
            catch (System.FormatException)
            {
                System.Windows.Forms.MessageBox.Show("Text + 숫자 or 숫자  형식으로 입력하세요.");
            }
        }
        private void Button_End(object sender, RoutedEventArgs e)
        {
            try
            {
                file.Close();
                
                 if (arduino.IsOpen)
                {
                    arduino.Write("254;");
                }
                if (serial.IsOpen)
                {
                    string str = "*e";
                    str += name + ";";
                    serial.Write(str);
                }
                flag_save = false;
                txtname.Text = name2 + Convert.ToString(file_num + 1);
            }
            catch (System.NullReferenceException)
            {
                System.Windows.Forms.MessageBox.Show("닫을 파일이 없습니다.");
            }
        }

        public void process_Soft()
        {
            for (;;)
            {
                if (RecvDataList.Count < 10) continue;
                int bufferlen = RecvDataList.Count;
                byte[] BtData = new byte[bufferlen];

                RecvDataList.CopyTo(0, BtData, 0, bufferlen);

                // size 가 500mb 보다 클시
                if (flag_save == true)
                {
                    string ff = Save_Path + name + ".txt";
                    var info = new FileInfo(ff);
                    if (info.Length >= 500000) // 변경할것
                    {
                        file.Close();
                        name2 = Regex.Replace(name, @"[\d-]", string.Empty);
                        file_num++; // file numbering
                        name = name2 + Convert.ToString(file_num); 
                        file = new System.IO.StreamWriter(Save_Path + name + ".txt", append: true); // 저장 경로
                    }
                }

                if (flag_save == true)
                {
                    for (int i = 0; i < bufferlen; i++)
                    {
                        file.WriteLine("{0}", BtData[i]) ;
                    }
                }
                RecvDataList.RemoveRange(0, bufferlen);

            }
        }

        private void Button_Click_Send(object sender, RoutedEventArgs e)
        {
            try
            {
                //serial.Write(Send_text.Text);
                //Edit_raw.Text = "send messages";
            }
            catch (System.InvalidOperationException)
            {
              //  System.Windows.Forms.MessageBox.Show("포트를 먼저 설정하세요.");
            }
        }

        private void Path_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            Path_Selected = dialog.SelectedPath;
            Save_Path = @"" + Path_Selected + "\\";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Box_In_Event.Stop();
           // COP_Draw_Event.Stop();
        }
    }
}
