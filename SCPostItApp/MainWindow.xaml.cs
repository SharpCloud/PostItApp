using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
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
using SC.PostItApp.Models;
using SC.PostItApp.Views;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SCItemSyncroniser.Helpers;
using Attribute = SC.API.ComInterop.Models.Attribute;
using Panel = SC.API.ComInterop.Models.Panel;
using Window = System.Windows.Window;
using System.Drawing;
using SC.PostItApp.ViewModels;

namespace SC.PostItApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _userDef = "";
        private static string _passwordDef = "";
        private static string _urlDef = "https://my.sharpcloud.com";
        private static string _ridDef = "";
  
        private string _url;
        private string _user;
        private string _password;
        private string _rid;
        private string _filename;

        private bool _bCancel;

        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = DataContext as MainViewModel;

            LoadSettings();

            mainTab.SelectedIndex = int.Parse(Helpers.ModelHelper.RegRead("selectedtab", "0"));

        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            var proxy = new ProxySettings(_viewModel);
            proxy.ShowDialog();
        }

        private void ClickClearPassword(object sender, RoutedEventArgs e)
        {
            tbPassword.Password = "";
            Helpers.ModelHelper.RegWrite("Password2", "");
        }

        private void SaveAndValidateCLick(object sender, RoutedEventArgs e)
        {
            if (ValidateCreds())
            {
                _viewModel._userName = tbUsername.Text;
                _viewModel._url = tbUrl.Text;
                _viewModel._password = tbPassword.Password;

                _viewModel.SaveAllData();
                MessageBox.Show("Awesome! Your credentials have been validated.");
            }
            else
            {
                MessageBox.Show("Sorry, your credentials were not correct, please try again.\n\nRemember, if you are logging in via O365 you need to use the SharpCloud password, not the O365 one.");
            }
        }

        private bool ValidateCreds()
        {
            return SC.API.ComInterop.SharpCloudApi.UsernamePasswordIsValid(tbUsername.Text, tbPassword.Password,
                tbUrl.Text, _viewModel._proxy, _viewModel._proxyAnnonymous, _viewModel._proxyUserName, _viewModel._proxyPassword);
        }

        private void SaveSettings()
        {
            RegWrite("Url", _url);
            RegWrite("User", _user);
            RegWrite("Password", _password);
            RegWrite("Roadmap", _rid);
            RegWrite("Filename", _filename);
        }

        private void LoadSettings()
        {
            _url = RegRead("Url", _urlDef);
            _user = RegRead("User", _userDef);
            _password = RegRead("Password", _passwordDef);
            _rid = RegRead("Roadmap", _ridDef);
            _filename = RegRead("Filename", _filename);

            tbUrl.Text = _url;
            tbUsername.Text = _user;
            tbPassword.Password = _password;
            tbStoryID.Text = _rid;
            tbFile.Text = _filename;
        }


        private bool ValidateSettings(bool onlyCheckCreds = false)
        {
            _url = tbUrl.Text;
            _user = tbUsername.Text;
            _password = tbPassword.Password;
            _rid = tbStoryID.Text;
            _filename = tbFile.Text;
            SaveSettings();

            if (string.IsNullOrEmpty(_url))
            {
                MessageBox.Show("Please enter a valid url");
                tbUrl.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(_user))
            {
                MessageBox.Show("Please enter a valid username");
                tbUsername.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(_password))
            {
                MessageBox.Show("Please enter a valid password");
                tbPassword.Focus();
                return false;
            }

            if (onlyCheckCreds)
                return true;

            if (string.IsNullOrEmpty(_filename))
            {
                MessageBox.Show("Please enter a filename for your spreadsheet");
                tbFile.Focus();
                return false;
            }

            if (!File.Exists(_filename))
            {
                File.OpenRead(_filename);

                MessageBox.Show("Please enter a valid filename for your spreadsheet");
                tbFile.Focus();
                return false;
            }
            return true;
        }
                
        private static List<string> ReadPostIts(SharpCloudApi sc, string filename, ViewModels.MainViewModel vm)
        {
            var list = new List<string>();
            
            var XL = new Microsoft.Office.Interop.Excel.Application();

            var wb = XL.Workbooks.Open(filename);

            var directoryName = System.IO.Path.GetDirectoryName(filename);

            int counter = 1;

            foreach (Microsoft.Office.Interop.Excel.Chart chart in wb.Charts)
            {
                string file = string.Format("{0}\\chart{1}.jpg", directoryName, counter++);

                vm.Status = "uploading images " + file;

                chart.Export(file, "JPEG", false);

                list.Add(sc.UploadImageFile(file, false));
            }

            wb.Close();
            XL = null;
            
            return list;
        }

        private static bool _wait;

        private async void ClickButtonPostIts(object sender, RoutedEventArgs e)
        {
            if (!ValidateSettings())
                return;

            try
            {
                // We need to use async here becasuse we need to use the clipboard to access the images in Excel
                // and this must be done from the UI thread
                var vm = DataContext as ViewModels.MainViewModel;

                vm.SetFormOn(true);
                vm.Status = "Starting...";
                await Task.Delay(100);

                var sc = new SharpCloudApi(_user, _password, _url);

                vm.Status = "Reading Post-It images from Excel...";
                await Task.Delay(100);

                ReadPostItsFromExcel(sc, _filename, vm);
                while(_wait)
                    await Task.Delay(100);

                if (!_bCancel)
                {
                    vm.Status = "Loading story...";
                    await Task.Delay(100);
                    var rm = sc.LoadStory(_rid);

                    vm.Status = "Adding items...";
                    await Task.Delay(100);

 //                   var atts = rm.Attribute_FindByName("Labels");
 //                   if (atts == null)
 //                       atts = rm.Attribute_Add("Labels", Attribute.AttributeType.List);
                    var attsRow = rm.Attribute_FindByName("ExcelRow");
                    if (attsRow == null)
                        attsRow = rm.Attribute_Add("ExcelRow", Attribute.AttributeType.Numeric);
                    var attsCol = rm.Attribute_FindByName("ExcelCol");
                    if (attsCol == null)
                        attsCol = rm.Attribute_Add("ExcelCol", Attribute.AttributeType.Numeric);

                    foreach (var imageProp in _list)
                    {
                        var item = rm.Item_AddNew(imageProp.itemName);

                        item.ImageId = imageProp.imageId;
                        item.Description = imageProp.itemName;
                        item.HideName = true;
                        item = CreateAndAssignCategoryIfPoss(item, imageProp.categoryName);
                        item.ExternalId = imageProp.Id;

                        item.SetAttributeValue(attsRow, imageProp.row);
                        item.SetAttributeValue(attsCol, imageProp.col);

                        item.Tag_AddNew(imageProp.categoryName);

                        //                        var lbl = atts.Labels_Find(name.categoryName);
                        //                       if (lbl == null)
                        //                           atts.Labels_Add(name.categoryName);
                        //                       item.SetAttributeValue(atts, name.categoryName);

                    }

                    vm.Status = "Saving...";
                    rm.Save();
                }
                vm.SetFormOn(false);

            }
            catch (Exception ex)
            {
                var vm = DataContext as ViewModels.MainViewModel;
                vm.SetFormOn(false);
                MessageBox.Show(ex.Message);

                var trace = new System.Diagnostics.StackTrace(ex);
                MessageBox.Show(trace.ToString());
            }
        }

        private Item CreateAndAssignCategoryIfPoss(Item item, string category)
        {
            if (item != null)
            {
                var cat = item.Story.Category_FindByName(category);
                if (cat == null)
                {
                    cat = item.Story.Category_AddNew(category);
                    item.Category = cat;
                }
                else
                {
                    item.Category = cat;
                }
            }
            return item;
        }

        private Category FindNextFreeCategor(Story story)
        {
            foreach (var category in story.Categories)
            {
                if (!category.InUse)
                    return category;
            }
  
            // non found
            return null;
        }

        private List<imageProperties> _list;
        private async void  ReadPostItsFromExcel(SharpCloudApi sc, string filename, ViewModels.MainViewModel vm )
        {
            try
            {
                _bCancel = false;
                _wait = true;
                _list = new List<imageProperties>();

                var XL = new Microsoft.Office.Interop.Excel.Application();

                Workbook wb = XL.Workbooks.Open(filename);

                var directoryName = System.IO.Path.GetDirectoryName(filename);

                int counter = 1;

                foreach (Worksheet worksheet in wb.Worksheets)
                {
                    foreach (Microsoft.Office.Interop.Excel.Shape shape in worksheet.Shapes)
                    {
                        if (!_bCancel)
                        { 
                            string file = string.Format("{0}\\chart{1}.jpg", directoryName, counter++);
                            vm.Status = "uploading images " + file;

                            int col = shape.TopLeftCell.Column;
                            int row = shape.TopLeftCell.Row;

                            string text = ((Range)worksheet.Cells[row, col + 1]).Text;
                            string textTitle = ((Range)worksheet.Cells[3, col]).Text;
                            string Id = $"R[{row}] : C[{col}]";

                            if (row > 3)
                            {
                                shape.CopyPicture(XlPictureAppearance.xlScreen, XlCopyPictureFormat.xlBitmap);

                                if (Clipboard.ContainsImage())
                                {
                                    try
                                    {
                                        var img = Clipboard.GetImage();
                                        vm.Image = img;
                                        vm.Status = $"{Id}\n{textTitle}\n{text}";
                                        await Task.Delay(100);
                                        WriteJpeg(file, 100, img);
                                    }
                                    catch (Exception exception)
                                    {
                                        MessageBox.Show(exception.Message);
                                    }
                                }
                                var imageName = new imageProperties();
                                imageName.imageId = sc.UploadImageFile(file, false);
                                imageName.itemName = text;
                                imageName.categoryName = textTitle;
                                imageName.Id = Id;
                                imageName.row = row;
                                imageName.col = col;
                                _list.Add(imageName);
                            }
                        }
                    }
                }
                vm.Image = null;
                wb.Close();
                XL = null;
                _wait = false;
            }
            catch (Exception e)
            {
                vm.Status = e.Message;
                await Task.Delay(1000);
            }
        }

        static void WriteJpeg(string fileName, int quality, BitmapSource bmp)
        {

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            BitmapFrame outputFrame = BitmapFrame.Create(bmp);
            encoder.Frames.Add(outputFrame);
            encoder.QualityLevel = quality;

            using (FileStream file = File.OpenWrite(fileName))
            {
                encoder.Save(file);
            }
        }


        public string RegRead(string KeyName, string defVal)
        {
            // Opening the registry key
            RegistryKey rk = Registry.CurrentUser;
            // Open a subKey as read-only
            RegistryKey sk1 = rk.OpenSubKey("SOFTWARE\\SharpCloud\\API\\ExcelCharts");
            // If the RegistrySubKey doesn't exist -> (null)
            if (sk1 == null)
            {
                return defVal;
            }
            else
            {
                try
                {
                    // If the RegistryKey exists I get its value
                    // or null is returned.
                    return (string)sk1.GetValue(KeyName.ToUpper());
                }
                catch (Exception e)
                {
                    // AAAAAAAAAAARGH, an error!
                    //ShowErrorMessage(e, "Reading registry " + KeyName.ToUpper());
                    return defVal;
                }
            }
        }

        public bool RegWrite(string KeyName, object Value)
        {
            try
            {
                // Setting
                RegistryKey rk = Registry.CurrentUser;
                // I have to use CreateSubKey 
                // (create or open it if already exits), 
                // 'cause OpenSubKey open a subKey as read-only
                RegistryKey sk1 = rk.CreateSubKey("SOFTWARE\\SharpCloud\\API\\ExcelCharts");
                // Save the value
                sk1.SetValue(KeyName.ToUpper(), Value);

                return true;
            }
            catch (Exception e)
            {
                // AAAAAAAAAAARGH, an error!
                //ShowErrorMessage(e, "Writing registry " + KeyName.ToUpper());
                return false;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = "*.xls*";
            dlg.Filter = "Excel Files (*.xls*)|*.xls*|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                tbFile.Text = filename;
            }


        }

        private void BrowseStory(object sender, RoutedEventArgs e)
        {
            if (ValidateSettings(true) == false)
            {
                return;
            }
            var sel = new SelectStory(new SharpCloudApi(_user, _password, _url), false, _user);

            sel.Closed += (o, args) =>
            {
                if (sel.DialogResult == true)
                {
                    tbStoryID.Text = sel.SelectedStoryLites[0].Id;
                }
            };
            sel.ShowDialog();
            

        }

        private void tbStoryID_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = tbStoryID.Text;
            if (s.Contains("#/story"))
            {
                var mid = s.Substring(s.IndexOf("#/story") + 8);
                if (mid.Length > 36)
                {
                    mid = mid.Substring(0, 36);
                    tbStoryID.Text = mid;
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Helpers.ModelHelper.RegWrite("selectedtab", mainTab.SelectedIndex.ToString());
        }

        private void Button_ClickCancel(object sender, RoutedEventArgs e)
        {
            _bCancel = true;
        }

        private void Button_ClickVideo(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/watch?v=JGeKKhqTm7A");
        }
    }
}
