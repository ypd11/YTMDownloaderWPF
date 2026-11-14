
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YTMDownloaderWPF.Models;
using YTMDownloaderWPF.Services;

namespace YTMDownloaderWPF
{
    public partial class MainWindow : Window
    {
        private readonly YtDlpService _ytDlpService =
            new YtDlpService();
        private readonly SettingsService _settingsService =
            new SettingsService();

        private DownloadSettings _downloadSettings;

        private readonly DownloadManager _downloadManager =
            new DownloadManager();

        private PlaylistInfo _currentPlaylist;
        private CancellationTokenSource _downloadCancellation;
        private bool _pauseRequested;
        private bool _downloadPaused;
        private bool _cancelRequested;

        private TaskCompletionSource<bool> _resumeSignal;

        public MainWindow()
        {
            InitializeComponent();

            _downloadSettings = _settingsService.LoadSettings();
            _downloadManager.ProgressChanged += DownloadManager_ProgressChanged;
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

                _currentPlaylist = playlist;
                DownloadButton.IsEnabled = true;

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

        private async void DownloadButton_Click(
    object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            _cancelRequested = false;
            _pauseRequested = false;
            _downloadPaused = false;

            DownloadButton.IsEnabled = false;
            DownloadButton.Content = "Downloading...";

            PauseDownloadButton.Visibility = Visibility.Visible;
            PauseDownloadButton.IsEnabled = true;
            PauseDownloadButton.Content = "Pause";

            CancelDownloadButton.Visibility = Visibility.Visible;
            CancelDownloadButton.IsEnabled = true;

            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;

            DownloadStatusText.Text = "Starting download...";

            try
            {
                while (true)
                {
                    if (_cancelRequested)
                        throw new OperationCanceledException();

                    _downloadCancellation = new CancellationTokenSource();

                    try
                    {
                        await _downloadManager.DownloadPlaylistAsync(
                            _currentPlaylist,
                            _downloadSettings,
                            _downloadCancellation.Token);

                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        if (_cancelRequested)
                            throw;

                        if (!_pauseRequested)
                            throw;

                        _pauseRequested = false;
                        _downloadPaused = true;

                        _resumeSignal = new TaskCompletionSource<bool>(
                            TaskCreationOptions.RunContinuationsAsynchronously);

                        PauseDownloadButton.Content = "Resume";
                        PauseDownloadButton.IsEnabled = true;

                        DownloadStatusText.Text = "Download paused";

                        await _resumeSignal.Task;

                        if (_cancelRequested)
                            throw new OperationCanceledException();

                        _downloadPaused = false;
                        _resumeSignal = null;

                        PauseDownloadButton.Content = "Pause";
                        PauseDownloadButton.IsEnabled = true;

                        DownloadStatusText.Text = "Resuming download...";
                    }
                    finally
                    {
                        if (_downloadCancellation != null)
                        {
                            _downloadCancellation.Dispose();
                            _downloadCancellation = null;
                        }
                    }
                }

                DownloadStatusText.Text = "Download complete";

                MessageBox.Show(
                    "Playlist downloaded successfully!",
                    "Download Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                DownloadStatusText.Text = "Download cancelled";

                MessageBox.Show(
                    "The download was cancelled.",
                    "Download Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DownloadStatusText.Text = "Download failed";

                MessageBox.Show(
                    ex.Message,
                    "Download Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_downloadCancellation != null)
                {
                    _downloadCancellation.Dispose();
                    _downloadCancellation = null;
                }

                _resumeSignal = null;
                _pauseRequested = false;
                _downloadPaused = false;
                _cancelRequested = false;

                DownloadButton.IsEnabled = true;
                DownloadButton.Content = "Download Playlist";

                PauseDownloadButton.Visibility = Visibility.Collapsed;
                PauseDownloadButton.IsEnabled = false;

                CancelDownloadButton.Visibility = Visibility.Collapsed;
                CancelDownloadButton.IsEnabled = false;
            }
        }

        private void DownloadManager_ProgressChanged(DownloadProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadProgressBar.Visibility = Visibility.Visible;

                DownloadProgressBar.Value = progress.ProgressPercentage;

                DownloadStatusText.Text =
                    progress.Status + " · " +
                    progress.ProgressPercentage.ToString("0.0") + "%";

                if (!string.IsNullOrWhiteSpace(progress.FileName))
                {
                    DownloadStatusText.Text =
                        progress.FileName + " · " +
                        progress.ProgressPercentage.ToString("0.0") + "%";
                }
            });
        }

        private void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelRequested = true;
            _pauseRequested = false;

            CancelDownloadButton.IsEnabled = false;
            PauseDownloadButton.IsEnabled = false;

            DownloadStatusText.Text = "Cancelling...";

            if (_downloadPaused)
            {
                // Wake the paused download so it can exit.
                if (_resumeSignal != null)
                    _resumeSignal.TrySetResult(true);
            }
            else if (_downloadCancellation != null)
            {
                _downloadCancellation.Cancel();
            }
        }

        private void PauseDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadPaused)
            {
                // Resume
                if (_resumeSignal != null)
                {
                    _resumeSignal.TrySetResult(true);
                    PauseDownloadButton.IsEnabled = false;
                }

                return;
            }

            if (_downloadCancellation != null &&
                !_downloadCancellation.IsCancellationRequested)
            {
                // Pause by stopping yt-dlp.
                _pauseRequested = true;
                PauseDownloadButton.IsEnabled = false;

                DownloadStatusText.Text = "Pausing...";

                _downloadCancellation.Cancel();
            }
        }

        private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow =
                new SettingsWindow(_downloadSettings);

            settingsWindow.Owner = this;

            if (settingsWindow.ShowDialog() == true)
            {
                _settingsService.SaveSettings(_downloadSettings);

                MessageBox.Show(
                    "Settings saved successfully!",
                    "Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
