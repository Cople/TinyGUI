using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TinifyAPI;
using TinyGUI.ViewModels;

namespace TinyGUI.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainModel _mainModel;

        public MainWindow()
        {
            InitializeComponent();
            _mainModel = (MainModel) DataContext;
            MigrateSaveModeSetting();
            Loaded += MainWindow_OnLoaded;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            TinyGUI.Properties.Settings.Default.Save();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            await RefreshCompressionCount();
        }

        private async void KeyTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            await RefreshCompressionCount();
        }

        private async void DropButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!EnsureApiKeyConfigured())
            {
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = "(*.jpg,*.png,*.jpeg,*.webp,*.avif)|*.jpg;*.png;*.jpeg;*.webp;*.avif;",
                CheckFileExists = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string[] imgPaths = openFileDialog.FileNames;
                await Start(imgPaths.ToList());
            }
        }

        private async void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            if (!EnsureApiKeyConfigured())
            {
                return;
            }

            List<string> imgPaths = new List<string>();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[]) e.Data.GetData(DataFormats.FileDrop);
                if (paths != null)
                {
                    foreach (string path in paths)
                    {
                        AddImagePaths(path, imgPaths);
                    }
                }
            }

            await Start(imgPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        private async Task Start(List<string> imgPaths)
        {
            if (imgPaths.Count > 0)
            {
                if (TinyGUI.Properties.Settings.Default.EnableSmartCut)
                {
                    uint width = 0;
                    uint height = 0;
                    if (uint.TryParse(_mainModel.ImageWidth, out uint result1))
                    {
                        width = result1;
                    }

                    if (uint.TryParse(_mainModel.ImageHeight, out uint result2))
                    {
                        height = result2;
                    }

                    if (_mainModel.ScaleRadioButtonIsChecked)
                    {
                        string type = "scale";
                        if (width != 0 || height != 0)
                        {
                            await Compress(imgPaths, type, width, height);
                        }
                    }
                    else if (_mainModel.FitRadioButtonIsChecked)
                    {
                        string type = "fit";
                        if (width != 0 && height != 0)
                        {
                            await Compress(imgPaths, type, width, height);
                        }
                    }
                    else if (_mainModel.CoverRadioButtonIsChecked)
                    {
                        string type = "cover";
                        if (width != 0 && height != 0)
                        {
                            await Compress(imgPaths, type, width, height);
                        }
                    }
                    else if (_mainModel.ThumbRadioButtonIsChecked)
                    {
                        string type = "thumb";
                        if (width != 0 && height != 0)
                        {
                            await Compress(imgPaths, type, width, height);
                        }
                    }
                }
                else
                {
                    await Compress(imgPaths);
                }
            }
        }

        private bool EnsureApiKeyConfigured()
        {
            if (!string.IsNullOrEmpty(TinyGUI.Properties.Settings.Default.Key))
            {
                return true;
            }

            _mainModel.SettingRadioButtonIsChecked = true;
            MessageBox.Show(
                TinyGUI.Properties.Resources.ConfigureApiKeyFirst,
                "TinyGUI",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            KeyTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                KeyTextBox.Focus();
                Keyboard.Focus(KeyTextBox);
                KeyTextBox.SelectAll();
            }));
            return false;
        }

        private async Task Compress(List<string> imgPaths)
        {
            imgPaths.Sort();
            _mainModel.IsIndeterminate = true;

            int i = 0;
            foreach (string path in imgPaths)
            {
                CompressionHistoryItem historyItem = CreateHistoryItem(path);
                _mainModel.CompressionHistoryItems.Insert(0, historyItem);
                _mainModel.HasCompressionHistoryItems = true;
                try
                {
                    EnsureTinifyKey();
                    var source = await UploadSourceFromFile(path, historyItem);
                    source = Preserve(source);
                    historyItem.BeginProcessing();
                    string savePath = GetSavePath(path);

                    await source.ToFile(savePath);
                    historyItem.Complete(savePath, new FileInfo(savePath).Length);
                    UpdateCompressionCount();
                }
                catch (Exception ex)
                {
                    historyItem.Fail(ex.Message);
                }
                finally
                {
                    i++;
                    _mainModel.ProgressBarValue = (i + 0.0) / imgPaths.Count;
                }
            }

            _mainModel.IsIndeterminate = false;
            _mainModel.ProgressBarValue = 0;
        }

        private async Task Compress(List<string> imgPaths, string type, uint width, uint height)
        {
            imgPaths.Sort();
            _mainModel.IsIndeterminate = true;

            int i = 0;
            foreach (string path in imgPaths)
            {
                CompressionHistoryItem historyItem = CreateHistoryItem(path);
                _mainModel.CompressionHistoryItems.Insert(0, historyItem);
                _mainModel.HasCompressionHistoryItems = true;
                try
                {
                    EnsureTinifyKey();
                    var source = await UploadSourceFromFile(path, historyItem);
                    source = Preserve(source);
                    historyItem.BeginProcessing();
                    string savePath = GetSavePath(path);

                    switch (type)
                    {
                        case "scale":
                        {
                            if (width > 0)
                            {
                                var resized = source.Resize(new
                                {
                                    method = type,
                                    width = width
                                });

                                await resized.ToFile(savePath);
                            }
                            else if (height > 0)
                            {
                                var resized = source.Resize(new
                                {
                                    method = type,
                                    height = height
                                });

                                await resized.ToFile(savePath);
                            }

                            break;
                        }

                        case "fit":
                        case "cover":
                        case "thumb":
                        {
                            var resized = source.Resize(new
                            {
                                method = type,
                                width = width,
                                height = height
                            });

                            await resized.ToFile(savePath);
                            break;
                        }
                    }

                    historyItem.Complete(savePath, new FileInfo(savePath).Length);
                    UpdateCompressionCount();
                }
                catch (Exception ex)
                {
                    historyItem.Fail(ex.Message);
                }
                finally
                {
                    i++;
                    _mainModel.ProgressBarValue = (i + 0.0) / imgPaths.Count;
                }
            }

            _mainModel.IsIndeterminate = false;
            _mainModel.ProgressBarValue = 0;
        }

        private static void EnsureTinifyKey()
        {
            Tinify.Key = TinyGUI.Properties.Settings.Default.Key;
        }

        private static async Task<Source> UploadSourceFromFile(string path, CompressionHistoryItem historyItem)
        {
            byte[] data = File.ReadAllBytes(path);
            using (ProgressByteArrayContent content = new ProgressByteArrayContent(data, progress =>
                   {
                       Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                       {
                           historyItem.ProgressValue = progress;
                       }));
                   }))
            {
                HttpResponseMessage response = await Tinify.Client.Request(HttpMethod.Post, new Uri("/shrink", UriKind.Relative), content);
                Uri location = response.Headers.Location;
                ConstructorInfo constructor = typeof(Source).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] {typeof(Uri), typeof(Dictionary<string, object>)},
                    null);

                if (location == null || constructor == null)
                {
                    throw new InvalidOperationException(TinyGUI.Properties.Resources.UnableCreateCompressedSource);
                }

                return (Source) constructor.Invoke(new object[] {location, new Dictionary<string, object>()});
            }
        }

        private static string GetSavePath(string path)
        {
            int saveMode = TinyGUI.Properties.Settings.Default.SaveMode;
            if (saveMode == 2)
            {
                return path;
            }

            string extension = Path.GetExtension(path).ToLower();
            string fileName = Path.GetFileName(path);
            string directoryName = Path.GetDirectoryName(path);
            Debug.Assert(directoryName != null, nameof(directoryName) + " != null");
            if (saveMode == 3 && !string.IsNullOrWhiteSpace(TinyGUI.Properties.Settings.Default.OutputFolder))
            {
                string outputDirectory = TinyGUI.Properties.Settings.Default.OutputFolder;
                Directory.CreateDirectory(outputDirectory);
                return GetAvailableSavePath(Path.Combine(outputDirectory, fileName));
            }

            return Path.Combine(directoryName, $"{fileName.Substring(0, fileName.Length - extension.Length)}-{GetTimeStamp()}{extension}");
        }

        private static string GetAvailableSavePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            string extension = Path.GetExtension(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string directoryName = Path.GetDirectoryName(path);
            Debug.Assert(directoryName != null, nameof(directoryName) + " != null");
            return Path.Combine(directoryName, $"{fileName}-{GetTimeStamp()}{extension}");
        }

        private static void MigrateSaveModeSetting()
        {
            if (TinyGUI.Properties.Settings.Default.ReplaceOriginalImage && TinyGUI.Properties.Settings.Default.SaveMode == 1)
            {
                TinyGUI.Properties.Settings.Default.SaveMode = 2;
                TinyGUI.Properties.Settings.Default.ReplaceOriginalImage = false;
                TinyGUI.Properties.Settings.Default.Save();
            }
        }

        private async Task RefreshCompressionCount()
        {
            if (string.IsNullOrEmpty(TinyGUI.Properties.Settings.Default.Key))
            {
                _mainModel.CompressionCountText = "0/500";
                return;
            }

            try
            {
                EnsureTinifyKey();
                await Tinify.Validate();
                UpdateCompressionCount();
            }
            catch
            {
                _mainModel.CompressionCountText = "0/500";
                // Invalid keys are already surfaced when the user tries to process an image.
            }
        }

        private void UpdateCompressionCount()
        {
            uint? count = Tinify.CompressionCount;
            if (count.HasValue)
            {
                _mainModel.CompressionCountText = $"{count.Value}/500";
            }
        }

        private static CompressionHistoryItem CreateHistoryItem(string path)
        {
            long originalSize = 0;
            try
            {
                originalSize = new FileInfo(path).Length;
            }
            catch
            {
                originalSize = 0;
            }

            return new CompressionHistoryItem
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                OriginalSize = originalSize,
                OriginalSizeText = CompressionHistoryItem.FormatSize(originalSize),
                Thumbnail = CreateThumbnail(path)
            };
        }

        private static ImageSource CreateThumbnail(string path)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 42;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        //保留元数据
        private Source Preserve(Source source)
        {
            List<string> metas = new List<string>();
            if (TinyGUI.Properties.Settings.Default.MetaCopyright)
            {
                metas.Add("copyright");
            }

            if (TinyGUI.Properties.Settings.Default.MetaLocation)
            {
                metas.Add("location");
            }

            if (TinyGUI.Properties.Settings.Default.MetaCreationTime)
            {
                metas.Add("creation");
            }

            if (metas.Count > 0)
            {
                return source.Preserve(metas.ToArray());
            }

            return source;
        }

        private void ViewImageMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            CompressionHistoryItem historyItem = GetHistoryItem(sender);
            OpenImage(historyItem);
        }

        private void HistoryItem_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                CompressionHistoryItem historyItem = GetHistoryItem(sender);
                OpenImage(historyItem);
            }
        }

        private static void OpenImage(CompressionHistoryItem historyItem)
        {
            string path = GetExistingImagePath(historyItem);
            if (!string.IsNullOrEmpty(path))
            {
                Process.Start(new ProcessStartInfo(path) {UseShellExecute = true});
            }
        }

        private void OpenInExplorerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            CompressionHistoryItem historyItem = GetHistoryItem(sender);
            string path = GetExistingImagePath(historyItem);
            if (!string.IsNullOrEmpty(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        }

        private static CompressionHistoryItem GetHistoryItem(object sender)
        {
            return (sender as FrameworkElement)?.DataContext as CompressionHistoryItem;
        }

        private static string GetExistingImagePath(CompressionHistoryItem historyItem)
        {
            if (historyItem == null)
            {
                return null;
            }

            string path = historyItem.GetImagePath();
            if (File.Exists(path))
            {
                return path;
            }

            return File.Exists(historyItem.FilePath) ? historyItem.FilePath : null;
        }

        private static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }

        private static bool IsImage(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".avif", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static void AddImagePaths(string path, List<string> imgPaths)
        {
            if (File.Exists(path))
            {
                if (IsImage(path))
                {
                    imgPaths.Add(path);
                }

                return;
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                foreach (string filePath in Directory.EnumerateFiles(path))
                {
                    if (IsImage(filePath))
                    {
                        imgPaths.Add(filePath);
                    }
                }

                foreach (string directoryPath in Directory.EnumerateDirectories(path))
                {
                    AddImagePaths(directoryPath, imgPaths);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        private void VersionHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/chenjing1294/TinyGUI");
        }

        private void TinifyHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(TinyGUI.Properties.Resources.KeyUrl);
        }

        private void RedisantHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(TinyGUI.Properties.Resources.Redisant);
        }

        private void OutputFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = TinyGUI.Properties.Resources.SelectOutputFolder;
                dialog.SelectedPath = TinyGUI.Properties.Settings.Default.OutputFolder;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TinyGUI.Properties.Settings.Default.OutputFolder = dialog.SelectedPath;
                    TinyGUI.Properties.Settings.Default.Save();
                }
            }
        }

        private class ProgressByteArrayContent : HttpContent
        {
            private readonly byte[] _data;
            private readonly Action<double> _progressChanged;

            public ProgressByteArrayContent(byte[] data, Action<double> progressChanged)
            {
                _data = data;
                _progressChanged = progressChanged;
                Headers.ContentLength = _data.Length;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                const int bufferSize = 81920;
                int uploaded = 0;
                while (uploaded < _data.Length)
                {
                    int count = Math.Min(bufferSize, _data.Length - uploaded);
                    await stream.WriteAsync(_data, uploaded, count);
                    uploaded += count;
                    _progressChanged(_data.Length == 0 ? 1 : uploaded / (double) _data.Length);
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _data.Length;
                return true;
            }
        }
    }
}
