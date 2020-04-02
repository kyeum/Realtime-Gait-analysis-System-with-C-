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
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Collections;
using System.Media;
using System.Drawing;
using Microsoft.Speech.Synthesis;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        #region Member
        int Save_flag = 0;
        int x = 0;
        int cop_x = 0;
        double old_copx_x = 0;
        double old_copx_y = 100;
        double old_copy_y = 100;
        int address_flag = 0;

        int backswing_flag = 0;
        int backswing_time = 0;

        int downswing_flag = 0;
        int downswing_time = 0;

        int realese_flag = 0;
        int realese_time = 0;

        [DllImport("KERNEL32.DLL")]
        public static extern void Beep(int fre, int dura);
        SpeechSynthesizer speaker = new SpeechSynthesizer();

        #endregion Member

        #region Draw
        public void TrackBodyJoint(Body body, JointType s) // JointType을 인자로 받고 해당 Joint data를 얻어 점으로 표현 
        {
            Joint joint_tracked = body.Joints[s];

            if (joint_tracked.TrackingState == TrackingState.Tracked)
            {
                // 3D space point
                CameraSpacePoint jointPosition = joint_tracked.Position;

                // 2D space point
                System.Windows.Point point = new System.Windows.Point();

                // depth 좌표와 body 좌표가 다르기때문에 Coordinate mapping 해준다
                DepthSpacePoint depthPoint = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition);

                point.X = float.IsInfinity(depthPoint.X) ? 0 : depthPoint.X;
                point.Y = float.IsInfinity(depthPoint.Y) ? 0 : depthPoint.Y;

                Ellipse ellipse = new Ellipse // 점 표현 
                {
                    Fill = System.Windows.Media.Brushes.Red,
                    Width = 6,
                    Height = 6
                };

                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                can.Children.Add(ellipse);
            }
        }

        private void TrackBodyMainJoint(Body body, JointType s) // TrackBodyJoint와 같은 기능, 점의 layout을 더크고 다른 색으로 표현
        {
            Joint joint_tracked = body.Joints[s];

            if (joint_tracked.TrackingState == TrackingState.Tracked)
            {
                // 3D space point
                CameraSpacePoint jointPosition = joint_tracked.Position;

                // 2D space point
                System.Windows.Point point = new System.Windows.Point();

                DepthSpacePoint depthPoint = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition);

                point.X = float.IsInfinity(depthPoint.X) ? 0 : depthPoint.X;
                point.Y = float.IsInfinity(depthPoint.Y) ? 0 : depthPoint.Y;

                Ellipse ellipse = new Ellipse
                {
                    Fill = System.Windows.Media.Brushes.LightGreen,
                    Width = 8,
                    Height = 8
                };

                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                can.Children.Add(ellipse);

            }
        }

        private void DrawLine(Body body, JointType s1, JointType s2) // 두 joint 사이를 선으로 이어줌
        {

            Joint joint_tracked1 = body.Joints[s1];
            Joint joint_tracked2 = body.Joints[s2];

            if (joint_tracked1.TrackingState == TrackingState.NotTracked || joint_tracked2.TrackingState == TrackingState.NotTracked) return;

            CameraSpacePoint jointPosition1 = joint_tracked1.Position;
            CameraSpacePoint jointPosition2 = joint_tracked2.Position;

            // 2D space point
            System.Windows.Point point1 = new System.Windows.Point();
            System.Windows.Point point2 = new System.Windows.Point();

            DepthSpacePoint depthPoint1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition1);
            DepthSpacePoint depthPoint2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition2);

            point1.X = float.IsInfinity(depthPoint1.X) ? 0 : depthPoint1.X;
            point1.Y = float.IsInfinity(depthPoint1.Y) ? 0 : depthPoint1.Y;

            point2.X = float.IsInfinity(depthPoint2.X) ? 0 : depthPoint2.X;
            point2.Y = float.IsInfinity(depthPoint2.Y) ? 0 : depthPoint2.Y;

            Line line = new Line
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                StrokeThickness = 3,
                Stroke = new SolidColorBrush(Colors.Yellow)
            };

            linedr.Children.Add(line);

        }

        private void GetLength(Body body, JointType joint1, JointType joint2) // 두 joint 사이 거리를 계산해 [m] 단위로 표현, return 하지 않음    화면에 그리는 용도
        {

            double length = 0;
            double length2 = 0;

            Joint joint1_tracked = body.Joints[joint1];
            Joint joint2_tracked = body.Joints[joint2];


            // 3D space point
            CameraSpacePoint jointPosition1 = joint1_tracked.Position;
            CameraSpacePoint jointPosition2 = joint2_tracked.Position;


            // 2D space point
            System.Windows.Point tpoint1 = new System.Windows.Point();
            System.Windows.Point tpoint2 = new System.Windows.Point();

            DepthSpacePoint depthPoint1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition1);
            DepthSpacePoint depthPoint2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition2);

            tpoint1.X = float.IsInfinity(depthPoint1.X) ? 0 : depthPoint1.X;
            tpoint1.Y = float.IsInfinity(depthPoint1.Y) ? 0 : depthPoint1.Y;
            tpoint2.X = float.IsInfinity(depthPoint2.X) ? 0 : depthPoint2.X;
            tpoint2.Y = float.IsInfinity(depthPoint2.Y) ? 0 : depthPoint2.Y;


            length = Math.Sqrt(Math.Pow(jointPosition1.X - jointPosition2.X, 2) + Math.Pow(jointPosition1.Y - jointPosition2.Y, 2)
                   + Math.Pow(jointPosition1.Z - jointPosition2.Z, 2));
            /*
            length2 = Math.Sqrt(Math.Pow(body.Joints[joint1].Position.X - body.Joints[joint2].Position.X, 2) + Math.Pow(body.Joints[joint1].Position.Y - body.Joints[joint2].Position.Y, 2)
                    + Math.Pow(body.Joints[joint1].Position.Z - body.Joints[joint2].Position.Z, 2));*/


            TextBlock lengthText = new TextBlock(); // 거리를 TextBlock로 표현
            lengthText.Text = string.Format("{0} m", Math.Round(length, 3));
            lengthText.Foreground = System.Windows.Media.Brushes.Aqua;
            lengthText.FontSize = 12;

            Canvas.SetLeft(lengthText, 2 * (tpoint2.X + tpoint1.X) / 5);
            Canvas.SetTop(lengthText, (tpoint2.Y + tpoint1.Y) / 2);

            lengthtext.Children.Add(lengthText);

        }

        public double GetLengthNotText(Body body, JointType joint1, JointType joint2) // 두 joint 사이 거리를 계산해 double형으로 return   결과값을 뽑아내는 용도
        {

            double length = 0;

            Joint joint1_tracked = body.Joints[joint1];
            Joint joint2_tracked = body.Joints[joint2];


            // 3D space point
            CameraSpacePoint jointPosition1 = joint1_tracked.Position;
            CameraSpacePoint jointPosition2 = joint2_tracked.Position;


            // 2D space point
            System.Windows.Point tpoint1 = new System.Windows.Point();
            System.Windows.Point tpoint2 = new System.Windows.Point();

            DepthSpacePoint depthPoint1 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition1);
            DepthSpacePoint depthPoint2 = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(jointPosition2);

            tpoint1.X = float.IsInfinity(depthPoint1.X) ? 0 : depthPoint1.X;
            tpoint1.Y = float.IsInfinity(depthPoint1.Y) ? 0 : depthPoint1.Y;
            tpoint2.X = float.IsInfinity(depthPoint2.X) ? 0 : depthPoint2.X;
            tpoint2.Y = float.IsInfinity(depthPoint2.Y) ? 0 : depthPoint2.Y;


            length = Math.Sqrt(Math.Pow(jointPosition1.X - jointPosition2.X, 2) + Math.Pow(jointPosition1.Y - jointPosition2.Y, 2)
                + Math.Pow(jointPosition1.Z - jointPosition2.Z, 2));

            return length;

        }


        public static double X_factor(Joint Left_Shoulder, Joint Right_Shoulder, Joint Left_Hip, Joint Right_Hip) // X-factor 연산, 벡터 내적 이용
        {
            double X_factor = 0;
            double svX = Left_Shoulder.Position.X - Right_Shoulder.Position.X;
            double svY = Left_Shoulder.Position.Y - Right_Shoulder.Position.Y;
            double svZ = Left_Shoulder.Position.Z - Right_Shoulder.Position.Z;
            double sv = vectorNorm(svX, svY, svZ);
            double hvX = Left_Hip.Position.X - Right_Hip.Position.X;
            double hvY = Left_Hip.Position.Y - Right_Hip.Position.Y;
            double hvZ = Left_Hip.Position.Z - Right_Hip.Position.Z;
            double hv = vectorNorm(hvX, hvY, hvZ);
            double IP = svX * hvX + svY * hvY + svZ * hvZ;
            double x = IP / (sv * hv);

            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    X_factor = angleRad * (180.0 / Math.PI);

                    /*if (j2.Position.Y < j3.Position.Y)
                    {
                        Angulo = 360 - Angulo;
                    }*/
                }
                else
                    X_factor = 0;

            }
            else
                X_factor = 0;


            return X_factor;
        }

        public static double Point_deg(Joint Left_Shoulder, Joint Right_Shoulder, Joint Left_Hip, Joint Right_Hip) // 각도 연산, 벡터 내적 이용
        {
            double X_factor = 0;
            double svX = Left_Shoulder.Position.X - Right_Shoulder.Position.X;
            double svY = Left_Shoulder.Position.Y - Right_Shoulder.Position.Y;
            double svZ = Left_Shoulder.Position.Z - Right_Shoulder.Position.Z;
            double sv = vectorNorm(svX, svY, svZ);
            double hvX = Left_Hip.Position.X - Right_Hip.Position.X;
            double hvY = Left_Hip.Position.Y - Right_Hip.Position.Y;
            double hvZ = Left_Hip.Position.Z - Right_Hip.Position.Z;
            double hv = vectorNorm(hvX, hvY, hvZ);
            double IP = svX * hvX + svY * hvY + svZ * hvZ;
            double x = IP / (sv * hv);

            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    X_factor = angleRad * (180.0 / Math.PI);

                    /*if (j2.Position.Y < j3.Position.Y)
                    {
                        Angulo = 360 - Angulo;
                    }*/
                }
                else
                    X_factor = 0;

            }
            else
                X_factor = 0;


            return X_factor;
        }

        private static double vectorNorm(double x, double y, double z) // 벡터 크기 연산
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
        }
        #endregion Member


        #region Phase
         
        /*
        public void Sound_play() //시작소리를 설정해준다
        {
            SoundPlayer sound = new SoundPlayer(@"C:\Users\Choong Hyun Kim\Desktop\sound\Lets go.wav");
            sound.Play();
        }

        public void Sound_ok() //끝소리를 설정해준다
        {
            SoundPlayer sound = new SoundPlayer(@"C:\Users\Choong Hyun Kim\Desktop\sound\ok1.wav");
            sound.Play();
        }*/


        public  Double Array_Max(ArrayList COPx1) //배열 최대값 찾기
        {
            Double max = 10;
           foreach (object obj in COPx1)
            {
                if(max<Convert.ToDouble(obj))
                {
                    max = Convert.ToDouble(obj);
                }
            }
            max=Math.Round(max, 3);
            return max;
        }

        public Double Array_Min(ArrayList COPx1) //배열 최소값 찾기
        {
            Double min = 60;
            foreach (object obj in COPx1)
            {
                if (min > Convert.ToDouble(obj))
                {
                    min = Convert.ToDouble(obj);
                }
            }
            min = Math.Round(min, 3);
            return min;
        }

        public void LineDraw(Double X1, Double Y1, Double X2, Double Y2)  //선그리기
        {
            Line MyLine = new Line();
            MyLine.Stroke = System.Windows.Media.Brushes.DarkTurquoise;
            MyLine.X1 = X1;
            MyLine.Y1 = Y1;
            MyLine.X2 = X2;
            MyLine.Y2 = Y2;
            MyLine.HorizontalAlignment = HorizontalAlignment.Left;
            MyLine.VerticalAlignment = VerticalAlignment.Center;
            MyLine.StrokeThickness = 2;
            COP_Draw.Children.Add(MyLine);
        }


        private void saveImage()  // 이미지 저장함수 : .tif 형식으로 저장, filename 변수를 바꾸어 경로변경가능
        {
            name = text.Text;
            string filename = @"C:\Users\Choong Hyun Kim\Documents\kinnecttest\" + name + "_" + frame_number + "_frame.tif";
            RenderTargetBitmap renderBitmap1 = new RenderTargetBitmap((int)ca.ActualWidth, (int)ca.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            ca.Measure(new System.Windows.Size((int)ca.ActualWidth, (int)ca.ActualHeight));
            ca.Arrange(new Rect(new System.Windows.Size((int)ca.ActualWidth+286, (int)ca.ActualHeight)));

            renderBitmap1.Render(ca);

            RenderTargetBitmap renderBitmap2 = new RenderTargetBitmap((int)can.ActualWidth, (int)can.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            can.Measure(new System.Windows.Size((int)can.ActualWidth, (int)can.ActualHeight));
            can.Arrange(new Rect(new System.Windows.Size((int)can.ActualWidth+286, (int)can.ActualHeight)));

            renderBitmap1.Render(can);

            RenderTargetBitmap renderBitmap3 = new RenderTargetBitmap((int)linedr.ActualWidth, (int)linedr.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            linedr.Measure(new System.Windows.Size((int)linedr.ActualWidth, (int)linedr.ActualHeight));
            linedr.Arrange(new Rect(new System.Windows.Size((int)linedr.ActualWidth+286, (int)linedr.ActualHeight)));

            renderBitmap1.Render(linedr);


            var dg = new DrawingGroup();
            var id1 = new ImageDrawing(renderBitmap1, new Rect(0, 0, renderBitmap1.Width, renderBitmap1.Height));
            var id2 = new ImageDrawing(renderBitmap2, new Rect(0, 0, renderBitmap2.Width, renderBitmap2.Height));
            var id3 = new ImageDrawing(renderBitmap2, new Rect(0, 0, renderBitmap2.Width, renderBitmap2.Height));

            dg.Children.Add(id1);
            //dg.Children.Add(id2);
            //dg.Children.Add(id3);

            // 2개의 layout source(layout root , canvas)를 한 이미지로 표현

            var combinedImg = new RenderTargetBitmap((int)renderBitmap1.Width, (int)renderBitmap1.Height, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }

            combinedImg.Render(dv);

            PngBitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(combinedImg));

            using (System.IO.FileStream file = System.IO.File.Create(filename))
            {
                encoder.Save(file);
            }
        }

        private void savecop()  // cop이미지 저장함수 : .tif 형식으로 저장, filename 변수를 바꾸어 경로변경가능
        {
            name = text.Text;
            string filename = @"C:\Users\Choong Hyun Kim\Documents\kinnecttest\" + name + "_cop_frame.tif";
            RenderTargetBitmap renderBitmap2 = new RenderTargetBitmap((int)COP_Draw.ActualWidth+286, (int)COP_Draw.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
            COP_Draw.Measure(new System.Windows.Size((int)COP_Draw.ActualWidth, (int)COP_Draw.ActualHeight));
            COP_Draw.Arrange(new Rect(new System.Windows.Size((int)COP_Draw.ActualWidth, (int)COP_Draw.ActualHeight)));

            renderBitmap2.Render(COP_Draw);

            var dg = new DrawingGroup();
            var id2 = new ImageDrawing(renderBitmap2, new Rect(0, 0, renderBitmap2.Width, renderBitmap2.Height));

            dg.Children.Add(id2);

            //2개의 layout source(layout root, canvas)를 한 이미지로 표현

            var combinedImg = new RenderTargetBitmap((int)renderBitmap2.Width, (int)renderBitmap2.Height, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }

            combinedImg.Render(dv);

            PngBitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(combinedImg));

            using (System.IO.FileStream file = System.IO.File.Create(filename))
            {
                encoder.Save(file);
            }
        }
    }
}
#endregion Member
