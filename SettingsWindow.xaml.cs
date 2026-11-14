
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using YTMDownloaderWPF.Models;

namespace YTMDownloaderWPF
{
    public partial class SettingsWindow : Window
    {
        private readonly DownloadSettings _settings;
        private readonly DownloadSettings _originalSettings;

        public SettingsWindow(DownloadSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            _originalSettings = new DownloadSettings
            {
                OutputFolder = settings.OutputFolder,
                AudioFormat = settings.AudioFormat,
                AudioQuality = settings.AudioQuality,
                EmbedThumbnail = settings.EmbedThumbnail,
                AddMetadata = settings.AddMetadata,
                ContinueDownloads = settings.ContinueDownloads
            };

            OutputFolderTextBox.Text = settings.OutputFolder;

            // Load audio format
            foreach (ComboBoxItem item in AudioFormatComboBox.Items)
            {
                if (item.Content.ToString().Equals(
                    settings.AudioFormat,
                    StringComparison.OrdinalIgnoreCase))
                {
                    AudioFormatComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load audio quality
            foreach (ComboBoxItem item in AudioQualityComboBox.Items)
            {
                if (item.Content.ToString().Equals(
                    settings.AudioQuality,
                    StringComparison.OrdinalIgnoreCase))
                {
                    AudioQualityComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load metadata options
            EmbedThumbnailCheckBox.IsChecked = settings.EmbedThumbnail;
            AddMetadataCheckBox.IsChecked = settings.AddMetadata;
        }

        private void BrowseFolderButton_Click(
            object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog =
                new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Choose your music download folder";
                dialog.SelectedPath = OutputFolderTextBox.Text;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    OutputFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SaveSettingsButton_Click(
            object sender, RoutedEventArgs e)
        {
            string folder = OutputFolderTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show(
                    "Please select a download folder.",
                    "Invalid Folder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Directory.CreateDirectory(folder);

                _settings.OutputFolder = folder;

                // Save audio format
                if (AudioFormatComboBox.SelectedItem is ComboBoxItem formatItem)
                {
                    _settings.AudioFormat = formatItem.Content.ToString().ToLowerInvariant();
                }

                // Save audio quality
                if (AudioQualityComboBox.SelectedItem is ComboBoxItem qualityItem)
                {
                    _settings.AudioQuality = qualityItem.Content.ToString();
                }

                // Save metadata options
                _settings.EmbedThumbnail =
                    EmbedThumbnailCheckBox.IsChecked == true;

                _settings.AddMetadata =
                    AddMetadataCheckBox.IsChecked == true;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Folder Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelSettingsButton_Click(
            object sender, RoutedEventArgs e)
        {
            _settings.OutputFolder = _originalSettings.OutputFolder;

            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(
            object sender, RoutedEventArgs e)
        {
            CancelSettingsButton_Click(sender, e);
        }

        private void Window_MouseLeftButtonDown(
    object sender,
    System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }
    }
}
