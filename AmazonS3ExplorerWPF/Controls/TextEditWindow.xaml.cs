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

namespace AmazonS3ExplorerWPF.Controls
{
    /// <summary>
    /// Interaction logic for TextEditWindow.xaml
    /// </summary>
    public partial class TextEditWindow : Window
    {
        private string _textValue;

        public string TextValue
        {
            get { return _textValue; }
            set { _textValue = value; }
        }
        public TextEditWindow(string fieldLabel, string fieldValue)
        {
            InitializeComponent();

            lblField.Content = fieldLabel;
            txtValue.Text = fieldValue;
            this.Title = fieldLabel;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _textValue = txtValue.Text;
            this.DialogResult = true;
        }

        private void txtValue_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter || e.Key == Key.Return)
            {
                _textValue = txtValue.Text;
                this.DialogResult = true;
            }
        }
    }
}
