using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AmazonS3ExplorerWPF.Controls.Adorner
{
    /// <summary>
    /// Interaction logic for LoadingWait.xaml
    /// </summary>
    public delegate void CancelOperationEventHandler();
    public partial class LoadingWait : UserControl
    {
        public event CancelOperationEventHandler CancelOperation;

        #region Data
        private readonly DispatcherTimer animationTimer;

        public bool AdornerShowsSubProgress
        {
            get
            {
                return (bool)GetValue(AdornerShowsSubProgressProperty);
            }
            set
            {
                SetValue(AdornerShowsSubProgressProperty, value);
            }
        }
        public int AdornerSubProgress
        {
            get
            {
                return (int)GetValue(AdornerSubProgressProperty);
            }
            set
            {
                SetValue(AdornerSubProgressProperty, value);
            }
        }

        public bool AdornerShowsProgress
        {
            get
            {
                return (bool)GetValue(AdornerShowsProgressProperty);
            }
            set
            {
                SetValue(AdornerShowsProgressProperty, value);
            }
        }


        public int AdornerProgress
        {
            get
            {
                return (int)GetValue(AdornerProgressProperty);
            }
            set
            {
                SetValue(AdornerProgressProperty, value);
            }
        }
        public bool AdornerShowsCancel
        {
            get
            {
                return (bool)GetValue(AdornerShowsCancelProperty);
            }
            set
            {
                SetValue(AdornerShowsCancelProperty, value);
            }
        }
        public static readonly DependencyProperty AdornerShowsCancelProperty =
         DependencyProperty.Register("AdornerShowsCancel", typeof(bool), typeof(LoadingWait),
             new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AdornerShowsSubProgressProperty =
          DependencyProperty.Register("AdornerShowsSubProgress", typeof(bool), typeof(LoadingWait),
              new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AdornerShowsProgressProperty =
          DependencyProperty.Register("AdornerShowsProgress", typeof(bool), typeof(LoadingWait),
              new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AdornerProgressProperty =
           DependencyProperty.Register("AdornerProgress", typeof(int), typeof(LoadingWait),
               new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty AdornerSubProgressProperty =
         DependencyProperty.Register("AdornerSubProgress", typeof(int), typeof(LoadingWait),
             new FrameworkPropertyMetadata(0));


        #endregion

        #region Constructor
        public LoadingWait()
        {
            InitializeComponent();

           /* prog.Visibility = System.Windows.Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Collapsed;
            progSubProg.Visibility = Visibility.Collapsed;
            if (this.AdornerShowsProgress)
                prog.Visibility = System.Windows.Visibility.Visible;
            if (this.AdornerShowsSubProgress)
                progSubProg.Visibility = System.Windows.Visibility.Visible;
            if (this.AdornerShowsCancel)
                btnCancel.Visibility = Visibility.Visible;*/

            this.IsVisibleChanged += LoadingWait_IsVisibleChanged;
            
            animationTimer = new DispatcherTimer(
                DispatcherPriority.ContextIdle, Dispatcher);
            animationTimer.Interval = new TimeSpan(0, 0, 0, 0, 75);
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if(CancelOperation != null)
            {
                CancelOperation();
            }
        }
        void LoadingWait_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.AdornerProgress = 0;
                this.AdornerSubProgress = 0;
                /*
                if (this.AdornerShowsProgress)
                    prog.Visibility = System.Windows.Visibility.Visible;
                if (this.AdornerShowsSubProgress)
                    progSubProg.Visibility = System.Windows.Visibility.Visible;*/

            }
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            //Mouse.OverrideCursor = Cursors.Wait;
            animationTimer.Tick += HandleAnimationTick;
            animationTimer.Start();
        }

        private void Stop()
        {
            animationTimer.Stop();
          //  Mouse.OverrideCursor = Cursors.Arrow;
            animationTimer.Tick -= HandleAnimationTick;
        }

        private void HandleAnimationTick(object sender, EventArgs e)
        {
            SpinnerRotate.Angle = (SpinnerRotate.Angle + 36) % 360;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            const double offset = Math.PI;
            const double step = Math.PI * 2 / 10.0;

            SetPosition(C0, offset, 0.0, step);
            SetPosition(C1, offset, 1.0, step);
            SetPosition(C2, offset, 2.0, step);
            SetPosition(C3, offset, 3.0, step);
            SetPosition(C4, offset, 4.0, step);
            SetPosition(C5, offset, 5.0, step);
            SetPosition(C6, offset, 6.0, step);
            SetPosition(C7, offset, 7.0, step);
            SetPosition(C8, offset, 8.0, step);
        }

        private void SetPosition(Ellipse ellipse, double offset,
            double posOffSet, double step)
        {
            ellipse.SetValue(Canvas.LeftProperty, 50.0
                + Math.Sin(offset + posOffSet * step) * 50.0);

            ellipse.SetValue(Canvas.TopProperty, 50
                + Math.Cos(offset + posOffSet * step) * 50.0);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void HandleVisibleChanged(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;

            if (isVisible)
                Start();
            else
                Stop();
        }
        #endregion

      
    }
}
