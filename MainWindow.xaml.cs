
using System;
using System.Threading;
using System.Windows;
using YTMDownloaderWPF.Services;
using YTMDownloaderWPF.Models;

namespace YTMDownloaderWPF
{
    public partial class MainWindow : Window
    {
        private readonly YtDlpService _ytDlpService =
            new YtDlpService();
        private readonly DownloadSettings _downloadSettings =
            new DownloadSettings();

        private readonly DownloadManager _downloadManager =
            new DownloadManager();

        private PlaylistInfo _currentPlaylist;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                MaximizeButton.Content = "\uE923";
            }
            else
            {
                WindowState = WindowState.Normal;
                MaximizeButton.Content = "\uE922";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string url = PlaylistUrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                PlaylistUrlTextBox.Focus();
                return;
            }

            AnalyzeButton.IsEnabled = false;
            AnalyzeButton.Content = "Analyzing...";

            PlaylistEmptyState.Visibility = Visibility.Visible;
            PlaylistTracksList.Visibility = Visibility.Collapsed;

            try
            {
                PlaylistInfo playlist =
                    await _ytDlpService.AnalyzePlaylistAsync(
                        url, CancellationToken.None);

                PlaylistPreviewTitle.Text = playlist.Title;
                PlaylistPreviewSubtitle.Text = playlist.Creator;
                TrackCountText.Text = playlist.TrackCount + " TRACKS";

                PlaylistTracksList.ItemsSource = playlist.Tracks;

                PlaylistEmptyState.Visibility = Visibility.Collapsed;
                PlaylistTracksList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Analysis Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                PlaylistEmptyState.Visibility = Visibility.Visible;
                PlaylistTracksList.Visibility = Visibility.Collapsed;
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
                AnalyzeButton.Content = "Analyze Playlist";
            }
        }

    }
}
