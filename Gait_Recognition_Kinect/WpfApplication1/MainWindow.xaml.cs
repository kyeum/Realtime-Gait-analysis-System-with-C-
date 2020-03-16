using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using Microsoft.Kinect;
using System.IO.Ports;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        #region Member
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        method Save_Kinect;
        Queue BodyJoint_que = new Queue();

        public int flag = 0;
        public int startflag = 0;
        public int endflag = 0;
        public int drawflag = 0;
        public int Momentumflag = 0;
        public string sum_gsRecvData;
        private ulong currTrackingId = 0;
        public int time_kinect = 0;


        string name = "test";
        string name2;
        int file_num = 1;
        string Path_Selected;
        string Save_Path = @"C:\Users\a\Desktop\DESKTOP\";  //default 경로: 바탕화면

        System.IO.StreamWriter file = null;
        double frame_number = 1;
        double tmp = 0;
        static float H = 1730f;
        float w1 = 0.055f * H;
        float l1 = 0.152f * H;
        int time;

        System.Windows.Threading.DispatcherTimer Box_In_Event = new System.Windows.Threading.DispatcherTimer();
        System.Windows.Threading.DispatcherTimer COP_Draw_Event = new System.Windows.Threading.DispatcherTimer();
        #endregion Member

        #region Constructor

        public MainWindow()  //초기화
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(InitSerialPort);  //Serial 받기

            Box_In_Event.Interval = new TimeSpan(0, 0, 0, 0, 40);
            Box_In_Event.Tick += new EventHandler(Text_box_in);
            Box_In_Event.Start();

            COP_Draw_Event.Interval = new TimeSpan(0, 0, 0, 0, 30);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;  // Depth, Color, Body 모두 이용

            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }
            if (_sensor != null)
            {
                _sensor.Close();
            }

            Box_In_Event.Stop();
            COP_Draw_Event.Stop();
        }

        #endregion Constructor

        #region method
        private Body GetActiveBody() //제일 가까운 사람 인식 함수 딱 1사람만 인식
        {
            if (currTrackingId <= 0)
            {
                foreach (Body body in this._bodies)
                {
                    if (body.IsTracked)
                    {
                        currTrackingId = body.TrackingId;
                        return body;
                    }
                }

                return null;
            }
            else
            {
                foreach (Body body in this._bodies)
                {
                    if (body.IsTracked && body.TrackingId == currTrackingId)
                    {
                        return body;
                    }
                }
            }

            currTrackingId = 0;
            return GetActiveBody();
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    ca.Source = frame.ToBitmap();
                    
                }
            }

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    can.Children.Clear();
                    linedr.Children.Clear();
                    lengthtext.Children.Clear();
            
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];
                    Save_Kinect = new method();
                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if ((body.IsTracked))
                            {                                                      
                                TrackBodyJoint(body, JointType.HipLeft);
                                TrackBodyJoint(body, JointType.HipRight);
                                TrackBodyJoint(body, JointType.KneeLeft);
                                TrackBodyJoint(body, JointType.KneeRight);
                                TrackBodyJoint(body, JointType.ShoulderLeft);
                                TrackBodyJoint(body, JointType.ShoulderRight);
                                TrackBodyJoint(body, JointType.SpineBase);
                                TrackBodyJoint(body, JointType.SpineShoulder);
                                TrackBodyJoint(body, JointType.WristLeft);
                                TrackBodyJoint(body, JointType.WristRight);
                                TrackBodyJoint(body, JointType.AnkleLeft);
                                TrackBodyJoint(body, JointType.AnkleRight);
                                TrackBodyJoint(body, JointType.ElbowLeft);
                                TrackBodyJoint(body, JointType.ElbowRight);
                                TrackBodyJoint(body, JointType.Head);
                                TrackBodyJoint(body, JointType.Neck);
                                TrackBodyJoint(body, JointType.SpineMid);
                                TrackBodyJoint(body, JointType.FootLeft);
                                TrackBodyJoint(body, JointType.FootRight);
                                TrackBodyMainJoint(body, JointType.HandLeft);
                                TrackBodyMainJoint(body, JointType.HandRight);
                                

                                 DrawLine(body, JointType.SpineShoulder, JointType.SpineBase);
                                 DrawLine(body, JointType.ShoulderLeft, JointType.ElbowLeft);
                                 DrawLine(body, JointType.ElbowLeft, JointType.WristLeft);
                                 DrawLine(body, JointType.ShoulderRight, JointType.ElbowRight);
                                 DrawLine(body, JointType.ElbowRight, JointType.WristRight);
                                 DrawLine(body, JointType.AnkleLeft, JointType.FootLeft);

                                 DrawLine(body, JointType.HipLeft, JointType.KneeLeft);
                                 DrawLine(body, JointType.KneeLeft, JointType.AnkleLeft);
                                 DrawLine(body, JointType.HipRight, JointType.KneeRight);
                                 DrawLine(body, JointType.KneeRight, JointType.AnkleRight);

                                if (Save_flag == 1)
                                {                                 
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HipLeft], frame_number, "HipLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HipRight], frame_number, "HipRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.KneeLeft], frame_number, "KneeLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.KneeRight], frame_number, "KneeRight", time);                               
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ShoulderLeft], frame_number, "ShoulderLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ShoulderRight], frame_number, "ShoulderRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.SpineBase], frame_number, "SpineBase", time);                                   
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.SpineShoulder], frame_number, "SpineShoulder", time);                                   
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.WristLeft], frame_number, "WristLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.WristRight], frame_number, "WristRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.AnkleLeft], frame_number, "AnkleLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.AnkleRight], frame_number, "AnkleRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ElbowLeft], frame_number, "ElbowLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ElbowRight], frame_number, "ElbowRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.Head], frame_number, "head", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.Neck],frame_number, "Neck", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.SpineMid], frame_number,"SpineMid", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.FootLeft], frame_number, "FootLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.FootRight], frame_number, "FootRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HandLeft],frame_number, "HandLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HandRight], frame_number,"HandRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ThumbLeft],frame_number, "ThumbLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.ThumbRight],frame_number, "ThumbRight", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HandTipLeft],frame_number, "HandTipLeft", time);
                                    Save_Kinect.WriteJointData(file, body.Joints[JointType.HandTipRight], frame_number,"HandTipRight", time);
                                    //if (frame_number % 10 == 0)
                                    // saveImage();

                                    frame_number++;
                                    if (frame_number > tmp)
                                    {
                                        tmp++;
                                        file.WriteLine();
                                    }
                                }
                                if (flag == 0)
                                {
                                    frame_number = 1;
                                    tmp = 0;                                 
                                    return;
                                }
                                
                            }
                        }
                    }
                }
            }
        }
        private void save_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                name = text.Text;
                file_num = Convert.ToInt16(Regex.Replace(name, @"\D", ""));
                name2 = Regex.Replace(name, @"[\d-]", string.Empty);
                file = new System.IO.StreamWriter(Save_Path + name + "_Kinect" + ".txt");//저장 경로 // append: true 파일모드로 추가해주고 싶을때 넣어준다.
                file_serial = new System.IO.StreamWriter(Save_Path + name + "_Serial" + ".txt", append: true); // 저장 경로 
                Save_flag = 1;
                flag = 1;
               // Sound_play();

                COP_Draw.Children.Clear();
            }
            catch (System.UnauthorizedAccessException)
            {
                System.Windows.MessageBox.Show("파일을 저장할 경로를 먼저 설정하세요.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch(System.IO.IOException)
            {
                System.Windows.MessageBox.Show("1번만 누르세요.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               // Sound_ok();
                Save_flag = 0;
                flag = 0;

                COP_que.Clear();
                file_serial.Close();
                COP_Draw.Children.Clear();
                file.Close();    
                text.Text = name2 + Convert.ToString(file_num + 1);
            }
            catch(System.NullReferenceException)
            {
                System.Windows.MessageBox.Show("Save X.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion method

        private void Path_Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            Path_Selected = dialog.SelectedPath;
            Save_Path = @"" + Path_Selected + "\\";
        }
    }
}

