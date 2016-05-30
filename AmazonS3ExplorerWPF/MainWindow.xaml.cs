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

namespace AmazonS3ExplorerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool Authenticated
        {
            get { return (bool)this.GetValue(AuthenticatedProperty); }
            set { this.SetValue(AuthenticatedProperty, value); }
        }
        public static readonly DependencyProperty AuthenticatedProperty = DependencyProperty.Register(
            "Authenticated", typeof(bool), typeof(MainWindow),
            new PropertyMetadata(false));

        public List<string> Buckets
        {
            get { return (List<string>)this.GetValue(BucketsProperty); }
            set { this.SetValue(BucketsProperty, value); }
        }
        public static readonly DependencyProperty BucketsProperty = DependencyProperty.Register(
            "Buckets", typeof(List<string>), typeof(MainWindow),
            new PropertyMetadata(new List<string>()));


        private AmazonS3Service _amazon;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                CheckForAmazonProfile();
            };
        }

        #region Authorization
        private void CheckForAmazonProfile()
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.LastProfileUsed) ||
                    Properties.Settings.Default.AutoUseProfile == false)
            {
                ShowLoginWindow();
            }
            else
            {
                InitializeUsingProfile(Properties.Settings.Default.LastProfileUsed);
            }
        }
        private void InitializeUsingProfile(string profile)
        {
            Amazon.Runtime.AWSCredentials credentials = null;
            if (Amazon.Util.ProfileManager.TryGetAWSCredentials(profile, out credentials))
            {
               
                try
                {
                    _amazon = new AmazonS3Service(credentials);
                    this.Buckets = _amazon.GetBuckets();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(Window.GetWindow(this), "Error occured while retrieving bucket list " + ex.Message, "Error");
                    ShowLoginWindow();
                }
                finally
                {
                    this.Authenticated = true;
                    

                }                              
            }
            else
            {
                MessageBox.Show(Window.GetWindow(this), "Failed to authenticate the selected profile: " +
                   profile, "Error");
                ShowLoginWindow();
            }
        }

        private void ShowLoginWindow()
        {
            AWSLoginWindow winLogin = new AWSLoginWindow();
            winLogin.Owner = Window.GetWindow(this);
            if (winLogin.ShowDialog() == true)
            {
                InitializeUsingProfile(Properties.Settings.Default.LastProfileUsed);
            }
        }
        private void ChangeProfile_Click(object sender, RoutedEventArgs e)
        {
            ShowLoginWindow();
        }



        #endregion

        private void LoadBucket_Click(object sender, RoutedEventArgs e)
        {
            if(cbBuckets.SelectedItem != null)
            {
                ctrlFileManager.LoadBucketContents(_amazon, cbBuckets.SelectedItem.ToString());
            }
          
        }
    }
}
