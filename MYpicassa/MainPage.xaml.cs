using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace MYpicassa
{
    public partial class MainPage : PhoneApplicationPage
    {
        private IsolatedStorageSettings appSettings;
        private const string emailKey = "emailKey";
        private const string passwordKey = "passwordKey";
        private const string savepasswordKey = "savepasswordKey";
        private const string usernameKey = "usernameKey";

        private string username = "";
        private string email = "";
        private string password = "";
        private bool savepassword = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            appSettings = IsolatedStorageSettings.ApplicationSettings;


        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            username = UsernameTextBox.Text;
            email = EmailTextBox.Text;
            password = PasswordTextBox.Password;
            savepassword = (bool)SavePasswordCheckBox.IsChecked;
            this.NavigationService.Navigate(new Uri("/AlbumsPage.xaml", UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // load and show saved email from isolated storage
            if (appSettings.Contains(emailKey))
            {
                email = (string)appSettings[emailKey];
            }
            EmailTextBox.Text = email;

            // load password from isolated storage
            if (appSettings.Contains(passwordKey))
            {
                password = (string)appSettings[passwordKey];
            }

            // username from isolated storage
            if (appSettings.Contains(usernameKey))
            {
                username = (string)appSettings[usernameKey];
            }
            UsernameTextBox.Text = username;

            // show password if selected to save
            if (appSettings.Contains(savepasswordKey))
            {
                string savepass = (string)appSettings[savepasswordKey];
                if (savepass == "true")
                {
                    savepassword = true;
                    PasswordTextBox.Password = password;
                }
                else
                {
                    savepassword = false;
                    PasswordTextBox.Password = "";
                }
                SavePasswordCheckBox.IsChecked = savepassword;
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // add email to isolated storage
            appSettings.Remove(emailKey);
            appSettings.Add(emailKey, email);

            // add savepassword and password to isolated storage
            appSettings.Remove(savepasswordKey);
            appSettings.Remove(passwordKey);

            // add username to isolated storage
            appSettings.Remove(usernameKey);
            appSettings.Add(usernameKey, username);

            if (SavePasswordCheckBox.IsChecked == true)
            {
                appSettings.Add(savepasswordKey, "true");
                appSettings.Add(passwordKey, password);
            }
            else
            {
                appSettings.Add(savepasswordKey, "false");
                appSettings.Add(passwordKey, password);
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {

        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            CreateTile();
        }

        private void CreateTile()
        {
            /**
             * http://msdn.microsoft.com/en-us/library/windowsphone/develop/hh202979(v=vs.105).aspx
             */
            // Look to see whether the Tile already exists and if so, don't try to create it again.
            // need to use the reference Microsoft.Phone.Shell and System.Linq
            ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("pinned=MyImage.jpg"));

            // Create the Tile if we didn't find that it already exists.
            if (TileToFind == null)
            {

                // Create the Tile object and set some initial properties for the Tile.
                // A Count value of 0 indicates that the Count should not be displayed.
                StandardTileData NewTileData = new StandardTileData
                {
                    BackgroundImage = new Uri("ApplicationIcon.png", UriKind.Relative),
                    Title = "Tile demo",
                    BackTitle = "E-Acquisition",
                    BackBackgroundImage = new Uri("BackBackIcon.png", UriKind.Relative),
                    Count = 0
                };

                // Create the Tile and pin it to Start. This will cause a navigation to Start and a deactivation of our application.
                ShellTile.Create(new Uri("/MainPage.xaml?pinned=MyImage.jpg", UriKind.Relative), NewTileData);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("--- A tile is already existed.---");
                MessageBox.Show("A tile is already existed.");
            }
        }
        
    }
}