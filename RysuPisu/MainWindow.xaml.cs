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
using System.Threading;

namespace RysuPisu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum Gesty
        {
            None,
            Rozszerzone,
            Chwyt,
            Machanie
        };
        private Gesty gest;
        private Thread processingThread;
        private PXCMSenseManager senseMenager;
        private Int32 nhands;
        private Int32 handID;
        private float handTipX;
        private float handTipY;
        private float handTipZ;
        private string detectAlert;
        private string calibAlert;
        private string borderAlert;
        private bool detectStatOK;
        private bool calibStatOK;
        private bool borderStatOK;
        private PXCMHandModule hand;
        private PXCMHandData handData;
        private PXCMHandData.GestureData gestData;
        private PXCMHandData.IHand ihand;
        private PXCMHandData.JointData[][] nodes;
        private PXCMHandConfiguration handConfig;
        private const int MaxPointSize = 30;
        private const int drawingAreaHeight = 600;
        private const int drawingAreaWidth = 600;
        private const int tabSize = 30;
        private float[] xValues;
        private float[] yValues;
        private float[] zValues;
        private int xyzValueIndex;
        Point cPoint;
        Brush ColorBrush;

        private enum PisakColor
        {
            None,
            Czerwony,
            Zolty,
            Zielony,
            Nebieski,
            Fioletowy,
            Pomarancz,
            Brazowy,
            Czarny
        };
        public MainWindow()
        {
            InitializeComponent();
            PisakColorChange(PisakColor.Czerwony);
            nodes = new PXCMHandData.JointData[][] { new PXCMHandData.JointData[0x20], new PXCMHandData.JointData[0x20] };
            cPoint = new Point();
            gest = Gesty.None;
            xValues = new float[tabSize];
            yValues = new float[tabSize];
            zValues = new float[tabSize];
            xyzValueIndex = 0;
            detectAlert = string.Empty;
            calibAlert = string.Empty;
            borderAlert = string.Empty;
            detectStatOK = false;
            calibStatOK = false;
            borderStatOK = false;
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            senseMenager = PXCMSenseManager.CreateInstance();
            senseMenager.EnableHand();
            senseMenager.Init();
            ConfigHandModule();
            processingThread.Start();
        }
        private void ConfigHandModule()
        {
            hand = senseMenager.QueryHand();
            handConfig = hand.CreateActiveConfiguration();
            handConfig.EnableGesture("spreadfingers");
            handConfig.EnableGesture("two_fingers_pinch_open");
            handConfig.EnableGesture("wave");
            handConfig.EnableAllAlerts();
            handConfig.ApplyChanges();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            drawingArea.Height = drawingAreaHeight;
            drawingArea.Width = drawingAreaWidth;
            pisak.Height = 10;
            pisak.Width = 10;
            Canvas.SetTop(pisak, drawingAreaHeight / 2 - pisak.Height / 2);
            Canvas.SetRight(pisak, drawingAreaWidth / 2 - pisak.Width / 2);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            processingThread.Abort();
            if (handData != null) handData.Dispose();
            if (handConfig != null) handConfig.Dispose();
            senseMenager.Dispose();
        }
        private void ProcessingThread()
        {
            
            while (senseMenager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                hand = senseMenager.QueryHand();

                if (hand != null)
                {

                    
                    handData = hand.CreateOutput();
                    handData.Update();

                    
                    nhands = handData.QueryNumberOfHands();

                    if (nhands > 0)
                    {
                        
                        handData.QueryHandId(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, 0, out handID);

                        
                        handData.QueryHandDataById(handID, out ihand);

                        
                        for (int i = 0; i < nhands; i++)
                        {
                            for (int j = 0; j < 0x20; j++)
                            {
                                PXCMHandData.JointData jointData;
                                ihand.QueryTrackedJoint((PXCMHandData.JointType)j, out jointData);
                                nodes[i][j] = jointData;
                            }
                        }

                        
                        handTipX = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.x;
                        handTipY = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.y;
                        handTipZ = nodes[0][Convert.ToInt32(PXCMHandData.JointType.JOINT_MIDDLE_TIP)].positionWorld.z;

                        
                        if (handData.IsGestureFired("spreadfingers", out gestData)) { gest = Gesty.Rozszerzone; }
                        else if (handData.IsGestureFired("two_fingers_pinch_open", out gestData)) { gest = Gesty.Chwyt; }
                        else if (handData.IsGestureFired("wave", out gestData)) { gest = Gesty.Machanie; }
                    }
                    else
                    {
                        gest = Gesty.None;
                    }

                    
                    for (int i = 0; i < handData.QueryFiredAlertsNumber(); i++)
                    {
                        PXCMHandData.AlertData alertData;
                        if (handData.QueryFiredAlertData(i, out alertData) != pxcmStatus.PXCM_STATUS_NO_ERROR) { continue; }

                    
                        switch (alertData.label)
                        {
                            case PXCMHandData.AlertType.ALERT_HAND_DETECTED:
                                detectAlert = "Dłoń znaleziona";
                                detectStatOK = true;
                                break;
                            case PXCMHandData.AlertType.ALERT_HAND_NOT_DETECTED:
                                detectAlert = "Dłoń nie znaleziona";
                                detectStatOK = false;
                                break;
                            case PXCMHandData.AlertType.ALERT_HAND_CALIBRATED:
                                calibAlert = "Dłoń skalibrowana";
                                calibStatOK = true;
                                break;
                            case PXCMHandData.AlertType.ALERT_HAND_NOT_CALIBRATED:
                                calibAlert = "Dłoń nie skalibrowana";
                                calibStatOK = false;
                                break;
                            case PXCMHandData.AlertType.ALERT_HAND_INSIDE_BORDERS:
                                borderAlert = "Jestes w polu widzenia";
                                borderStatOK = true;
                                break;
                            case PXCMHandData.AlertType.ALERT_HAND_OUT_OF_BORDERS:
                                borderAlert = "Wyjechałeś!";
                                borderStatOK = false;
                                break;
                        }
                    }

                    UpdateUI();
                    if (handData != null) handData.Dispose();
                }
                senseMenager.ReleaseFrame();
            }
        }

        private void UpdateUI()
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {

                lKoordynaty.Content = string.Format("X: {0},   Y: {1}", handTipX, handTipY);


                if (xyzValueIndex < tabSize)
                {
                    xValues[xyzValueIndex] = handTipX;
                    yValues[xyzValueIndex] = handTipY;
                    zValues[xyzValueIndex] = handTipZ;
                    xyzValueIndex++;
                }
                else
                {
                    for (int i = 0; i < tabSize - 1; i++)
                    {
                        xValues[i] = xValues[i + 1];
                        yValues[i] = yValues[i + 1];
                        zValues[i] = zValues[i + 1];
                    }

                    xValues[tabSize - 1] = handTipX;
                    yValues[tabSize - 1] = handTipY;
                    zValues[tabSize - 1] = handTipZ;
                }

                
                float scaledZ = zValues.Average() * -100 + 50;

                if (scaledZ > MaxPointSize)
                {
                    pisak.Height = MaxPointSize;
                    pisak.Width = MaxPointSize;
                }
                else if (scaledZ < 0)
                {
                    pisak.Height = 0;
                    pisak.Width = 0;
                }
                else
                {
                    pisak.Height = scaledZ;
                    pisak.Width = scaledZ;
                }

                
                Canvas.SetRight(pisak, (xValues.Average() * 3000 + 300) - pisak.Width / 2);
                Canvas.SetTop(pisak, (yValues.Average() * -3000 + 300) - pisak.Height / 2);

                
                if ((gest == Gesty.Chwyt) && (handTipX >= -0.12))
                {
                    pisak.Stroke = ColorBrush;
                    pisak.Fill = ColorBrush;
                    Line line = new Line();
                    line.Stroke = ColorBrush;
                    line.StrokeThickness = scaledZ;
                    line.StrokeDashCap = PenLineCap.Round;
                    line.StrokeStartLineCap = PenLineCap.Round;
                    line.StrokeEndLineCap = PenLineCap.Round;
                    line.X1 = cPoint.X;
                    line.Y1 = cPoint.Y;
                    line.X2 = xValues.Average() * -3000 + 300;
                    line.Y2 = yValues.Average() * -3000 + 300;

                    cPoint.X = xValues.Average() * -3000 + 300;
                    cPoint.Y = yValues.Average() * -3000 + 300;
                    drawingArea.Children.Add(line);
                }
                else
                {
                    pisak.Stroke = ColorBrush;
                    pisak.Fill = Brushes.Transparent;
                    cPoint.X = xValues.Average() * -3000 + 300;
                    cPoint.Y = yValues.Average() * -3000 + 300;
                }

                // Erase canvas on hand wave
                if (gest == Gesty.Machanie)
                {
                    drawingArea.Children.Clear();
                    drawingArea.Children.Add(pisak);
                }
                
                if (handTipX < -0.12)
                {
                    if (gest == Gesty.Rozszerzone)
                    {
                        if ((handTipY <= 0.1) && (handTipY > 0.075))
                        {
                            PisakColorChange(PisakColor.Czerwony);
                        }
                        else if ((handTipY <= 0.075) && (handTipY > 0.05))
                        {
                            PisakColorChange(PisakColor.Zolty);
                        }
                        else if ((handTipY <= 0.05) && (handTipY > 0.025))
                        {
                            PisakColorChange(PisakColor.Zielony);
                        }
                        else if ((handTipY <= 0.025) && (handTipY > 0.0))
                        {
                            PisakColorChange(PisakColor.Nebieski);
                        }
                        else if ((handTipY <= 0.0) && (handTipY > -0.025))
                        {
                            PisakColorChange(PisakColor.Fioletowy);
                        }
                        else if ((handTipY <= -0.025) && (handTipY > -0.05))
                        {
                            PisakColorChange(PisakColor.Pomarancz);
                        }
                        else if ((handTipY <= -0.05) && (handTipY > -0.075))
                        {
                            PisakColorChange(PisakColor.Brazowy);
                        }
                        else if ((handTipY <= -0.075) && (handTipY > -0.1))
                        {
                            PisakColorChange(PisakColor.Czarny);
                        }
                    }
                }


                // Update gesture info
                switch (gest)
                {
                    case Gesty.None:
                        lRysuj.Foreground = Brushes.White;
                        lWymarz.Foreground = Brushes.White;
                        lPusc.Foreground = Brushes.White;
                        break;
                    case Gesty.Rozszerzone:
                        lRysuj.Foreground = Brushes.White;
                        lWymarz.Foreground = Brushes.White;
                        lPusc.Foreground = Brushes.LightGreen;
                        break;
                    case Gesty.Chwyt:
                        lRysuj.Foreground = Brushes.LightGreen;
                        lWymarz.Foreground = Brushes.White;
                        lPusc.Foreground = Brushes.White;
                        break;
                    case Gesty.Machanie:
                        lRysuj.Foreground = Brushes.White;
                        lWymarz.Foreground = Brushes.LightGreen;
                        lPusc.Foreground = Brushes.White;
                        break;
                }

                // Update alert info
                lDetectAlert.Foreground = (detectStatOK) ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Red;
                lDetectAlert.Content = detectAlert;

                lCalibAlert.Foreground = (calibStatOK) ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Red;
                lCalibAlert.Content = calibAlert;

                lBorderAlert.Foreground = (borderStatOK) ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Red;
                lBorderAlert.Content = borderAlert;
            }));
        }
        private void btnRed_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Czerwony);
        }

        private void btnYellow_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Zolty);
        }

        private void btnGreen_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Zielony);
        }

        private void btnBlue_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Nebieski);
        }

        private void btnViolet_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Fioletowy);
        }

        private void btnOrange_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Pomarancz);
        }

        private void btnBrown_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Brazowy);
        }

        private void btnBlack_Click(object sender, RoutedEventArgs e)
        {
            PisakColorChange(PisakColor.Czarny);
        }

        private void PisakColorChange(PisakColor color)
        {
            switch (color)
            {
                case PisakColor.Czerwony:
                    ColorBrush = Brushes.Red;
                    borderRed.BorderBrush = Brushes.White;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Zolty:
                    ColorBrush = Brushes.Yellow;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.White;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Zielony:
                    ColorBrush = Brushes.Green;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.White;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Nebieski:
                    ColorBrush = Brushes.Blue;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.White;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Fioletowy:
                    ColorBrush = Brushes.Violet;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.White;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Pomarancz:
                    ColorBrush = Brushes.Orange;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.White;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Brazowy:
                    ColorBrush = Brushes.Brown;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.White;
                    borderBlack.BorderBrush = Brushes.Gray;
                    break;
                case PisakColor.Czarny:
                    ColorBrush = Brushes.Black;
                    borderRed.BorderBrush = Brushes.Gray;
                    borderYellow.BorderBrush = Brushes.Gray;
                    borderGreen.BorderBrush = Brushes.Gray;
                    borderBlue.BorderBrush = Brushes.Gray;
                    borderViolet.BorderBrush = Brushes.Gray;
                    borderOrange.BorderBrush = Brushes.Gray;
                    borderBrown.BorderBrush = Brushes.Gray;
                    borderBlack.BorderBrush = Brushes.White;
                    break;
                default:
                    break;
            }
        }
    }
}
