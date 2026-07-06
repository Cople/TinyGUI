using System.Collections.ObjectModel;
using System.Windows.Media;
using TinyGUI.Properties;

namespace TinyGUI.ViewModels
{
    public class MainModel : ViewModelBase
    {
        private bool _compressRadioButtonIsChecked = true;

        public bool CompressRadioButtonIsChecked
        {
            get => _compressRadioButtonIsChecked;
            set => SetField(ref _compressRadioButtonIsChecked, value, nameof(CompressRadioButtonIsChecked));
        }

        private bool _historyRadioButtonIsChecked;

        public bool HistoryRadioButtonIsChecked
        {
            get => _historyRadioButtonIsChecked;
            set => SetField(ref _historyRadioButtonIsChecked, value, nameof(HistoryRadioButtonIsChecked));
        }

        private bool _smartCutRadioButtonIsChecked;

        public bool SmartCutRadioButtonIsChecked
        {
            get => _smartCutRadioButtonIsChecked;
            set => SetField(ref _smartCutRadioButtonIsChecked, value, nameof(SmartCutRadioButtonIsChecked));
        }

        private bool _settingRadioButtonIsChecked;

        public bool SettingRadioButtonIsChecked
        {
            get => _settingRadioButtonIsChecked;
            set => SetField(ref _settingRadioButtonIsChecked, value, nameof(SettingRadioButtonIsChecked));
        }

        #region 智能剪切

        private string _imageWidth = string.Empty;

        public string ImageWidth
        {
            get => _imageWidth;
            set => SetField(ref _imageWidth, value, nameof(ImageWidth));
        }

        private string _imageHeight = string.Empty;

        public string ImageHeight
        {
            get => _imageHeight;
            set => SetField(ref _imageHeight, value, nameof(ImageHeight));
        }

        private bool _scaleRadioButtonIsChecked = true;

        public bool ScaleRadioButtonIsChecked
        {
            get => _scaleRadioButtonIsChecked;
            set => SetField(ref _scaleRadioButtonIsChecked, value, nameof(ScaleRadioButtonIsChecked));
        }

        private bool _fitRadioButtonIsChecked;

        public bool FitRadioButtonIsChecked
        {
            get => _fitRadioButtonIsChecked;
            set => SetField(ref _fitRadioButtonIsChecked, value, nameof(FitRadioButtonIsChecked));
        }

        private bool _coverRadioButtonIsChecked;

        public bool CoverRadioButtonIsChecked
        {
            get => _coverRadioButtonIsChecked;
            set => SetField(ref _coverRadioButtonIsChecked, value, nameof(CoverRadioButtonIsChecked));
        }

        private bool _thumbRadioButtonIsChecked;

        public bool ThumbRadioButtonIsChecked
        {
            get => _thumbRadioButtonIsChecked;
            set => SetField(ref _thumbRadioButtonIsChecked, value, nameof(ThumbRadioButtonIsChecked));
        }

        #endregion

        private bool _isIndeterminate = false;

        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetField(ref _isIndeterminate, value, nameof(IsIndeterminate));
        }

        private double _progressBarValue = 0.0;

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                if (value > 0)
                {
                    IsIndeterminate = false;
                }

                SetField(ref _progressBarValue, value, nameof(ProgressBarValue));
            }
        }

        public ObservableCollection<CompressionHistoryItem> CompressionHistoryItems { get; } =
            new ObservableCollection<CompressionHistoryItem>();

        private bool _hasCompressionHistoryItems;

        public bool HasCompressionHistoryItems
        {
            get => _hasCompressionHistoryItems;
            set => SetField(ref _hasCompressionHistoryItems, value, nameof(HasCompressionHistoryItems));
        }

        private string _compressionCountText = "0";

        public string CompressionCountText
        {
            get => _compressionCountText;
            set => SetField(ref _compressionCountText, value, nameof(CompressionCountText));
        }
    }

    public class CompressionHistoryItem : ViewModelBase
    {
        public string FilePath { get; set; }

        public string OutputPath { get; private set; }

        public ImageSource Thumbnail { get; set; }

        public string FileName { get; set; }

        public string OriginalSizeText { get; set; }

        public long OriginalSize { get; set; }

        private double _progressValue;

        public double ProgressValue
        {
            get => _progressValue;
            set => SetField(ref _progressValue, value, nameof(ProgressValue));
        }

        private bool _isProgressIndeterminate;

        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            set => SetField(ref _isProgressIndeterminate, value, nameof(IsProgressIndeterminate));
        }

        private string _compressedSizeText = "-";

        public string CompressedSizeText
        {
            get => _compressedSizeText;
            set => SetField(ref _compressedSizeText, value, nameof(CompressedSizeText));
        }

        private string _reductionText = "-";

        public string ReductionText
        {
            get => _reductionText;
            set => SetField(ref _reductionText, value, nameof(ReductionText));
        }

        private bool _isProcessing = true;

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetField(ref _isProcessing, value, nameof(IsProcessing)))
                {
                    SetField(ref _isCompleted, !value && !HasError, nameof(IsCompleted));
                }
            }
        }

        private bool _isCompleted;

        public bool IsCompleted
        {
            get => _isCompleted;
            private set => SetField(ref _isCompleted, value, nameof(IsCompleted));
        }

        private bool _hasError;

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (SetField(ref _hasError, value, nameof(HasError)) && value)
                {
                    IsCompleted = false;
                }
            }
        }

        private string _statusText = Resources.Uploading;

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value, nameof(StatusText));
        }

        public void Complete(string outputPath, long compressedSize)
        {
            OutputPath = outputPath;
            CompressedSizeText = FormatSize(compressedSize);
            ReductionText = FormatReduction(OriginalSize, compressedSize);
            StatusText = Resources.Completed;
            ProgressValue = 1;
            IsProgressIndeterminate = false;
            HasError = false;
            IsProcessing = false;
            IsCompleted = true;
        }

        public void Fail(string error)
        {
            CompressedSizeText = "-";
            ReductionText = "-";
            StatusText = error;
            IsProgressIndeterminate = false;
            HasError = true;
            IsProcessing = false;
            IsCompleted = false;
        }

        public void BeginProcessing()
        {
            StatusText = Resources.Compressing;
            IsProgressIndeterminate = true;
        }

        public string GetImagePath()
        {
            return string.IsNullOrEmpty(OutputPath) ? FilePath : OutputPath;
        }

        public static string FormatSize(long bytes)
        {
            string[] units = {"B", "KB", "MB", "GB"};
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        private static string FormatReduction(long originalSize, long compressedSize)
        {
            if (originalSize <= 0)
            {
                return "-";
            }

            double reduction = (originalSize - compressedSize) * 100.0 / originalSize;
            return $"-{reduction:0.##}%";
        }
    }
}
