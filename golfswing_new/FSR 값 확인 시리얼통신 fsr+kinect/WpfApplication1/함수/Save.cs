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

namespace WpfApplication1
{
    public class method
    {
        public void WriteJointData(System.IO.StreamWriter file, Joint joint,double frame_number, string JointName, int Time) //메모장으로 값을 저장
        {
            double z = joint.Position.Z;
            if (joint.Position.Z == 0)
            {
                return;
            }
           
            file.Write(JointName + "{0} {1} {2} {3} {4} {5} ", frame_number,Time, DateTime.Now.ToString("ss.fff"), joint.Position.X, joint.Position.Y, joint.Position.Z); // 해당 Joint의 좌표
            }
    }

        
}

