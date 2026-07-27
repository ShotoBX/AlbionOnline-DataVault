using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models.TranslationModel;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace StatisticsAnalysisTool.Dungeon;

public class DungeonBindings : BaseViewModel
{
    private ObservableRangeCollection<DungeonBaseFragment> _dungeons = new();
    private ListCollectionView _dungeonsCollectionView;
    private DungeonCloseTimer _dungeonCloseTimer = new();
    private DungeonStatsFilter _dungeonStatsFilter;
    private DungeonStats _stats = new();
    private GridLength _gridSplitterPosition;
    private DungeonsTranslation _translation = new();
    private DungeonStatsFilterStruct _selectedStatsTimeType;
    private DungeonStats _dungeonStatsSelection;
    private DungeonOptionsObject _dungeonOptionsObject = new();
    private StatsTypeFilterStruct _selectedDungeonStatsType;
    private DungeonBaseFragment _bestRun;
    private ObservableCollection<ISeries> _seriesDungeonEfficiency = [];
    private Axis[] _xAxesDungeonEfficiency = [new Axis { LabelsPaint = AxisLabelPaint, SeparatorsPaint = AxisSeparatorPaint }];
    private Axis[] _yAxesDungeonEfficiency = [new Axis { LabelsPaint = AxisLabelPaint, SeparatorsPaint = AxisSeparatorPaint }];
    private bool _hasDungeonEfficiencyData;

    public DungeonBindings()
    {
        DungeonStatTimeTypes = new List<DungeonStatsFilterStruct>
        {
            new ()
            {
                Name = LocalizationController.Translation("TODAY"),
                StatTimeType = DungeonStatTimeType.Today
            },
            new ()
            {
                Name = LocalizationController.Translation("LAST_7_DAYS"),
                StatTimeType = DungeonStatTimeType.Last7Days
            },
            new ()
            {
                Name = LocalizationController.Translation("LAST_30_DAYS"),
                StatTimeType = DungeonStatTimeType.Last30Days
            },
            new ()
            {
                Name = LocalizationController.Translation("LAST_365_DAYS"),
                StatTimeType = DungeonStatTimeType.Last365Days
            },
            new ()
            {
                Name = LocalizationController.Translation("TOTAL"),
                StatTimeType = DungeonStatTimeType.Total
            }
        };

        SelectedStatsTimeType = DungeonStatTimeTypes.FirstOrDefault(x => x.StatTimeType == DungeonStatTimeType.Total);

        DungeonStatsType =
        [
            new()
            {
                Name = LocalizationController.Translation("TOTAL_OVERVIEW"),
                StatsViewType = DungeonMode.Unknown
            },

            new()
            {
                Name = LocalizationController.Translation("SOLO_DUNGEON"),
                StatsViewType = DungeonMode.Solo
            },

            new()
            {
                Name = LocalizationController.Translation("STANDARD_DUNGEON"),
                StatsViewType = DungeonMode.Standard
            },

            new()
            {
                Name = LocalizationController.Translation("AVALONIAN_DUNGEON"),
                StatsViewType = DungeonMode.Avalon
            },

            new()
            {
                Name = LocalizationController.Translation("CORRUPTED"),
                StatsViewType = DungeonMode.Corrupted
            },

            new()
            {
                Name = LocalizationController.Translation("HELLGATE"),
                StatsViewType = DungeonMode.HellGate
            },

            new()
            {
                Name = LocalizationController.Translation("HCE_EXPEDITION"),
                StatsViewType = DungeonMode.Expedition
            },

            new()
            {
                Name = LocalizationController.Translation("MISTS"),
                StatsViewType = DungeonMode.Mists
            },

            new()
            {
                Name = LocalizationController.Translation("MISTS_DUNGEON"),
                StatsViewType = DungeonMode.MistsDungeon
            },

            new()
            {
                Name = LocalizationController.Translation("ABYSSALDEPTHS"),
                StatsViewType = DungeonMode.AbyssalDepths
            }
        ];

        SelectedDungeonStatsType = DungeonStatsType.FirstOrDefault(x => x.StatsViewType == DungeonMode.Unknown);
    }

    public void InitListCollectionView()
    {
        DungeonsCollectionView = CollectionViewSource.GetDefaultView(Dungeons) as ListCollectionView;
        if (DungeonsCollectionView != null)
        {
            DungeonsCollectionView.IsLiveSorting = true;
            DungeonsCollectionView.IsLiveFiltering = true;
            DungeonsCollectionView.CustomSort = new DungeonComparer();
            DungeonsCollectionView.Refresh();
        }
    }

    public ObservableRangeCollection<DungeonBaseFragment> Dungeons
    {
        get => _dungeons;
        set
        {
            _dungeons = value;
            OnPropertyChanged();
        }
    }

    public ListCollectionView DungeonsCollectionView
    {
        get => _dungeonsCollectionView;
        set
        {
            _dungeonsCollectionView = value;
            OnPropertyChanged();
        }
    }

    public DungeonCloseTimer DungeonCloseTimer
    {
        get => _dungeonCloseTimer;
        set
        {
            _dungeonCloseTimer = value;
            OnPropertyChanged();
        }
    }

    public DungeonStats Stats
    {
        get => _stats;
        set
        {
            _stats = value;
            OnPropertyChanged();
        }
    }

    public DungeonStatsFilter DungeonStatsFilter
    {
        get => _dungeonStatsFilter;
        set
        {
            _dungeonStatsFilter = value;
            OnPropertyChanged();
        }
    }

    public DungeonOptionsObject DungeonOptionsObject
    {
        get => _dungeonOptionsObject;
        set
        {
            _dungeonOptionsObject = value;
            OnPropertyChanged();
        }
    }

    public GridLength GridSplitterPosition
    {
        get => _gridSplitterPosition;
        set
        {
            _gridSplitterPosition = value;
            SettingsController.CurrentSettings.DungeonsGridSplitterPosition = _gridSplitterPosition.Value;
            OnPropertyChanged();
        }
    }

    public List<DungeonStatsFilterStruct> DungeonStatTimeTypes { get; }

    public DungeonStatsFilterStruct SelectedStatsTimeType
    {
        get => _selectedStatsTimeType;
        set
        {
            _selectedStatsTimeType = value;
            _ = UpdateFilteredDungeonsAsync();
            OnPropertyChanged();
        }
    }

    public List<StatsTypeFilterStruct> DungeonStatsType { get; }

    public StatsTypeFilterStruct SelectedDungeonStatsType
    {
        get => _selectedDungeonStatsType;
        set
        {
            _selectedDungeonStatsType = value;
            _ = UpdateFilteredDungeonsAsync();
            UpdateStatsView();
            OnPropertyChanged();
        }
    }

    public DungeonStats DungeonStatsSelection
    {
        get => _dungeonStatsSelection;
        set
        {
            _dungeonStatsSelection = value;
            OnPropertyChanged();
        }
    }

    public DungeonsTranslation Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            OnPropertyChanged();
        }
    }

    #region Insights (Best Run + Fame/Silver per hour by dungeon type)

    private static SolidColorPaint AxisLabelPaint => new(new SKColor(0xA8, 0x99, 0x79));
    private static SolidColorPaint AxisSeparatorPaint => new(new SKColor(0x24, 0x24, 0x28));

    private static readonly SKColor[] ChartPalette =
    [
        new SKColor(0x00, 0xB8, 0xFF), new SKColor(0x3D, 0xDC, 0x84), new SKColor(0xFF, 0xB7, 0x4D),
        new SKColor(0xFF, 0x5C, 0x5C), new SKColor(0xB8, 0x84, 0xFF), new SKColor(0x4D, 0xE0, 0xE0)
    ];

    public DungeonBaseFragment BestRun
    {
        get => _bestRun;
        set
        {
            _bestRun = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BestRunVisibility));
            OnPropertyChanged(nameof(BestRunEmptyVisibility));
        }
    }

    public Visibility BestRunVisibility => BestRun == null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility BestRunEmptyVisibility => BestRun == null ? Visibility.Visible : Visibility.Collapsed;

    public ObservableCollection<ISeries> SeriesDungeonEfficiency
    {
        get => _seriesDungeonEfficiency;
        set
        {
            _seriesDungeonEfficiency = value;
            OnPropertyChanged();
        }
    }

    public Axis[] XAxesDungeonEfficiency
    {
        get => _xAxesDungeonEfficiency;
        set
        {
            _xAxesDungeonEfficiency = value;
            OnPropertyChanged();
        }
    }

    public Axis[] YAxesDungeonEfficiency
    {
        get => _yAxesDungeonEfficiency;
        set
        {
            _yAxesDungeonEfficiency = value;
            OnPropertyChanged();
        }
    }

    public Visibility DungeonEfficiencyEmptyVisibility => _hasDungeonEfficiencyData ? Visibility.Collapsed : Visibility.Visible;

    public Visibility DungeonEfficiencyChartVisibility => _hasDungeonEfficiencyData ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Builds the "best run" highlight and the Fame/Silver-per-hour-by-dungeon-type comparison chart.
    /// Reuses the per-mode stats DungeonStats.Set() already computes (FamePerHour/SilverPerHour/Entered
    /// per StatsSolo/StatsStandard/etc.) - no new tracking or calculation, just a visual summary of it.
    /// </summary>
    public void RebuildDungeonInsights(List<DungeonBaseFragment> dungeons, DungeonStats stats)
    {
        BestRun = dungeons?.OrderByDescending(x => x.TotalValue).ThenByDescending(x => x.EnterDungeonFirstTime).FirstOrDefault();

        if (stats == null)
        {
            _hasDungeonEfficiencyData = false;
            OnPropertyChanged(nameof(DungeonEfficiencyEmptyVisibility));
            OnPropertyChanged(nameof(DungeonEfficiencyChartVisibility));
            return;
        }

        (string Name, int Entered, double FamePerHour, double SilverPerHour)[] modeData =
        [
            (LocalizationController.Translation("SOLO_DUNGEON"), stats.StatsSolo.Entered, stats.StatsSolo.FamePerHour, stats.StatsSolo.SilverPerHour),
            (LocalizationController.Translation("STANDARD_DUNGEON"), stats.StatsStandard.Entered, stats.StatsStandard.FamePerHour, stats.StatsStandard.SilverPerHour),
            (LocalizationController.Translation("AVALONIAN_DUNGEON"), stats.StatsAvalonian.Entered, stats.StatsAvalonian.FamePerHour, stats.StatsAvalonian.SilverPerHour),
            (LocalizationController.Translation("CORRUPTED"), stats.StatsCorrupted.Entered, stats.StatsCorrupted.FamePerHour, stats.StatsCorrupted.SilverPerHour),
            (LocalizationController.Translation("HELLGATE"), stats.StatsHellGate.Entered, stats.StatsHellGate.FamePerHour, stats.StatsHellGate.SilverPerHour),
            (LocalizationController.Translation("HCE_EXPEDITION"), stats.StatsExpedition.Entered, stats.StatsExpedition.FamePerHour, stats.StatsExpedition.SilverPerHour),
            (LocalizationController.Translation("MISTS"), stats.StatsMists.Entered, stats.StatsMists.FamePerHour, stats.StatsMists.SilverPerHour),
            (LocalizationController.Translation("MISTS_DUNGEON"), stats.StatsMistsDungeon.Entered, stats.StatsMistsDungeon.FamePerHour, stats.StatsMistsDungeon.SilverPerHour),
            (LocalizationController.Translation("ABYSSALDEPTHS"), stats.StatsAbyssalDepths.Entered, stats.StatsAbyssalDepths.FamePerHour, stats.StatsAbyssalDepths.SilverPerHour)
        ];

        var visited = modeData.Where(x => x.Entered > 0).ToList();
        _hasDungeonEfficiencyData = visited.Count > 0;

        SeriesDungeonEfficiency =
        [
            new ColumnSeries<double>
            {
                Name = LocalizationController.Translation("FAME_PER_HOUR"),
                Values = visited.Select(x => x.FamePerHour).ToList(),
                Fill = new SolidColorPaint(ChartPalette[0])
            },
            new ColumnSeries<double>
            {
                Name = LocalizationController.Translation("SILVER_PER_HOUR"),
                Values = visited.Select(x => x.SilverPerHour).ToList(),
                Fill = new SolidColorPaint(ChartPalette[1])
            }
        ];

        XAxesDungeonEfficiency =
        [
            new Axis
            {
                Labels = visited.Select(x => x.Name).ToArray(),
                LabelsRotation = 15,
                LabelsPaint = AxisLabelPaint,
                SeparatorsPaint = AxisSeparatorPaint
            }
        ];

        YAxesDungeonEfficiency =
        [
            new Axis
            {
                LabelsPaint = AxisLabelPaint,
                SeparatorsPaint = AxisSeparatorPaint
            }
        ];

        OnPropertyChanged(nameof(DungeonEfficiencyEmptyVisibility));
        OnPropertyChanged(nameof(DungeonEfficiencyChartVisibility));
    }

    public static string TranslationBestRun => LocalizationController.Translation("DUNGEON_BEST_RUN");
    public static string TranslationEfficiencyByType => LocalizationController.Translation("DUNGEON_EFFICIENCY_BY_TYPE");
    public static string TranslationEfficiencyEmpty => LocalizationController.Translation("DUNGEON_EFFICIENCY_EMPTY");
    public static string TranslationTotalLootedValue => LocalizationController.Translation("TOTAL_LOOT_VALUE");

    #endregion

    #region Filter

    private CancellationTokenSource _cancellationTokenSource;

    public async Task UpdateFilteredDungeonsAsync()
    {
        if (Dungeons?.Count <= 0 && DungeonsCollectionView?.Count <= 0)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var filteredDungeons = await Task.Run(ParallelDungeonFilterProcess, _cancellationTokenSource.Token);

            DungeonsCollectionView = CollectionViewSource.GetDefaultView(filteredDungeons) as ListCollectionView;
            var currentDungeons = DungeonsCollectionView?.Cast<DungeonBaseFragment>().ToList();
            Stats?.Set(currentDungeons);
            RebuildDungeonInsights(currentDungeons, Stats);
        }
        catch (TaskCanceledException)
        {
            // ignored
        }
    }

    public List<DungeonBaseFragment> ParallelDungeonFilterProcess()
    {
        var partitioner = Partitioner.Create(Dungeons, EnumerablePartitionerOptions.NoBuffering);
        var result = new ConcurrentBag<DungeonBaseFragment>();

        Parallel.ForEach(partitioner, (dungeon, state) =>
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                state.Stop();
            }

            if (Filter(dungeon))
            {
                result.Add(dungeon);
            }
        });

        return result.OrderByDescending(d => d.EnterDungeonFirstTime).ToList();
    }

    private bool Filter(object obj)
    {
        if (obj is not DungeonBaseFragment)
        {
            return false;
        }

        bool isLevelOkay = false;
        bool isTierOkay = false;
        bool isModeOkay = false;
        bool isMapTypeOkay = false;
        bool isTimestampOkay = false;

        if (IsLevelOkay(obj))
        {
            isLevelOkay = true;
        }
        
        if (IsTierOkay(obj))
        {
            isTierOkay = true;
        }

        if (IsModeOkay(obj))
        {
            isModeOkay = true;
        }

        if (IsMapTypeOkay(obj))
        {
            isMapTypeOkay = true;
        }

        if (IsTimestampOkay(obj))
        {
            isTimestampOkay = true;
        }

        return isLevelOkay && isTierOkay && isModeOkay && isMapTypeOkay && isTimestampOkay;
    }

    private bool IsTierOkay(object obj)
    {
        if (obj is not DungeonBaseFragment baseDun)
        {
            return false;
        }

        if (DungeonStatsFilter.IsTierUnknown == true && baseDun.Tier < 0)
        {
            return true;
        }

        if (DungeonStatsFilter.TierFilters.Contains(baseDun.Tier))
        {
            return true;
        }

        if (DungeonStatsFilter.IsTierUnknown == false && DungeonStatsFilter.TierFilters.Count <= 0)
        {
            return true;
        }

        return false;
    }

    private bool IsLevelOkay(object obj)
    {
        if (obj is not RandomDungeonFragment ranDun)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevelUnknown == true && ranDun.Level < 0)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevel0 == true && ranDun.Level == 0)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevel1 == true && ranDun.Level == 1)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevel2 == true && ranDun.Level == 2)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevel3 == true && ranDun.Level == 3)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevel4 == true && ranDun.Level == 4)
        {
            return true;
        }

        if (DungeonStatsFilter.IsLevelUnknown == false
            && DungeonStatsFilter.IsLevel0 == false
            && DungeonStatsFilter.IsLevel1 == false
            && DungeonStatsFilter.IsLevel2 == false
            && DungeonStatsFilter.IsLevel3 == false
            && DungeonStatsFilter.IsLevel4 == false)
        {
            return true;
        }

        return false;
    }

    private bool IsTimestampOkay(object obj)
    {
        if (obj is not DungeonBaseFragment dungeon)
        {
            return false;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Total)
        {
            return true;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Last365Days && dungeon.EnterDungeonFirstTime > DateTime.UtcNow.AddDays(-365))
        {
            return true;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Last30Days && dungeon.EnterDungeonFirstTime > DateTime.UtcNow.AddDays(-30))
        {
            return true;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Last7Days && dungeon.EnterDungeonFirstTime > DateTime.UtcNow.AddDays(-7))
        {
            return true;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Today && dungeon.EnterDungeonFirstTime.Date == DateTime.UtcNow.Date)
        {
            return true;
        }

        if (SelectedStatsTimeType.StatTimeType == DungeonStatTimeType.Unknown)
        {
            return true;
        }

        return false;
    }

    private bool IsModeOkay(object obj)
    {
        if (obj is not DungeonBaseFragment dungeon)
        {
            return false;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Solo && dungeon.Mode == DungeonMode.Solo)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Standard && dungeon.Mode == DungeonMode.Standard)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Avalon && dungeon.Mode == DungeonMode.Avalon)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Corrupted && dungeon.Mode == DungeonMode.Corrupted)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.HellGate && dungeon.Mode == DungeonMode.HellGate)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Expedition && dungeon.Mode == DungeonMode.Expedition)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Mists && dungeon.Mode == DungeonMode.Mists)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.MistsDungeon && dungeon.Mode == DungeonMode.MistsDungeon)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.AbyssalDepths && dungeon.Mode == DungeonMode.AbyssalDepths)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Unknown)
        {
            return true;
        }

        return false;
    }

    private bool IsMapTypeOkay(object obj)
    {
        if (obj is not DungeonBaseFragment dungeon)
        {
            return false;
        }

        if (SelectedDungeonStatsType.StatsViewType is DungeonMode.Solo or DungeonMode.Standard or DungeonMode.Avalon && dungeon.MapType == MapType.RandomDungeon)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Corrupted && dungeon.MapType == MapType.CorruptedDungeon)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.HellGate && dungeon.MapType == MapType.HellGate)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Expedition && dungeon.MapType == MapType.Expedition)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Mists && dungeon.MapType == MapType.Mists)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.MistsDungeon && dungeon.MapType == MapType.MistsDungeon)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.AbyssalDepths && dungeon.MapType == MapType.AbyssalDepths)
        {
            return true;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Unknown || dungeon.MapType == MapType.Unknown)
        {
            return true;
        }

        return false;
    }

    #endregion

    private void UpdateStatsView()
    {
        SetAllStatViewsToCollapsed();

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Unknown)
        {
            Stats.StatsTotal.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Solo)
        {
            Stats.StatsSolo.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Standard)
        {
            Stats.StatsStandard.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Avalon)
        {
            Stats.StatsAvalonian.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Corrupted)
        {
            Stats.StatsCorrupted.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.HellGate)
        {
            Stats.StatsHellGate.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Expedition)
        {
            Stats.StatsExpedition.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.Mists)
        {
            Stats.StatsMists.Visibility = Visibility.Visible;
            return;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.MistsDungeon)
        {
            Stats.StatsMistsDungeon.Visibility = Visibility.Visible;
        }

        if (SelectedDungeonStatsType.StatsViewType == DungeonMode.AbyssalDepths)
        {
            Stats.StatsAbyssalDepths.Visibility = Visibility.Visible;
        }
    }

    private void SetAllStatViewsToCollapsed()
    {
        Stats.StatsTotal.Visibility = Visibility.Collapsed;
        Stats.StatsSolo.Visibility = Visibility.Collapsed;
        Stats.StatsStandard.Visibility = Visibility.Collapsed;
        Stats.StatsAvalonian.Visibility = Visibility.Collapsed;
        Stats.StatsExpedition.Visibility = Visibility.Collapsed;
        Stats.StatsCorrupted.Visibility = Visibility.Collapsed;
        Stats.StatsHellGate.Visibility = Visibility.Collapsed;
        Stats.StatsMists.Visibility = Visibility.Collapsed;
        Stats.StatsMistsDungeon.Visibility = Visibility.Collapsed;
        Stats.StatsAbyssalDepths.Visibility = Visibility.Collapsed;
    }
}