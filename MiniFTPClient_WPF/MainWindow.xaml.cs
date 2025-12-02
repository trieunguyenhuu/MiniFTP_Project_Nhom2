using MiniFTPClient_WPF.home;
using MiniFTPClient_WPF.tinnhan;
using MiniFTPClient_WPF.thungrac;
using MiniFTPClient_WPF.setting;
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

namespace MiniFTPClient_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new MiniFTPClient_WPF.home.Page1());
        }

        private void trangchu_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Page1());
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void tinnhan_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Tinnhan());
        }

        private void thungrac_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Thungrac());
        }

        private void setting_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Setting());
        }
    }
}