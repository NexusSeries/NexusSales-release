using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Timer = System.Timers.Timer;
using NexusSales.Utils;
using NexusSales.Core.Commanding;
using System.Configuration;
using System.Media;
using CsvHelper;

namespace NexusSales.UI.Facebook
{
    // Move CommentRow to top-level public class
    public class CommentRow : INotifyPropertyChanged
    {
        public string Author { get; set; }
        public string PostId { get; set; }
        public string CommentId { get; set; }
        public string Message { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public bool IsCurrent { get; set; }
        public string ReactionVisualStatus { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public partial class CommentView : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<CommentRow> Comments { get; set; } = new ObservableCollection<CommentRow>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // --- State for UI feedback ---
        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(nameof(IsProcessing)); UpdateClearButtonState(); }
        }
        private int _processedCount;
        public int ProcessedCount
        {
            get => _processedCount;
            set { _processedCount = value; OnPropertyChanged(nameof(ProcessedCount)); }
        }
        public int TotalCount => Comments?.Count ?? 0;
        public string TotalCountDisplay => FormatCount(TotalCount);
        private string _summaryText;
        public string SummaryText
        {
            get => $"Total: {TotalCountDisplay}";
            set { _summaryText = value; OnPropertyChanged(nameof(SummaryText)); }
        }
        private string _elapsedTime;
        public string ElapsedTime
        {
            get => string.IsNullOrEmpty(_elapsedTime) ? "00:00" : _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(nameof(ElapsedTime)); }
        }
        private string _estimatedTime;
        public string EstimatedTime
        {
            get => string.IsNullOrEmpty(_estimatedTime) ? "00:00" : _estimatedTime;
            set { _estimatedTime = value; OnPropertyChanged(nameof(EstimatedTime)); }
        }
        private Timer _timer;
        private DateTime _startTime;

        // --- Static event for ExtractDataView integration ---
        public static Action<List<CommentRow>> CommentsPushedFromExtract;

        // --- Custom Dropdown State ---
        public ObservableCollection<string> ModeOptions { get; set; } = new ObservableCollection<string> { "Preview Only", "Apply" };
        private string _selectedMode = "Preview Only";
        public string SelectedMode
        {
            get => _selectedMode;
            set { _selectedMode = value; OnPropertyChanged(nameof(SelectedMode)); }
        }

        private Button _clearButton;

        public CommentView()
        {
            InitializeComponent();
            DataContext = this;
            CommentsPushedFromExtract += OnCommentsPushedFromExtract;
            _clearButton = null;
        }

        private void OnCommentsPushedFromExtract(List<CommentRow> rows)
        {
            Comments.Clear();
            foreach (var row in rows)
                Comments.Add(row);
            OnPropertyChanged(nameof(Comments));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(TotalCountDisplay));
            OnPropertyChanged(nameof(SummaryText));
        }

        // --- XAML event handlers ---
        private async void CommentView_BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx" };
            if (dlg.ShowDialog() == true)
            {
                UploadedFilePath = dlg.FileName;
                Comments.Clear();
                IsProcessing = true;
                try
                {
                    // Run file extraction and parsing in a background task
                    var result = await Task.Run(() =>
                    {
                        var commentRows = new List<CommentRow>();
                        var extractor = new FacebookCommentIdExtractor();
                        int count = extractor.Extract(dlg.FileName);
                        var extractedIds = new HashSet<string>();
                        // Read extracted IDs from output Excel
                        using (var workbook = new ClosedXML.Excel.XLWorkbook(extractor.Config.OutputExcelPath))
                        {
                            var ws = workbook.Worksheets.First();
                            foreach (var row in ws.RowsUsed().Skip(1))
                            {
                                var id = row.Cell(1).GetValue<string>();
                                if (!string.IsNullOrWhiteSpace(id))
                                    extractedIds.Add(id);
                            }
                        }
                        // Now, reload the original file to get Name/Comment columns, and match with extracted IDs
                        if (dlg.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var workbook = new ClosedXML.Excel.XLWorkbook(dlg.FileName))
                            {
                                var ws = workbook.Worksheets.First();
                                var header = ws.FirstRowUsed();
                                var colNames = header.Cells().Select(c => c.GetValue<string>().Trim()).ToList();
                                int nameIdx = colNames.FindIndex(c => c.Equals("Name", StringComparison.OrdinalIgnoreCase));
                                int commentIdx = colNames.FindIndex(c => c.Equals("Comment", StringComparison.OrdinalIgnoreCase));
                                foreach (var row in ws.RowsUsed().Skip(1))
                                {
                                    var cells = row.Cells().ToList();
                                    string commentId = null;
                                    foreach (var cell in cells)
                                    {
                                        foreach (var id in FacebookCommentIdExtractor
                                            .UnderscoreIdRegex.Matches(cell.GetValue<string>() ?? string.Empty)
                                            .Cast<Match>().Select(m => m.Value)
                                            .Concat(FacebookCommentIdExtractor
                                                .DigitsOnlyIdRegex.Matches(cell.GetValue<string>() ?? string.Empty)
                                                .Cast<Match>().Select(m => m.Value)))
                                        {
                                            if (extractedIds.Contains(id))
                                            {
                                                commentId = id;
                                                break;
                                            }
                                        }
                                        if (commentId != null) break;
                                    }
                                    if (!string.IsNullOrWhiteSpace(commentId))
                                    {
                                        var commentRow = new CommentRow
                                        {
                                            Author = (nameIdx >= 0 && cells.Count > nameIdx) ? cells[nameIdx].GetValue<string>() : string.Empty,
                                            Comment = (commentIdx >= 0 && cells.Count > commentIdx) ? cells[commentIdx].GetValue<string>() : string.Empty,
                                            CommentId = commentId
                                        };
                                        commentRows.Add(commentRow);
                                    }
                                }
                            }
                        }
                        else if (dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                            {
                                Encoding = Encoding.UTF8,
                                DetectDelimiter = true,
                                BadDataFound = null,
                                MissingFieldFound = null,
                                HeaderValidated = null,
                                IgnoreBlankLines = true,
                                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                                Delimiter = ","
                            };
                            using (var reader = new StreamReader(dlg.FileName, Encoding.UTF8, true))
                            using (var csv = new CsvHelper.CsvReader(reader, config))
                            {
                                var records = csv.GetRecords<dynamic>().ToList();
                                if (records.Count == 0) return commentRows;
                                var headerDict = ((IDictionary<string, object>)records.FirstOrDefault())?.Keys.ToList();
                                int nameIdx = headerDict.FindIndex(c => c.Trim().Equals("Name", StringComparison.OrdinalIgnoreCase));
                                int commentIdx = headerDict.FindIndex(c => c.Trim().Equals("Comment", StringComparison.OrdinalIgnoreCase));
                                foreach (var record in records)
                                {
                                    var dict = (IDictionary<string, object>)record;
                                    string commentId = null;
                                    foreach (var cell in dict.Values)
                                    {
                                        string cellText = cell?.ToString() ?? string.Empty;
                                        foreach (var id in FacebookCommentIdExtractor
                                            .UnderscoreIdRegex.Matches(cellText)
                                            .Cast<Match>().Select(m => m.Value)
                                            .Concat(FacebookCommentIdExtractor
                                                .DigitsOnlyIdRegex.Matches(cellText)
                                                .Cast<Match>().Select(m => m.Value)))
                                        {
                                            if (extractedIds.Contains(id))
                                            {
                                                commentId = id;
                                                break;
                                            }
                                        }
                                        if (commentId != null) break;
                                    }
                                    if (!string.IsNullOrWhiteSpace(commentId))
                                    {
                                        var commentRow = new CommentRow
                                        {
                                            Author = (nameIdx >= 0 && dict.Count > nameIdx) ? dict.ElementAt(nameIdx).Value?.ToString() : string.Empty,
                                            Comment = (commentIdx >= 0 && dict.Count > commentIdx) ? dict.ElementAt(commentIdx).Value?.ToString() : string.Empty,
                                            CommentId = commentId
                                        };
                                        commentRows.Add(commentRow);
                                    }
                                }
                            }
                        }
                        return commentRows;
                    });

                    // UI thread: update Comments collection
                    Comments.Clear();
                    foreach (var row in result)
                        Comments.Add(row);
                    OnPropertyChanged(nameof(Comments));
                    OnPropertyChanged(nameof(TotalCount));
                    OnPropertyChanged(nameof(TotalCountDisplay));
                    OnPropertyChanged(nameof(SummaryText));
                    // SystemSounds.Asterisk.Play(); // Removed to prevent double sound
                    var blueBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("FbButtonPrimaryBackgroundBrush");
                    NexusSales.UI.Dialogs.MessageDialog.Show($"Extracted {Comments.Count} Comments Successfully.", "Info", "Info.wav", blueBrush);
                }
                catch (Exception ex)
                {
                    NexusSales.UI.Dialogs.MessageDialog.Show($"Failed to load file: {ex.Message}", "Error", "Warning.wav");
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }
        private void CommentView_AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeButton_Click(sender, e);
        }

        // --- Play sound helper ---
        private void PlaySound(string soundFileName)
        {
            try
            {
                string uriString = $"pack://application:,,,/NexusSales.UI;component/Audio/{soundFileName}";
                var resourceInfo = System.Windows.Application.GetResourceStream(new Uri(uriString));
                if (resourceInfo != null)
                {
                    using (var stream = resourceInfo.Stream)
                    {
                        var player = new System.Media.SoundPlayer(stream);
                        player.Play();
                    }
                }
            }
            catch { /* Ignore sound errors */ }
        }

        // --- Show warning dialog helper ---
        private void ShowWarningDialog(string message)
        {
            // Only play the warning sound, do not show the dialog
            PlaySound("Warning.wav");
        }

        // --- Show info dialog helper ---
        private void ShowInfoDialog(string message)
        {
            var blueBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("FbButtonPrimaryBackgroundBrush");
            NexusSales.UI.Dialogs.MessageDialog.Show(message, "Info", "Info.wav", blueBrush);
        }

        // --- Dummy reply generator ---
        private string GenerateReplyForComment(string comment)
        {
            // Replace with your real logic or AI integration
            if (string.IsNullOrWhiteSpace(comment)) return "Thank you!";
            if (comment.ToLower().Contains("interested")) return "Thank you for your interest!";
            return "Thank you for your comment!";
        }

        // --- Cancel support ---
        private CancellationTokenSource _analyzeCts;

        // --- Clear all data and reset UI ---
        private void ClearAll()
        {
            Comments.Clear();
            ProcessedCount = 0;
            ElapsedTime = "";
            EstimatedTime = "";
            SummaryText = "";
            UploadedFilePath = string.Empty;
            _timer?.Stop();
            _timer = null;
            IsProcessing = false;
        }

        private void CommentView_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Only allow clear if not processing
            if (IsProcessing)
                return;
            ClearAll();
        }

        private void CommentView_CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _analyzeCts?.Cancel();
            IsProcessing = false;
            _timer?.Stop();
        }

        // --- Analyze logic ---
        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all state and UI before starting a new analyze
            ProcessedCount = 0;
            ElapsedTime = "";
            EstimatedTime = "";
            SummaryText = "";
            _timer?.Stop();
            _timer = null;
            foreach (var row in Comments)
            {
                row.Status = string.Empty;
                row.ReactionVisualStatus = string.Empty;
                row.Message = string.Empty;
                row.OnPropertyChanged(nameof(row.Status));
                row.OnPropertyChanged(nameof(row.ReactionVisualStatus));
                row.OnPropertyChanged(nameof(row.Message));
            }

            Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Analyze started. Comments count: {Comments.Count}");
            if (IsProcessing) return;
            if (Comments.Count == 0)
            {
                Logger.Log("WARNING", "CommentView", "AnalyzeButton_Click", "No comments to reply to.");
                ShowWarningDialog("No comments to reply to.");
                return;
            }
            string accessToken = System.Configuration.ConfigurationManager.AppSettings["access_token"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Logger.Log("ERROR", "CommentView", "AnalyzeButton_Click", "access_token must be set in App.config.");
                ShowWarningDialog("access_token must be set in App.config.");
                return;
            }
            IsProcessing = true;
            _analyzeCts = new CancellationTokenSource();
            var token = _analyzeCts.Token;
            _startTime = DateTime.Now;
            _timer = new Timer(1000);
            _timer.Elapsed += (s, args) =>
            {
                var ts = DateTime.Now - _startTime;
                ElapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            };
            _timer.Start();
            foreach (var row in Comments)
            {
                if (string.IsNullOrWhiteSpace(row.Message))
                {
                    Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Generating reply for commentId={row.CommentId}");
                    row.Message = GenerateReplyForComment(row.Comment);
                    row.OnPropertyChanged(nameof(row.Message));
                }
            }
            var commentIdReplyPairs = Comments
                .Where(c => !string.IsNullOrWhiteSpace(c.CommentId))
                .Select(c => new { c.CommentId, Reply = c.Message })
                .ToList();
            Logger.Log("DEBUG", "CommentView", "AnalyzeButton_Click", $"CommentIdReplyPairs: {string.Join(", ", commentIdReplyPairs.Select(p => p.CommentId))}");
            int processed = 0, success = 0, fail = 0;
            foreach (var pair in commentIdReplyPairs)
            {
                if (token.IsCancellationRequested)
                {
                    Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", "Operation cancelled by user.");
                    break;
                }
                Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Processing reply for commentId={pair.CommentId}, reply={pair.Reply}");
                try
                {
                    Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Directly calling FacebookHandler.ReplyToComment for commentId={pair.CommentId}");
                    var resultJson = await Task.Run(() => NexusSales.Core.Commanding.Handlers.FacebookHandler.ReplyToComment(pair.CommentId, pair.Reply, accessToken));
                    Logger.Log("DEBUG", "CommentView", "AnalyzeButton_Click", $"Raw resultJson for commentId={pair.CommentId}: {resultJson}");
                    var result = Newtonsoft.Json.Linq.JObject.Parse(resultJson);
                    var row = Comments.FirstOrDefault(c => c.CommentId == pair.CommentId);
                    if (result.Value<bool>("success"))
                    {
                        Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Reply success for commentId={pair.CommentId}");
                        if (row != null)
                        {
                            row.Status = "Replied";
                            row.ReactionVisualStatus = "Success";
                            row.OnPropertyChanged(nameof(row.Status));
                            row.OnPropertyChanged(nameof(row.ReactionVisualStatus));
                            var idx = Comments.IndexOf(row);
                            if (idx >= 0)
                            {
                                Comments[idx] = row;
                            }
                        }
                        success++;
                    }
                    else
                    {
                        Logger.Log("ERROR", "CommentView", "AnalyzeButton_Click", $"Reply failed for commentId={pair.CommentId}: {result["error"]}");
                        if (row != null)
                        {
                            row.Status = "Fail: " + (result["error"]?.ToString() ?? "");
                            row.ReactionVisualStatus = "Fail";
                            row.OnPropertyChanged(nameof(row.Status));
                            row.OnPropertyChanged(nameof(row.ReactionVisualStatus));
                            var idx = Comments.IndexOf(row);
                            if (idx >= 0)
                            {
                                Comments[idx] = row;
                            }
                        }
                        fail++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("FATAL", "CommentView", "AnalyzeButton_Click", $"Exception for commentId={pair.CommentId}: {ex.Message}", ex);
                    var row = Comments.FirstOrDefault(c => c.CommentId == pair.CommentId);
                    if (row != null)
                    {
                        row.Status = "Exception: " + ex.Message;
                        row.ReactionVisualStatus = "Fail";
                        row.OnPropertyChanged(nameof(row.Status));
                        row.OnPropertyChanged(nameof(row.ReactionVisualStatus));
                    }
                    fail++;
                }
                processed++;
                ProcessedCount = processed;
                if (processed > 0)
                {
                    var secondsLeft = (DateTime.Now - _startTime).TotalSeconds / processed * (commentIdReplyPairs.Count - processed);
                    var tsLeft = TimeSpan.FromSeconds(secondsLeft);
                    EstimatedTime = $"~{(int)tsLeft.TotalHours:D2}:{tsLeft.Minutes:D2}:{tsLeft.Seconds:D2}";
                }
                else
                {
                    EstimatedTime = "--";
                }
            }
            Logger.Log("INFO", "CommentView", "AnalyzeButton_Click", $"Replies complete. Total: {Comments.Count}, Success: {success}, Fail: {fail}");
            SummaryText = $"Replies complete. Total: {Comments.Count}, Success: {success}, Fail: {fail}";
            IsProcessing = false;
            _timer?.Stop();
            if (success == Comments.Count && fail == 0 && Comments.Count > 0)
            {
                PlaySound("Success.wav");
            }
        }

        // --- For ExtractDataView integration ---
        public static string SummarizeComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return "";
            if (comment.ToLower().Contains("interested")) return "Interested!";
            if (comment.Length > 40) return comment.Substring(0, 40) + "...";
            return comment;
        }

        public string UploadedFilePath
        {
            get { return (string)GetValue(UploadedFilePathProperty); }
            set { SetValue(UploadedFilePathProperty, value); OnPropertyChanged(nameof(UploadedFilePath)); }
        }
        public static readonly DependencyProperty UploadedFilePathProperty =
            DependencyProperty.Register("UploadedFilePath", typeof(string), typeof(CommentView), new PropertyMetadata(""));

        private string FormatCount(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 10000) return (count / 1000.0).ToString("0.#") + "k";
            if (count < 1000000) return (count / 1000.0).ToString("0.#") + "k";
            return (count / 1000000.0).ToString("0.#") + "M";
        }

        // --- Custom Dropdown Handlers ---
        private void CustomDropdownButton_Click(object sender, RoutedEventArgs e)
        {
            var popup = this.FindName("CustomDropdownPopup") as Popup;
            if (popup != null)
                popup.IsOpen = true;
        }

        private void CustomDropdownListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var popup = this.FindName("CustomDropdownPopup") as Popup;
            if (popup != null)
                popup.IsOpen = false;
        }

        private void UpdateClearButtonState()
        {
            if (_clearButton != null)
            {
                _clearButton.IsEnabled = !IsProcessing;
                _clearButton.Cursor = IsProcessing ? Cursors.No : Cursors.Hand;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_clearButton == null)
                _clearButton = this.FindName("ClearButton") as Button;
            UpdateClearButtonState();
        }
    }
}
