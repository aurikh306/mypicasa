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
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;

namespace MYpicassa
{
    public partial class ImagesPage : PhoneApplicationPage
    {
        App app = App.Current as App;
        GestureListener gestureListener;
        private BitmapImage bitmapImage;
        private String selectedAlbumTitle;
        private Boolean ShowProgress = false;
        private BitmapImage capturedImage;
        private CameraCaptureTask ccTask = new CameraCaptureTask();
        private WriteableBitmap[] imgList = new WriteableBitmap[10];
        private Random rnd = new Random();

        public ImagesPage()
        {
            InitializeComponent();

            // Initialize GestureListener
            gestureListener = GestureService.GetGestureListener(ContentPanel);
            // Handle Dragging (to show next or previous image from Album)
            gestureListener.DragCompleted += new EventHandler<DragCompletedGestureEventArgs>(gestureListener_DragCompleted);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Find selected image index from parameters
            IDictionary<string, string> parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("SelectedIndex"))
            {
                app.selectedImageIndex = Int32.Parse(parameters["SelectedIndex"]);
            }
            else
            {
                app.selectedImageIndex = 0;
            }
            // Find selected album name
            if (parameters.ContainsKey("SelectedAlbum"))
            {
                selectedAlbumTitle = parameters["SelectedAlbum"];
            }
            else
            {
                selectedAlbumTitle = "No album name";
            }

            // Load image from Google
            LoadImage();
        }

        // Gesture - Drag is complete
        void gestureListener_DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            // Left or Right
            if (e.HorizontalChange > 0)
            {
                // previous image (or last if first is shown)
                app.selectedImageIndex--;
                if (app.selectedImageIndex < 0) app.selectedImageIndex = app.albumImages.Count - 1;
            }
            else
            {
                // next image (or first if last is shown)
                app.selectedImageIndex++;
                if (app.selectedImageIndex > (app.albumImages.Count - 1)) app.selectedImageIndex = 0;
            }
            // Load image from Google
            LoadImage();
        }

        // Load Image from Google
        private void LoadImage()
        {
            // Load a new image
            bitmapImage = new BitmapImage(new Uri(app.albumImages[app.selectedImageIndex].content, UriKind.RelativeOrAbsolute));
            // Handle loading (hide Loading... animation)
            bitmapImage.DownloadProgress += new EventHandler<DownloadProgressEventArgs>(bitmapImage_DownloadProgress);
            // Loaded Image is image source in XAML
            image.Source = bitmapImage;
        }

        // Image is loaded from Google
        void bitmapImage_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            // Hide loading... animation
            ShowProgress = false;
            // Disable LoadingListener for this image
            bitmapImage.DownloadProgress -= new EventHandler<DownloadProgressEventArgs>(bitmapImage_DownloadProgress);
            // Show image details in UI
            ImageInfoTextBlock.Text = String.Format("Album {0} : Image {1} of {2}.",
                selectedAlbumTitle,
                (app.selectedImageIndex + 1),
                app.albumImages.Count);
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            ccTask.Show();
            ccTask.Completed += ccTask_Completed;
        }

        public void addXML(object sender, UploadStringCompletedEventArgs e)
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
                    String result = e.Result;
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

        public void uploadImage(object sender,OpenWriteCompletedEventArgs e)
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
                    Stream stream = e.Result;
                    int length = 1024;
                    byte[] buffer = new byte[length];
                    int bytesRead;

                    MemoryStream ms = new System.IO.MemoryStream();
                    WriteableBitmap btmMap = new WriteableBitmap(capturedImage);

                        // write an image into the stream
                        Extensions.SaveJpeg(btmMap, ms, bitmapImage.PixelWidth, bitmapImage.PixelHeight, 0, 100);

                        // reset the stream pointer to the beginning
                        ms.Seek(0, 0);
                    

                    do
                    {
                        bytesRead = ms.Read(buffer, 0, length);
                        stream.Write(buffer, 0, bytesRead);
                    }
                    while (bytesRead != 0);

                    ms.Close();
                    stream.Close();
                    app.newPictures = true;
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

        void ccTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(1600, 1200);
                writeableBitmap.LoadJpeg(e.ChosenPhoto);

                string imageFolder = "Unfaelle";
                string datetime = DateTime.Now.ToString().Replace("/", "");
                datetime = datetime.Replace(":", "");
                string imageFileName = "Foto_" + datetime + ".jpg";
                using (var isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (!isoFile.DirectoryExists(imageFolder))
                    {
                        isoFile.CreateDirectory(imageFolder);
                    }

                    string filePath = System.IO.Path.Combine(imageFolder, imageFileName);
                    using (var stream = isoFile.CreateFile(filePath))
                    {
                        writeableBitmap.SaveJpeg(stream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, 100);
                    }
                }

                //now read the image back from storage to show it worked...
                BitmapImage imageFromStorage = new BitmapImage();

                using (var isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string filePath = System.IO.Path.Combine(imageFolder, imageFileName);
                    using (var imageStream = isoFile.OpenFile(
                        filePath, FileMode.Open, FileAccess.Read))
                    {
                        imageFromStorage.SetSource(imageStream);
                    }
                }

                Rectangle b = new Rectangle()
                {
                    Width = 100,
                    Height = 100,
                };

                Thickness margin = b.Margin;
                margin.Left = 10;
                margin.Top = 10;
                b.Margin = margin;

                capturedImage = imageFromStorage;

                WebClient webClient = new WebClient();
                string auth = "GoogleLogin auth=" + app.auth;
                webClient.Headers[HttpRequestHeader.Authorization] = auth;
                //Uri uri = new Uri(app.albumImages[app.selectedImageIndex].content, UriKind.Absolute);
                Uri uri2 = new Uri("https://picasaweb.google.com/data/feed/api/user/104650757579342390182/albumid/1000000442390182");

                webClient.Headers["Content-Type"] = "image/jpeg";
                webClient.Headers["Slug"] = "name-of-picture.jpg";

                webClient.OpenWriteCompleted += new OpenWriteCompletedEventHandler(uploadImage);
                webClient.OpenWriteAsync(uri2);
             }
        }
    }
}