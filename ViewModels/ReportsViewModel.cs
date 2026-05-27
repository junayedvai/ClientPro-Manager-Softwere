using System.Collections.ObjectModel;
using System.Windows.Input;
using ClientProManager.Helpers;
using ClientProManager.Models;
using ClientProManager.Services;

namespace ClientProManager.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly IPermissionService _permissionService;
    private readonly IDialogService _dialogService;
    private string _selectedReportType;
    private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _toDate = DateTime.Today;

    public ReportsViewModel(
        IReportService reportService,
        IExportService exportService,
        IPermissionService permissionService,
        IDialogService dialogService)
    {
        _reportService = reportService;
        _exportService = exportService;
        _permissionService = permissionService;
        _dialogService = dialogService;
        _selectedReportType = _reportService.ReportTypes.First();
        RunReportCommand = new AsyncRelayCommand(async _ => await RunReportAsync());
        ExportPdfCommand = new AsyncRelayCommand(async _ => await ExportPdfAsync());
        ExportExcelCommand = new AsyncRelayCommand(async _ => await ExportExcelAsync());
        ExportCsvCommand = new AsyncRelayCommand(async _ => await ExportCsvAsync());
        _ = RunReportAsync();
    }

    public string[] ReportTypes => _reportService.ReportTypes;
    public ObservableCollection<ReportRow> Rows { get; } = [];

    public string SelectedReportType
    {
        get => _selectedReportType;
        set => SetProperty(ref _selectedReportType, value);
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ICommand RunReportCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportCsvCommand { get; }

    private async Task RunReportAsync()
    {
        Rows.ReplaceWith(await _reportService.BuildReportAsync(SelectedReportType, FromDate, ToDate));
        StatusMessage = $"{Rows.Count} report row(s) generated.";
    }

    private bool CanExport()
    {
        if (_permissionService.CanExportReports())
        {
            return true;
        }

        _dialogService.Error("Only admin and accountant users can export reports.");
        return false;
    }

    private async Task ExportPdfAsync()
    {
        if (!CanExport())
        {
            return;
        }

        var path = await _exportService.ExportReportPdfAsync(SelectedReportType, Rows.ToList(), FromDate, ToDate);
        _dialogService.Info($"PDF report exported:\n{path}");
    }

    private async Task ExportExcelAsync()
    {
        if (!CanExport())
        {
            return;
        }

        var path = await _exportService.ExportReportExcelAsync(SelectedReportType, Rows.ToList(), FromDate, ToDate);
        _dialogService.Info($"Excel report exported:\n{path}");
    }

    private async Task ExportCsvAsync()
    {
        if (!CanExport())
        {
            return;
        }

        var path = await _exportService.ExportReportCsvAsync(SelectedReportType, Rows.ToList(), FromDate, ToDate);
        _dialogService.Info($"CSV report exported:\n{path}");
    }
}
