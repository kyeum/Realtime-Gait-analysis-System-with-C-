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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.IO.Ports;
using System.Collections;
using System.Windows.Threading;
using System.Threading;
using CRC;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        SerialPort serial = new SerialPort(); //Main Serial
        SerialPort arduino = new SerialPort(); //Analog output
        List<int> Buff_Rec_2 = new List<int>();
        Queue COP_que = new Queue(); // COP Draw

        System.IO.StreamWriter file_serial = null;

        int[] Rec_Check = new int[46];
        int[] Tempo_FSR = new int[20];
        int[] Tempo_FSR2 = new int[20];
        int[] Tempo_FSR3 = new int[20];
        byte[] test = new byte[40];
        byte[] Crc;

        float[] posx = new float[10]; //COP 계산 변수
        float[] posy = new float[10];
        float w = 93.5f;
        float l = 258.4f;
        bool Draw_flag = false; //Start, Stop 버튼 클릭    

        delegate void SetTextCallBack(String text);
        private Crc crc;

        public object SystemGlobalization { get; private set; }

        void InitSerialPort(object sender, EventArgs e)
        {
            serial.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                Comport_num.Items.Add(port);
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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] names = cbComSpeed.SelectedItem.ToString().Split(':');
            serial.BaudRate = int.Parse(names[1].ToString().Trim());
        }


        void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            int intRecSize = serial.BytesToRead;
            if (intRecSize != 0)
            {
                try
                {
                    for (int i = 0; i < serial.BytesToRead; i++)
                    {
                        Buff_Rec_2.Add((Byte)serial.ReadByte());
                    }
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate { process2(); }));
                }
                catch { }
            }
        }

        private void process2()
        {
            try
            {
                for (int d = 0; d < Buff_Rec_2.Count; d++)
                {
                    if (Buff_Rec_2[d] == 255 && Buff_Rec_2[d+1] == 255) //FF, FF 확인
                    {
                        Buff_Rec_2.RemoveRange(0, d); //FF, FF전 데이터 모두삭제
                        /*
                        test.Initialize();
                        for (int e = 0; e < 40; e++)
                        {
                          test[e] = Convert.ToByte(Buff_Rec_2[e + 2]);
                          //file_serial.Write("{0} ", Convert.ToString(test[e], 16));
                        }
                        Crc = crc.ComputeHash(test);
                        /*
                        for (int e = 0; e < Crc.Length; e++)
                        {
                            file.Write("{0} ", Convert.ToString(Crc[e],16));
                        }
                        file.WriteLine();
                        file.Write("{0} ", Convert.ToString(Buff_Rec_2[42], 16));
                        file.Write("{0} ", Convert.ToString(Buff_Rec_2[43], 16));
                        file.WriteLine();*/

                        if (Buff_Rec_2[44] == 255 && Buff_Rec_2[45] == 254) //FF, FE 확인, Checksum 확인 ) && (Crc[6] == Buff_Rec_2[42] && Crc[7] == Buff_Rec_2[43])
                        {
                            for (int i = 0; i < 46; i++)
                            {
                                Rec_Check[i] = Buff_Rec_2[i];
                            }      

                            //시간                    
                            Tempo_FSR[0] = (Rec_Check[2] << 8) + Rec_Check[3];
                           // Tempo_FSR[0] = time;
                            if (Save_flag == 1)
                            {
                                file_serial.Write("{0}", Tempo_FSR[0]);
                                file_serial.Write(" {0}", DateTime.Now.ToString("ss.fff"));
                            }
                            //오른발
                            Tempo_FSR[1] = (Rec_Check[4] << 8) + Rec_Check[5];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[1]);                       
                            }
                            Tempo_FSR[2] = (Rec_Check[6] << 8) + Rec_Check[7];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[2]);
                            }
                            Tempo_FSR[3] = (Rec_Check[8] << 8) + Rec_Check[9];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[3]);
                            }
                            Tempo_FSR[4] = (Rec_Check[10] << 8) + Rec_Check[11];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[4]);
                            }
                            Tempo_FSR[5] = (Rec_Check[12] << 8) + Rec_Check[13];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[5]);
                            }

                            //왼발                      
                            Tempo_FSR[6] = (Rec_Check[14] << 8) + Rec_Check[15];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[6]);
                            }
                            Tempo_FSR[7] = (Rec_Check[16] << 8) + Rec_Check[17];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[7]);
                            }
                            Tempo_FSR[8] = (Rec_Check[18] << 8) + Rec_Check[19];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[8]);
                            }
                            Tempo_FSR[9] = (Rec_Check[20] << 8) + Rec_Check[21];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[9]);
                            }
                            Tempo_FSR[10] = (Rec_Check[22] << 8) + Rec_Check[23];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[10]);
                            }

                            //IMU acc
                            Tempo_FSR[11] = (Rec_Check[24] << 8) + Rec_Check[25];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[11]);
                            }
                            Tempo_FSR[12] = (Rec_Check[26] << 8) + Rec_Check[27];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[12]);
                            }
                            Tempo_FSR[13] = (Rec_Check[28] << 8) + Rec_Check[29];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[13]);
                            }

                            //IMU angle acc
                            Tempo_FSR[14] = (Rec_Check[30] << 8) + Rec_Check[31];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[14]);
                            }
                            Tempo_FSR[15] = (Rec_Check[32] << 8) + Rec_Check[33];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[15]);
                            }
                            Tempo_FSR[16] = (Rec_Check[34] << 8) + Rec_Check[35];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[16]);
                            }

                            //IMU mag
                            Tempo_FSR[17] = (Rec_Check[36] << 8) + Rec_Check[37];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[17]);
                            }
                            Tempo_FSR[18] = (Rec_Check[38] << 8) + Rec_Check[39];
                            if (Save_flag == 1)
                            {
                                file_serial.Write(" {0}", Tempo_FSR[18]);
                            }
                            Tempo_FSR[19] = (Rec_Check[40] << 8) + Rec_Check[41];
                            if (Save_flag == 1)
                            {
                                file_serial.WriteLine(" {0}", Tempo_FSR[19]);
                            }
                            Buffer.BlockCopy(Tempo_FSR, 0, Tempo_FSR2, 0, 80);
                            Buffer.BlockCopy(Tempo_FSR, 0, Tempo_FSR3, 0, 80);
                            Buff_Rec_2.RemoveRange(0, 45); //사용 데이터 모두삭제
                            Draw_flag = true;
                        }
                        d = 0; //데이터 Shift
                    }
                }
            }
            catch { }
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

            Canvas.SetRight(ellipse, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Width * 0.47));
            Canvas.SetBottom(ellipse, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Height * 0.5 - 230));

            Canvas.SetRight(ellipse_R, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Width * 0.6));
            Canvas.SetBottom(ellipse_R, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Height * 0.5 - 230));

            Canvas.SetRight(ellipse_L, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Width * 0.37));
            Canvas.SetBottom(ellipse_L, Convert.ToDouble(COP_que.Dequeue()) * 4 + (COP_Draw.Height * 0.5 - 230));

            COP_Draw.Children.Add(ellipse_L);
            COP_Draw.Children.Add(ellipse_R);
            COP_Draw.Children.Add(ellipse);
        }

        public void COP_Cal(object sender, System.EventArgs e)
        {
            if (Draw_flag == true)
            {
                //COP_Draw.Children.Clear();

                //COP Total
                if ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5] + Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[1] * posx[0] + Tempo_FSR3[2] * posx[1] + Tempo_FSR3[3] * posx[2] + Tempo_FSR3[4] * posx[3] + Tempo_FSR3[5] * posx[4] + Tempo_FSR3[6] * posx[5] + Tempo_FSR3[7] * posx[6] + Tempo_FSR3[8] * posx[7] + Tempo_FSR3[9] * posx[8] + Tempo_FSR3[10] * posx[9]) * 50) / ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5] + Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[1] * posy[0] + Tempo_FSR3[2] * posy[1] + Tempo_FSR3[3] * posy[2] + Tempo_FSR3[4] * posy[3] + Tempo_FSR3[5] * posy[4] + Tempo_FSR3[6] * posy[5] + Tempo_FSR3[7] * posy[6] + Tempo_FSR3[8] * posy[7] + Tempo_FSR3[9] * posy[8] + Tempo_FSR3[10] * posy[9]) * 100) / ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5] + Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) * (0.7f * l)), 1));
                }
                else
                {
                    COP_que.Enqueue(0);
                    COP_que.Enqueue(50);
                }

                //COP Right
                if ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[1] * posx[0] + Tempo_FSR3[2] * posx[1] + Tempo_FSR3[3] * posx[2] + Tempo_FSR3[4] * posx[3] + Tempo_FSR3[5] * posx[4]) * 50) / ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[1] * posy[0] + Tempo_FSR3[2] * posy[1] + Tempo_FSR3[3] * posy[2] + Tempo_FSR3[4] * posy[3] + Tempo_FSR3[5] * posy[4]) * 100) / ((Tempo_FSR3[1] + Tempo_FSR3[2] + Tempo_FSR3[3] + Tempo_FSR3[4] + Tempo_FSR3[5]) * 0.7f * l), 1));
                }
                else
                {
                    COP_que.Enqueue(-40);
                    COP_que.Enqueue(50);
                }

                //COP Left
                if ((Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) > 30)
                {
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[6] * posx[5] + Tempo_FSR3[7] * posx[6] + Tempo_FSR3[8] * posx[7] + Tempo_FSR3[9] * posx[8] + Tempo_FSR3[10] * posx[9]) * 50) / ((Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) * (162.35f + 0.25f * w)), 1));
                    COP_que.Enqueue(Math.Round(((Tempo_FSR3[6] * posy[5] + Tempo_FSR3[7] * posy[6] + Tempo_FSR3[8] * posy[7] + Tempo_FSR3[9] * posy[8] + Tempo_FSR3[10] * posy[9]) * 100) / ((Tempo_FSR3[6] + Tempo_FSR3[7] + Tempo_FSR3[8] + Tempo_FSR3[9] + Tempo_FSR3[10]) * (0.7f * l)), 1));
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
            if (Draw_flag == true)
            {
                Time.Text = Tempo_FSR2[0].ToString();
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
                Draw_flag = false;
            }
         //   System.Windows.Forms.Application.DoEvents();

        }
    }
 }

