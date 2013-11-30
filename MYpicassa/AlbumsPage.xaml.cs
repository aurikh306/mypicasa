﻿using System;
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

namespace MYpicassa
{
    public partial class AlbumsPage : PhoneApplicationPage
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
        private string dataFeed  = "";
        private App app = App.Current as App;

        public AlbumsPage()
        {
            InitializeComponent();
            appSettings = IsolatedStorageSettings.ApplicationSettings;
        }

        private void AlbumsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If real selection is happened, go to a AlbumPage
            if (AlbumsListBox.SelectedIndex == -1) return;
            app.selectedAlbumIndex = AlbumsListBox.SelectedIndex;
            this.NavigationService.Navigate(new Uri("/AlbumPage.xaml?SelectedIndex=" + AlbumsListBox.SelectedIndex, UriKind.Relative));
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // load saved username from isolated storage
            if (appSettings.Contains(usernameKey))
            {
                string differentUsername = (string)appSettings[usernameKey];
                if (differentUsername != "") username = differentUsername;
            }

            // load saved email from isolated storage
            if (appSettings.Contains(emailKey))
            {
                email = (string)appSettings[emailKey]; // for example firstname.lastname@gmail.com
                if (username == "" && email.IndexOf("@") != -1) username = email.Substring(0, email.IndexOf("@")); // firstname.lastname
                dataFeed = String.Format("http://picasaweb.google.com/data/feed/api/user/{0}?alt=json", username);
            }

            // load password from isolated storage
            if (appSettings.Contains(passwordKey))
            {
                password = (string)appSettings[passwordKey];
            }

            // we are coming back from AlbumPage
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                AlbumsListBox.ItemsSource = app.albums;
                AlbumsListBox.SelectedIndex = -1;
            }
            else
            {
                // get authentication from Google
                GetAuth();
            }
        }
        private void GetAuth()
        {
            string service = "lh2"; // Picasa
            string accountType = "GOOGLE";

            WebClient webClient = new WebClient();
            Uri uri = new Uri(string.Format("https://www.google.com/accounts/ClientLogin?Email={0}&Passwd={1}&service={2}&accountType={3}", email, password, service, accountType));
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AuthDownloaded);
            webClient.DownloadStringAsync(uri);
        }
        private void AuthDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result != null && e.Error == null)
                {
                    app.auth = "";
                    int index = e.Result.IndexOf("Auth=");
                    if (index != -1)
                    {
                        app.auth = e.Result.Substring(index + 5);
                    }
                    if (app.auth != "")
                    {
                        // get albums from Google
                        GetAlbums();
                        return;
                    }
                }
                MessageBox.Show("Cannot get authentication from google. Check your login and password.");
            }
            catch (WebException)
            {
                MessageBox.Show("Cannot get authentication from google. Check your login and password.");
            }
        }
        private void GetAlbums()
        {
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.Authorization] = "GoogleLogin auth=" + app.auth;
            Uri uri = new Uri(dataFeed, UriKind.Absolute);
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AlbumsDownloaded);
            webClient.DownloadStringAsync(uri);
        }
        public void AlbumsDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result == null || e.Error != null)
                {
                    MessageBox.Show("Cannot get albums data from Picasa server!");
                    return;
                }
                else
                {
                    // Deserialize JSON string to dynamic object
                    IDictionary<string, object> json = (IDictionary<string, object>)SimpleJson.DeserializeObject(e.Result);
                    // Feed object
                    IDictionary<string, object> feed = (IDictionary<string, object>)json["feed"];
                    // Author List
                    IList<Object> author = (IList<Object>)feed["author"];
                    // First author (and only)
                    IDictionary<string, object> firstAuthor = (IDictionary<string, object>)author[0];
                    // Album entries
                    IList<Object> entries = (IList<Object>)feed["entry"];
                    // Delete previous albums from albums list
                    app.albums.Clear();
                    // Find album details
                    for (int i = 0; i < entries.Count; i++)
                    {
                        // Create a new Album
                        Album album = new Album();
                        // Album entry object
                        IDictionary<string, object> entry = (IDictionary<string, object>)entries[i];
                        // Published object
                        IDictionary<string, object> published = (IDictionary<string, object>)entry["published"];
                        // Get published date
                        album.published = (string)published["$t"];
                        // Title object
                        IDictionary<string, object> title = (IDictionary<string, object>)entry["title"];
                        // Album title
                        album.title = (string)title["$t"];
                        // Link List
                        IList<Object> link = (IList<Object>)entry["link"];
                        // First link is album data link object
                        IDictionary<string, object> href = (IDictionary<string, object>)link[0];
                        // Get album data addres
                        album.href = (string)href["href"];
                        // Media group object
                        IDictionary<string, object> mediagroup = (IDictionary<string, object>)entry["media$group"];
                        // Media thumbnail object list
                        IList<Object> mediathumbnailList = (IList<Object>)mediagroup["media$thumbnail"];
                        // First thumbnail object (smallest)
                        var mediathumbnail = (IDictionary<string, object>)mediathumbnailList[0];
                        // Get thumbnail url
                        album.thumbnail = (string)mediathumbnail["url"];
                        // Add album to albums
                        app.albums.Add(album);
                    }
                    // Add albums to AlbumListBox
                    AlbumsListBox.ItemsSource = app.albums;
                }
            }
            catch (WebException)
            {
                MessageBox.Show("Cannot get albums data from Picasa server!");
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show("Cannot load images from Picasa Server - JSON parsing error happened!");
            }
        }
    }
}