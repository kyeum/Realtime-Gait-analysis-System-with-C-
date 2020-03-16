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
using CRC;


namespace serial_new
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serial = new SerialPort(); //Main Serial
        SerialPort arduino = new SerialPort(); //data from arduino serial
         //Receive & Send data buffer
         //buffer initialize 
        const int bufferlength = 46;
        const int datalength = bufferlength - 6; // STX, ETX, CRC

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
        System.IO.StreamWriter file = null; // Data Save

        byte[] Rec_Check = new byte[bufferlength];
        int[] Tempo_FSR = new int[datalength];
        byte[] test = new byte[42];
        byte[] Crc;

        float[] posx = new float[10]; //COP 계산 변수
        float[] posy = new float[10];
        byte[] pos1y = new byte[10];
        float w = 93.5f;
        float l = 258.4f;

        string name = "test";  //파일 숫자 자동 변경
        string name2;
        int file_num = 1;
        string Path_Selected;
        string Save_Path = @"EYs-MacBook-Pro Desktop";  //default 경로: 바탕화면

        bool flag = false; //Start, Stop 버튼 클릭
        bool Draw_flag = false; //Start, Stop 버튼 클릭    

        System.Windows.Threading.DispatcherTimer Box_In_Event = new System.Windows.Threading.DispatcherTimer();
        System.Windows.Threading.DispatcherTimer COP_Draw_Event = new System.Windows.Threading.DispatcherTimer();
        private Crc crc;

        #region Initial
        public MainWindow()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(InitSerialPort);  //Serial 받기

            
            Box_In_Event.Interval = new TimeSpan(0, 0, 0, 0, 40);
            Box_In_Event.Tick += new EventHandler(Text_box_in);
            Box_In_Event.Start();
            
            
            COP_Draw_Event.Interval = new TimeSpan(0, 0, 0, 0, 30);
            COP_Draw_Event.Tick += new EventHandler(COP_Cal);
            COP_Draw_Event.Start();
            
            crc = new CRC.Crc(CRC.CrcStdParams.StandartParameters[CRC.CrcAlgorithms.Crc32Mpeg2]);
            //COP 계산 변수

            //Left position
            posx[0] = -162.35f + 0.25f * w;
            posx[1] = -162.35f + 0.25f * w;
            posx[2] = -162.35f - 0.25f * w;
            posx[3] = -162.35f - 0.25f * w;
            posx[4] = -162.35f;

            //Right position
            posx[5] = 162.35f - 0.25f * w;
            posx[6] = 162.35f - 0.25f * w;
            posx[7] = 162.35f + 0.25f * w;
            posx[8] = 162.35f + 0.25f * w;
            posx[9] = 162.35f;

            posy[0] = 0.7f * l;
            posy[1] = 0.58f * l;
            posy[2] = 0.49f * l;
            posy[3] = 0.35f * l;
            posy[4] = 0f;
            posy[5] = 0.7f * l;
            posy[6] = 0.58f * l;
            posy[7] = 0.49f * l;
            posy[8] = 0.35f * l;
            posy[9] = 0f;
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
                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate { process2(); }));
                }
                catch { }
            }
        }

        protected void Button_Click(object sender, RoutedEventArgs e)  // save 시작
        {
            try
            {
                name = txtname.Text;
                file_num = Convert.ToInt16(Regex.Replace(name, @"\D", ""));
                name2 = Regex.Replace(name, @"[\d-]", string.Empty);
                file = new System.IO.StreamWriter(Save_Path + name + ".txt", append: true); // 저장 경로
                flag = true; //데이터 저장 시작 신호
                if (arduino.IsOpen) //Analog input 아두이노
                {
                    arduino.Write("1");
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

        private void process2()
        {
            try
            {
                for (;;)
                {
                    if (RecvDataList.Count < 40) return;

                    if (!(RecvDataList[0] == 0xFF && RecvDataList[1] == 0xFF)) //FF, FF 확인
                    {
                        RecvDataList.RemoveRange(0, 1); //FF, FF전 데이터 모두삭제
                        return;
                    }
                    else
                    {
                        //test.Initialize();
                        //for (int e=0; e<42; e++)
                        //{
                        //    test[e] = (RecvDataList[e+2]);
                        //    //file.Write("{0} ", Convert.ToString(test[e], 16));
                        //}
                        //file.WriteLine();

                       // Crc =crc.ComputeHash(test);
                        /*
                        for (int e = 0; e < Crc.Length; e++)
                        {
                            file.Write("{0} ", Convert.ToString(Crc[e],16));
                        }
                        file.WriteLine();
                        file.WriteLine();

                        file.Write("{0} ", Convert.ToString(Buff_Rec_2[47], 16));
                        file.Write("{0} ", Convert.ToString(Buff_Rec_2[48], 16));
                        */
                        if (RecvDataList[bufferlength-2] == 0xFF && RecvDataList[bufferlength-1] == 0xFE) //FF, FE 확인
                        { 
                            // RecvDataList.CopyTo(0,Rec_Check,2,41);
                            RecvDataList.CopyTo(0,Rec_Check,0, bufferlength-1);

                            //data parse
                            //0~1 time
                            //1~5 right 6~10 left 11~19 imu
                            for (int i =0; i < 20; i++)
                            {
                                int k = 2 * i;
                                Tempo_FSR[i] = (Rec_Check[2+k] << 8) + Rec_Check[3+k];
                                if (flag == true)
                                {
                                    file.Write("{0}", Tempo_FSR[i]);
                                }
                            }
                            Draw_flag = true;
                            RecvDataList.RemoveRange(0, bufferlength-1); //사용 데이터 모두삭제
                        }
                        else
                        {
                            RecvDataList.RemoveRange(0, bufferlength-1); //사용 데이터 모두삭제
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                file.Close();
                if (arduino.IsOpen)
                {
                    arduino.Write("2");
                }
                flag = false;
                COP_Draw.Children.Clear();
                txtname.Text = name2 + Convert.ToString(file_num + 1);
            }
            catch (System.NullReferenceException)
            {
                System.Windows.Forms.MessageBox.Show("닫을 파일이 없습니다.");
            }
        }

        private void Button_Click_Send(object sender, RoutedEventArgs e)
        {
            try
            {
                serial.Write(Send_text.Text);
                Edit_raw.Text = "send messages";
            }
            catch (System.InvalidOperationException)
            {
                System.Windows.Forms.MessageBox.Show("포트를 먼저 설정하세요.");
            }
        }

        private void Path_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            Path_Selected = dialog.SelectedPath;
            Save_Path = @"" + Path_Selected + "\\";
        }

        public void COP_D(Queue COP_que)
        {
            Ellipse ellipse = new Ellipse // 점 표현 
            {
                Fill = System.Windows.Media.Brushes.Black,
                Width = 20,
                Height = 20
            };

            Ellipse ellipse_R = new Ellipse // 점 표현 
            {
                Fill = System.Windows.Media.Brushes.Red,
                Width = 15,
                Height = 15
            };

            Ellipse ellipse_L = new Ellipse // 점 표현 
            {
                Fill = System.Windows.Media.Brushes.Blue,
                Width = 15,
                Height = 15
            };

            Canvas.SetRight(ellipse, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Width * 0.45));
            Canvas.SetBottom(ellipse, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Height * 0.5 - 100));

            Canvas.SetRight(ellipse_R, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Width * 0.45));
            Canvas.SetBottom(ellipse_R, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Height * 0.5 - 100));

            Canvas.SetRight(ellipse_L, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Width * 0.45));
            Canvas.SetBottom(ellipse_L, Convert.ToDouble(COP_que.Dequeue()) * 1.7 + (COP_Draw.Height * 0.5 - 100));

            COP_Draw.Children.Add(ellipse_L);
            COP_Draw.Children.Add(ellipse_R);
            COP_Draw.Children.Add(ellipse);
        }

        public void COP_Cal(object sender, System.EventArgs e)
        {
            COP_Draw.Children.Clear();
            if (Draw_flag == true)
            {
                //COP Total
                int cop_r = Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5];
                int cop_l = Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10];
                float cop_pos_r = ((Tempo_FSR[1] * posx[0] + Tempo_FSR[2] * posx[1] + Tempo_FSR[3] * posx[2] + Tempo_FSR[4] * posx[3] + Tempo_FSR[5] * posx[4]));
                float cop_pos_l = ((Tempo_FSR[6] * posx[5] + Tempo_FSR[7] * posx[6] + Tempo_FSR[8] * posx[7] + Tempo_FSR[9] * posx[8] + Tempo_FSR[10] * posx[9]));


                if (cop_r + cop_l > 30)
                {
                    COP_que.Enqueue(Math.Round(((cop_pos_r + cop_pos_l) * 50 / (cop_r + cop_l) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((cop_pos_r + cop_pos_l) * 100 / (cop_r + cop_l) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(0);
                    COP_que.Enqueue(50);
                }

                //COP Right
                if (cop_r > 30)
                {
                    COP_que.Enqueue(Math.Round(((cop_pos_r) * 50 / (cop_r) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((cop_pos_r) * 100 / (cop_r) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(-40);
                    COP_que.Enqueue(50);
                }

                //COP Left
                if (cop_l > 30)
                {
                    COP_que.Enqueue(Math.Round(((cop_pos_l) * 50 / (cop_r) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((cop_pos_l) * 100 / (cop_r) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(40);
                    COP_que.Enqueue(50);
                }
                COP_D(COP_que);
            }
        }

        public void Text_box_in(object sender, System.EventArgs e)
        {
            int[] Tempo_FSR2 = new int[35];
            Buffer.BlockCopy(Tempo_FSR, 0, Tempo_FSR2, 0, 50);
            
            Time.Text = Tempo_FSR[0].ToString();

            fsr6_box.Text = Tempo_FSR2[1].ToString();
            fsr7_box.Text = Tempo_FSR2[2].ToString();
            fsr8_box.Text = Tempo_FSR2[3].ToString();
            fsr9_box.Text = Tempo_FSR2[4].ToString();
            fsr10_box.Text = Tempo_FSR2[5].ToString();

            fsr1_box.Text = Tempo_FSR2[6].ToString();
            fsr2_box.Text = Tempo_FSR2[7].ToString();
            fsr3_box.Text = Tempo_FSR2[8].ToString();
            fsr4_box.Text = Tempo_FSR2[9].ToString();
            fsr5_box.Text = Tempo_FSR2[10].ToString();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Box_In_Event.Stop();
            COP_Draw_Event.Stop();
        }

    }
}

