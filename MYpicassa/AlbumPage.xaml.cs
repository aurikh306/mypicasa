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

namespace MYpicassa
{
    public partial class AlbumPage : PhoneApplicationPage
    {
        private App app = App.Current as App;

        public AlbumPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // We are coming back from Images Page
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back && app.newPictures == false)
            {
                AlbumImagesListBox.ItemsSource = app.albumImages;
                PageTitle.Text = app.albums[app.selectedAlbumIndex].title;
                AlbumImagesListBox.SelectedIndex = -1;
                return;
            }

            // We are coming from MainPage, start loading album images
            IDictionary<string, string> parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("SelectedIndex"))
            {
                int selectedIndex = Int32.Parse(parameters["SelectedIndex"]);
                PageTitle.Text = app.albums[selectedIndex].title;
                GetImages(selectedIndex);
            }
        }

        private void GetImages(int selectedIndex)
        {
            WebClient webClient = new WebClient();
            string auth = "GoogleLogin auth=" + app.auth;
            webClient.Headers[HttpRequestHeader.Authorization] = auth;
            Uri uri = new Uri(app.albums[selectedIndex].href, UriKind.Absolute);
            //MessageBox.Show(uri.AbsoluteUri);
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(ImagesDownloaded);
            webClient.DownloadStringAsync(uri);
        }
        public void ImagesDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result == null || e.Error != null)
                {
                    MessageBox.Show("Cannot load images from Picasa server!");
                    return;
                }
                else
                {
                    // Deserialize JSON string to dynamic object
                    IDictionary<string, object> json = (IDictionary<string, object>)SimpleJson.DeserializeObject(e.Result);
                    // Feed object
                    IDictionary<string, object> feed = (IDictionary<string, object>)json["feed"];
                    // Number of photos object
                    IDictionary<string, object> numberOfPhotos = (IDictionary<string, object>)feed["gphoto$numphotos"];
                    // Entries List
                    var entries = (IList<Object>)feed["entry"];
                    // clear previous images from albumImages
                    app.albumImages.Clear();
                    // Find image details from entries
                    for (int i = 0; i < entries.Count; i++)
                    {
                        // Create a new albumImage
                        AlbumImage albumImage = new AlbumImage();
                        // Image entry object
                        IDictionary<string, object> entry = (IDictionary<string, object>)entries[i];
                        // Image title object
                        IDictionary<string, object> title = (IDictionary<string, object>)entry["title"];
                        // Get album title
                        albumImage.title = (string)title["$t"];
                        // Album content object
                        IDictionary<string, object> content = (IDictionary<string, object>)entry["content"];
                        // Get image src url
                        albumImage.content = (string)content["src"];
                        // Image width object
                        IDictionary<string, object> width = (IDictionary<string, object>)entry["gphoto$width"];
                        // Get image width
                        albumImage.width = (string)width["$t"];
                        // Image height object
                        IDictionary<string, object> height = (IDictionary<string, object>)entry["gphoto$height"];
                        // Get image height
                        albumImage.height = (string)height["$t"];
                        // Image size object
                        IDictionary<string, object> size = (IDictionary<string, object>)entry["gphoto$size"];
                        // Get image size 
                        albumImage.size = (string)size["$t"];
                        // Image media group List
                        IDictionary<string, object> mediaGroup = (IDictionary<string, object>)entry["media$group"];
                        IList<Object> mediaThumbnailList = (IList<Object>)mediaGroup["media$thumbnail"];
                        // First thumbnail object
                        IDictionary<string, object> mediathumbnail = (IDictionary<string, object>)mediaThumbnailList[0];
                        // Get thumnail url
                        albumImage.thumbnail = (string)mediathumbnail["url"];
                        // Add albumImage to albumImages Collection
                        app.albumImages.Add(albumImage);
                    }
                    // Add albumImages to AlbumImagesListBox
                    AlbumImagesListBox.ItemsSource = app.albumImages;
                    app.newPictures = false;
                }
            }
            catch (WebException)
            {
                MessageBox.Show("Cannot load images from Picasa server!");
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show("Cannot load images from Picasa Server - JSON parsing error happened!");
            }
        }
        private void AlbumImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If real selection is happened, go to a ImagesPage
            if (AlbumImagesListBox.SelectedIndex == -1) return;
            this.NavigationService.Navigate(new Uri("/ImagesPage.xaml?SelectedIndex=" + AlbumImagesListBox.SelectedIndex + "&SelectedAlbum=" + PageTitle.Text, UriKind.Relative));
        }
    }
}