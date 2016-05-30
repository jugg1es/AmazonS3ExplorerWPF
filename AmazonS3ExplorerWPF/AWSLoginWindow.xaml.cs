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
using System.Windows.Shapes;
using System.Collections.ObjectModel;


namespace AmazonS3ExplorerWPF
{
    /// <summary>
    /// Interaction logic for AWSLoginWindow.xaml
    /// </summary>
    public partial class AWSLoginWindow : Window
    {
        public List<string> AmazonProfiles
        {
            get { return (List<string>)this.GetValue(AmazonProfilesProperty); }
            set { this.SetValue(AmazonProfilesProperty, value); }
        }
        public static readonly DependencyProperty AmazonProfilesProperty = DependencyProperty.Register(
            "AmazonProfiles", typeof(List<string>), typeof(AWSLoginWindow),
            new PropertyMetadata(new List<string>()));



        public AWSLoginWindow()
        {
            InitializeComponent();
            
            this.IsVisibleChanged += (s, e) =>
            {
                LoadAmazonProfiles();

            };
        }
        private void ManageProfiles_Click(object sender, RoutedEventArgs e)
        {

        }
        private void LoadAmazonProfiles()
        {
            this.AmazonProfiles = Amazon.Util.ProfileManager.ListProfileNames().ToList();
            if (string.IsNullOrEmpty(Properties.Settings.Default.LastProfileUsed) == false)
            {
                cbProfiles.SelectedItem = Properties.Settings.Default.LastProfileUsed;
            }
        }

        private void SelectProfile_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LastProfileUsed = cbProfiles.SelectedItem.ToString();
            Properties.Settings.Default.Save();
            this.DialogResult = true;
            this.Close();
        }

        private void CreateAWSProfile_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(txtProfileName.Text) ||
                string.IsNullOrEmpty(txtAccessKey.Password) ||
                    string.IsNullOrEmpty(txtSecret.Password))
            {
                MessageBox.Show(Window.GetWindow(this), "All fields are required", "Error");
                return;
            }

            Amazon.Util.ProfileManager.RegisterProfile(txtProfileName.Text, txtAccessKey.Password, txtSecret.Password);

            MessageBox.Show(Window.GetWindow(this), "Profile Created", "Success");

            txtAccessKey.Password = string.Empty;
            txtProfileName.Text = string.Empty;
            txtSecret.Password = string.Empty;

            Properties.Settings.Default.LastProfileUsed = txtProfileName.Text;
            Properties.Settings.Default.Save();
            LoadAmazonProfiles();

        }

      
    }
}
