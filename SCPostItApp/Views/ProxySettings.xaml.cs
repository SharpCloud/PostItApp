using System.Windows;
using SC.PostItApp.ViewModels;

namespace SC.PostItApp.Views
{
    /// <summary>
    /// Interaction logic for ProxySettings.xaml
    /// </summary>
    public partial class ProxySettings : Window
    {
        private MainViewModel _viewModel;

        public ProxySettings(MainViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            tbProxy.Text = _viewModel._proxy;
            chkAnnonymous.IsChecked = _viewModel._proxyAnnonymous;
            tbUsername.Text = _viewModel._proxyUserName;
            tbPassword.Password = _viewModel._proxyPassword;

            tbPassword.Password = _viewModel._password;

        }

        private void ClickOnOK(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbProxy.Text) && (bool)!chkAnnonymous.IsChecked)
            {
                if (string.IsNullOrEmpty(tbUsername.Text) || string.IsNullOrEmpty(tbPassword.Password))
                {
                    MessageBox.Show(
                        "You must provide a username and password if you are not using an anonymous proxy.",
                        "Proxy Server error");
                    return;
                }
            }

            _viewModel._proxy = tbProxy.Text;
            _viewModel._proxyAnnonymous = (bool)chkAnnonymous.IsChecked;
            _viewModel._proxyUserName = tbUsername.Text;
            _viewModel._proxyPassword = tbPassword.Password;
            _viewModel.SaveAllData();
            Close();
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
