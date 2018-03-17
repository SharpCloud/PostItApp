using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.PostItApp.Helpers;
using System.Windows.Media.Imaging;

namespace SC.PostItApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        public string _url { get; set; }
        public string _userName { get; set; }
        public string _password { get; set; }
        public string _proxy { get; set; }
        public bool _proxyAnnonymous { get; set; }
        public string _proxyUserName { get; set; }
        public string _proxyPassword { get; set; }

        public MainViewModel()
        {
            _proxyPassword = "";
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }
        private string _status;

        public void SetFormOn(bool show)
        {
            ShowStatus = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public Visibility ShowStatus
        {
            get { return _showStatus; }
            set
            {
                _showStatus = value;
                OnPropertyChanged("ShowStatus");
            }
        }
        private Visibility _showStatus = Visibility.Collapsed;

        public Visibility ShowImage
        {
            get { return _showImage; }
            set
            {
                _showImage = value;
                OnPropertyChanged("ShowImage");
            }
        }
        private Visibility _showImage = Visibility.Collapsed;

        public BitmapSource Image
        {
            get { return _image; }
            set
            {
                _image = value;
                ShowImage = (value == null)? Visibility.Collapsed : ShowImage = Visibility.Visible;

                OnPropertyChanged("Image");
            }
        }
        private BitmapSource _image;
        public bool ShowWaitForm
        {
            get { return _showWaitForm; }
            set
            {
                _showWaitForm = value;
                OnPropertyChanged("ShowWaitForm");
            }
        }
        private bool _showWaitForm;

        public async void ShowWaitFormNow(string message)
        {
            ShowWaitForm = true;
            Status = message;
            await Task.Delay(10);
        }
        public bool RememberPassword 
        { get { return _rememberPassword; }
            set
            {
                _rememberPassword = value;
                OnPropertyChanged("RememberPassword");
            } }
        private bool _rememberPassword;

        public string Logs
        {
            get { return _logs; }
            set
            {
                _logs = value;
                OnPropertyChanged("Logs");
            }
        }
        private string _logs = string.Empty;


        public event PropertyChangedEventHandler PropertyChanged;
        private Dispatcher currentDispatcher;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                //check if we are on the UI thread if not switch
                if (Dispatcher.CurrentDispatcher.CheckAccess())
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                else
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action<string>(this.OnPropertyChanged), propertyName);
            }
        }

        public void SaveUserData()
        {
            ModelHelper.RegWrite("Url", _url);
            ModelHelper.RegWrite("UserName", _userName);
            ModelHelper.RegWrite("RememberPassword", RememberPassword.ToString());
            if (RememberPassword)
                ModelHelper.RegWrite("Password2", Convert.ToBase64String( Encoding.Default.GetBytes(_password)));
            ModelHelper.RegWrite("Proxy", _proxy);
            ModelHelper.RegWrite("ProxyAnnonymous", _proxyAnnonymous.ToString());
            ModelHelper.RegWrite("ProxyUserName", _proxyUserName);
            ModelHelper.RegWrite("ProxyPassword", Convert.ToBase64String(Encoding.Default.GetBytes(_proxyPassword)));
        }

        public void SaveAllData()
        {
            SaveUserData();
        }

        public void LoadData()
        {
            _proxyPassword = Encoding.Default.GetString(Convert.FromBase64String(ModelHelper.RegRead("ProxyPassword", "")));
            _proxyUserName = ModelHelper.RegRead("ProxyUserName", "");
            _proxyAnnonymous = Boolean.Parse(ModelHelper.RegRead("ProxyAnnonymous", true.ToString()));
            _proxy = ModelHelper.RegRead("Proxy", "");
            _url = ModelHelper.RegRead("Url", "https://my.sharpcloud.com");
            _userName = ModelHelper.RegRead("UserName", "");
            RememberPassword = ModelHelper.RegRead("RememberPassword", true.ToString()) == true.ToString();

            if (RememberPassword)
            {
                _password = Encoding.Default.GetString(Convert.FromBase64String(ModelHelper.RegRead("Password2", "")));
            }

            SaveAllData();
        }

        private SharpCloudApi GetApi()
        {
            return new SharpCloudApi(_userName, _password, _url, _proxy);
        }
    }
}
