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


namespace serial_new
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serial = new SerialPort(); //Main Serial
        SerialPort arduino = new SerialPort(); //Analog output
        List<int> Buff_Rec_2 = new List<int>();
        Queue COP_que = new Queue(); // COP Draw
        System.IO.StreamWriter file = null; // Data Save

        int[] Rec_Check = new int[35];
        int[] Tempo_FSR = new int[15];
        int[] Tempo_FSR2 = new int[15];

        float[] posx = new float[10]; //COP 계산 변수
        float[] posy = new float[10];
        float w = 93.5f;
        float l = 258.4f;

        string name = "test";  //파일 숫자 자동 변경
        string name2;
        int file_num = 1;
        string Path_Selected;
        string Save_Path = @"C:\Users\Choong Hyun Kim\Desktop\";  //default 경로: 바탕화면

        bool flag = false; //Start, Stop 버튼 클릭
        bool Draw_flag = false; //Start, Stop 버튼 클릭    

        System.Windows.Threading.DispatcherTimer Box_In_Event = new System.Windows.Threading.DispatcherTimer();
        System.Windows.Threading.DispatcherTimer COP_Draw_Event = new System.Windows.Threading.DispatcherTimer();

        #region Initial
        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(InitSerialPort);  //Serial 받기

            Box_In_Event.Interval = new TimeSpan(0, 0, 0, 0, 40);
            Box_In_Event.Tick += new EventHandler(Text_box_in);
            Box_In_Event.Start();

            COP_Draw_Event.Interval = new TimeSpan(0, 0, 0, 0, 20);
            COP_Draw_Event.Tick += new EventHandler(COP_Cal);
            COP_Draw_Event.Start();

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
                        Buff_Rec_2.Add((Byte)serial.ReadByte());
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
                for (int d = 0; d < Buff_Rec_2.Count; d++)
                {
                    if (Buff_Rec_2[d] == 255 && Buff_Rec_2[d + 1] == 255) //FF, FF 확인
                    {
                        Buff_Rec_2.RemoveRange(0, d); //FF, FF전 데이터 모두삭제

                        if (Buff_Rec_2[26] == 255 && Buff_Rec_2[27] == 254) //FF, FE 확인
                        {
                            for (int i = 0; i < 28; i++)
                            {
                                Rec_Check[i] = Buff_Rec_2[i];
                            }

                            //시간
                            Tempo_FSR[0] = (Rec_Check[2] << 8) + Rec_Check[3];                        
                            if (flag == true)
                            {
                                file.Write("{0}", Tempo_FSR[0]);    
                            }

                            //오른발
                            Tempo_FSR[1] = (Rec_Check[4] << 8) + Rec_Check[5];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[1]);
                            }
                            Tempo_FSR[2] = (Rec_Check[6] << 8) + Rec_Check[7];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[2]);
                            }
                            Tempo_FSR[3] = (Rec_Check[8] << 8) + Rec_Check[9];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[3]);
                            }
                            Tempo_FSR[4] = (Rec_Check[10] << 8) + Rec_Check[11];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[4]);
                            }
                            Tempo_FSR[5] = (Rec_Check[12] << 8) + Rec_Check[13];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[5]);
                            }

                            //왼발                      
                            Tempo_FSR[6] = (Rec_Check[14] << 8) + Rec_Check[15];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[6]);
                            }
                            Tempo_FSR[7] = (Rec_Check[16] << 8) + Rec_Check[17];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[7]);
                            }
                            Tempo_FSR[8] = (Rec_Check[18] << 8) + Rec_Check[19];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[8]);
                            }
                            Tempo_FSR[9] = (Rec_Check[20] << 8) + Rec_Check[21];
                            if (flag == true)
                            {
                                file.Write(" {0}", Tempo_FSR[9]);
                            }
                            Tempo_FSR[10] = (Rec_Check[22] << 8) + Rec_Check[23];
                            if (flag == true)
                            {
                                file.WriteLine(" {0}", Tempo_FSR[10]);
                            }
                            Buff_Rec_2.RemoveRange(0, 27); //사용 데이터 모두삭제
                        }
                        Draw_flag = true;
                        d = 0; //데이터 Shift
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
            if (Draw_flag == true)
            {
                COP_Draw.Children.Clear();
                //COP Total
                if ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5] + Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[1] * posx[0] + Tempo_FSR[2] * posx[1] + Tempo_FSR[3] * posx[2] + Tempo_FSR[4] * posx[3] + Tempo_FSR[5] * posx[4] + Tempo_FSR[6] * posx[5] + Tempo_FSR[7] * posx[6] + Tempo_FSR[8] * posx[7] + Tempo_FSR[9] * posx[8] + Tempo_FSR[10] * posx[9]) * 50) / ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5] + Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[1] * posy[0] + Tempo_FSR[2] * posy[1] + Tempo_FSR[3] * posy[2] + Tempo_FSR[4] * posy[3] + Tempo_FSR[5] * posy[4] + Tempo_FSR[6] * posy[5] + Tempo_FSR[7] * posy[6] + Tempo_FSR[8] * posy[7] + Tempo_FSR[9] * posy[8] + Tempo_FSR[10] * posy[9]) * 100) / ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5] + Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(0);
                    COP_que.Enqueue(50);
                }

                //COP Right
                if ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[1] * posx[0] + Tempo_FSR[2] * posx[1] + Tempo_FSR[3] * posx[2] + Tempo_FSR[4] * posx[3] + Tempo_FSR[5] * posx[4]) * 50) / ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[1] * posy[0] + Tempo_FSR[2] * posy[1] + Tempo_FSR[3] * posy[2] + Tempo_FSR[4] * posy[3] + Tempo_FSR[5] * posy[4]) * 100) / ((Tempo_FSR[1] + Tempo_FSR[2] + Tempo_FSR[3] + Tempo_FSR[4] + Tempo_FSR[5]) * 0.7f * l), 1));
                }
                else
                {
                    COP_que.Enqueue(-40);
                    COP_que.Enqueue(50);
                }

                //COP Left
                if ((Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[6] * posx[5] + Tempo_FSR[7] * posx[6] + Tempo_FSR[8] * posx[7] + Tempo_FSR[9] * posx[8] + Tempo_FSR[10] * posx[9]) * 50) / ((Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR[6] * posy[5] + Tempo_FSR[7] * posy[6] + Tempo_FSR[8] * posy[7] + Tempo_FSR[9] * posy[8] + Tempo_FSR[10] * posy[9]) * 100) / ((Tempo_FSR[6] + Tempo_FSR[7] + Tempo_FSR[8] + Tempo_FSR[9] + Tempo_FSR[10]) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(40);
                    COP_que.Enqueue(50);
                }
                COP_D(COP_que);
                Draw_flag = false;
            }
        }

        public void Text_box_in(object sender, System.EventArgs e)
        {
            Time.Text = Convert.ToString(Tempo_FSR[0]);

            fsr6_box.Text = Convert.ToString(Tempo_FSR[1]);
            fsr7_box.Text = Convert.ToString(Tempo_FSR[2]);
            fsr8_box.Text = Convert.ToString(Tempo_FSR[3]);
            fsr9_box.Text = Convert.ToString(Tempo_FSR[4]);
            fsr10_box.Text = Convert.ToString(Tempo_FSR[5]);

            fsr1_box.Text = Convert.ToString(Tempo_FSR[6]);
            fsr2_box.Text = Convert.ToString(Tempo_FSR[7]);
            fsr3_box.Text = Convert.ToString(Tempo_FSR[8]);
            fsr4_box.Text = Convert.ToString(Tempo_FSR[9]);
            fsr5_box.Text = Convert.ToString(Tempo_FSR[10]);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Box_In_Event.Stop();
            COP_Draw_Event.Stop();
        }

    }
}