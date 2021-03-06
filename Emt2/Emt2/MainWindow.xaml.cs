﻿using System;
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

namespace Emt2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _KinectDevice;
        private readonly Brush[] _SkeletonBrushes;
        private Skeleton[] _FrameSkeletons;
        #endregion Member Variables

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            this._SkeletonBrushes = new[] { Brushes.Black, Brushes.Crimson, Brushes.Indigo, Brushes.DodgerBlue, Brushes.Purple, Brushes.Pink };
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }
        #endregion Constructor

        #region Methods
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }
        #endregion Methods

        #region Properties
        public KinectSensor KinectDevice
        {
            get { return this._KinectDevice; }
            set
            {
                if (this._KinectDevice != value)
                {
                    //Uninitialize
                    if (this._KinectDevice != null)
                    {
                        this._KinectDevice.Stop();
                        this._KinectDevice.SkeletonFrameReady -= KinectDevice_SkeletonFrameReady;
                        this._KinectDevice.SkeletonStream.Disable();
                        this._FrameSkeletons = null;
                    }
                    this._KinectDevice = value;
                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            this._KinectDevice.SkeletonStream.Enable();
                            this._KinectDevice.ColorStream.Enable();
                            this._FrameSkeletons = new Skeleton[this._KinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                            this.KinectDevice.SkeletonFrameReady += KinectDevice_SkeletonFrameReady;
                            this.KinectDevice.ColorFrameReady += KinectDevice_ColorFrameReady;
                            this._KinectDevice.Start();
                        }
                    }
                }
            }
        }
        #endregion Properties

        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    Polyline figure;
                    Brush userBrush;
                    Skeleton skeleton;
                    JointType[] joints;

                    LayoutRoot.Children.Clear();
                    frame.CopySkeletonDataTo(this._FrameSkeletons);
                    for (int i = 0; i < this._FrameSkeletons.Length; i++)
                    {
                        skeleton = this._FrameSkeletons[i];
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            userBrush = this._SkeletonBrushes[i % this._SkeletonBrushes.Length];
                            //Draws the skeleton’s head and torso
                            joints = new[] {
                                            JointType.Head,
                                            JointType.ShoulderCenter,
                                            JointType.ShoulderLeft,
                                            JointType.Spine,
                                            JointType.ShoulderRight,
                                            JointType.ShoulderCenter,
                                            JointType.HipCenter,
                                            JointType.HipLeft,
                                            JointType.Spine,
                                            JointType.HipRight,
                                            JointType.HipCenter
                            };
                            LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                            //Draws the skeleton’s left leg
                            joints = new[] {
                                            JointType.HipLeft,
                                            JointType.KneeLeft,
                                            JointType.AnkleLeft,
                                            JointType.FootLeft
                            };
                            LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                            //Draws the skeleton’s right leg
                            joints = new[] {
                                            JointType.HipRight,
                                            JointType.KneeRight,
                                            JointType.AnkleRight,
                                            JointType.FootRight };
                            LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                            //Draws the skeleton’s left arm
                            joints = new[] {
                                            JointType.ShoulderLeft,
                                            JointType.ElbowLeft,
                                            JointType.WristLeft,
                                            JointType.HandLeft };
                            LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                            //Draws the skeleton’s right arm
                            joints = new[] {
                                            JointType.ShoulderRight,
                                            JointType.ElbowRight,
                                            JointType.WristRight,
                                            JointType.HandRight };
                            LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                        }
                    }
                }
            }
        }

        private void KinectDevice_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);

                    ImageBrush ib = new ImageBrush(BitmapImage.Create(
                                                                frame.Width, frame.Height, 96, 96,
                                                                PixelFormats.Bgr32, null, pixelData,
                                                                frame.Width * frame.BytesPerPixel
                                                                ));
                    this.LayoutRoot.Background = ib;
                }
            }
        }

        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();
            figure.StrokeThickness = 8;
            figure.Stroke = brush;
            for (int i = 0; i < joints.Length; i++)
            {
                figure.Points.Add(GetJointPoint(skeleton.Joints[joints[i]]));
            }
            return figure;
        }

        private Point GetJointPoint(Joint joint)
        {
            DepthImagePoint point = this.KinectDevice.MapSkeletonPointToDepth(joint.Position, this.KinectDevice.DepthStream.Format);
            point.X *= (int)this.LayoutRoot.ActualWidth / this.KinectDevice.DepthStream.FrameWidth;
            point.Y *= (int)this.LayoutRoot.ActualHeight / this.KinectDevice.DepthStream.FrameHeight;
            return new Point(point.X, point.Y);
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            if(_KinectDevice.ElevationAngle + 5 <= _KinectDevice.MaxElevationAngle)
            {
                _KinectDevice.ElevationAngle = _KinectDevice.ElevationAngle + 5;
            } else
            {
                setDefaultAngle();
            }
        }

        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            if(_KinectDevice.ElevationAngle - 5 >= _KinectDevice.MinElevationAngle)
            {
                _KinectDevice.ElevationAngle = _KinectDevice.ElevationAngle - 5;
            } else
            {
                setDefaultAngle();
            }
        }

        private void setDefaultAngle()
        {
            _KinectDevice.ElevationAngle = 0;
        }
    }
}
