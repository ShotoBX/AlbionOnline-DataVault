using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.DamageMeter;
using StatisticsAnalysisTool.EventLogging;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace StatisticsAnalysisTool.Guild;

public class GuildBindings : BaseViewModel
{
    public ListCollectionView SiphonedEnergyCollectionView { get; set; }
    private ManuallySiphonedEnergy _manuallySiphonedEnergy;
    private GridLength _gridSplitterPosition;
    private ObservableRangeCollection<SiphonedEnergyItem> _siphonedEnergyList = new();
    private ObservableRangeCollection<SiphonedEnergyItem> _siphonedEnergyOverviewList = new();
    private Visibility _guildPopupVisibility = Visibility.Collapsed;
    private bool _isDeleteEntriesButtonEnabled = true;
    private DateTime _siphonedEnergyLastUpdate;
    private Visibility _siphonedEnergyLastUpdateVisibility = Visibility.Collapsed;
    private long _totalSiphonedEnergyQuantity;

    private ObservableCollection<DamageMeterFragment> _battleReportMembers = [];
    private DamageMeterFragment _battleReportMvp;
    private long _battleReportTotalDamage;
    private long _battleReportTotalHealing;
    private ObservableCollection<ISeries> _seriesBattleReportContribution = [];
    private Axis[] _xAxesBattleReportContribution = [new Axis { LabelsPaint = AxisLabelPaint, SeparatorsPaint = AxisSeparatorPaint }];
    private Axis[] _yAxesBattleReportContribution = [new Axis { LabelsPaint = AxisLabelPaint, SeparatorsPaint = AxisSeparatorPaint }];


    public GuildBindings()
    {
        SiphonedEnergyCollectionView = CollectionViewSource.GetDefaultView(SiphonedEnergyList) as ListCollectionView;

        if (SiphonedEnergyCollectionView != null)
        {
            SiphonedEnergyCollectionView.IsLiveSorting = true;
            SiphonedEnergyCollectionView.IsLiveFiltering = true;
            SiphonedEnergyCollectionView.CustomSort = new SiphonedEnergyItemComparer();

            SiphonedEnergyCollectionView?.Refresh();
        }

        ManuallySiphonedEnergy = new ManuallySiphonedEnergy();
    }

    public ObservableRangeCollection<SiphonedEnergyItem> SiphonedEnergyList
    {
        get => _siphonedEnergyList;
        set
        {
            _siphonedEnergyList = value;
            OnPropertyChanged();
        }
    }

    public ObservableRangeCollection<SiphonedEnergyItem> SiphonedEnergyOverviewList
    {
        get => _siphonedEnergyOverviewList;
        set
        {
            _siphonedEnergyOverviewList = value;
            OnPropertyChanged();
        }
    }

    public GridLength GridSplitterPosition
    {
        get => _gridSplitterPosition;
        set
        {
            _gridSplitterPosition = value;
            SettingsController.CurrentSettings.GuildGridSplitterPosition = _gridSplitterPosition.Value;
            OnPropertyChanged();
        }
    }

    public ManuallySiphonedEnergy ManuallySiphonedEnergy
    {
        get => _manuallySiphonedEnergy;
        set
        {
            _manuallySiphonedEnergy = value;
            OnPropertyChanged();
        }
    }

    public long TotalSiphonedEnergyQuantity
    {
        get => _totalSiphonedEnergyQuantity;
        set
        {
            _totalSiphonedEnergyQuantity = value;
            OnPropertyChanged();
        }
    }

    public Visibility GuildPopupVisibility
    {
        get => _guildPopupVisibility;
        set
        {
            _guildPopupVisibility = value;
            OnPropertyChanged();
        }
    }

    public bool IsDeleteEntriesButtonEnabled
    {
        get => _isDeleteEntriesButtonEnabled;
        set
        {
            _isDeleteEntriesButtonEnabled = value;
            OnPropertyChanged();
        }
    }

    public DateTime SiphonedEnergyLastUpdate
    {
        get => _siphonedEnergyLastUpdate;
        set
        {
            _siphonedEnergyLastUpdate = value;
            OnPropertyChanged();
        }
    }

    public Visibility SiphonedEnergyLastUpdateVisibility
    {
        get => _siphonedEnergyLastUpdateVisibility;
        set
        {
            _siphonedEnergyLastUpdateVisibility = value;
            OnPropertyChanged();
        }
    }

    public string TranslationSiphonedEnergy => LocalizationController.Translation("SIPHONED_ENERGY");
    public string TranslationDeleteSelectedEntries => LocalizationController.Translation("DELETE_SELECTED_ENTRIES");
    public string TranslationSelectDeselectAll => LocalizationController.Translation("SELECT_DESELECT_ALL");
    public string TranslationLastUpdate => LocalizationController.Translation("LAST_UPDATE");
    public string TranslationTotal => LocalizationController.Translation("TOTAL");

    #region Battle Report

    private static SolidColorPaint AxisLabelPaint => new(new SKColor(0xA8, 0x99, 0x79));
    private static SolidColorPaint AxisSeparatorPaint => new(new SKColor(0x24, 0x24, 0x28));

    private static readonly SKColor[] ChartPalette =
    [
        new SKColor(0x00, 0xB8, 0xFF), new SKColor(0x3D, 0xDC, 0x84), new SKColor(0xFF, 0xB7, 0x4D),
        new SKColor(0xFF, 0x5C, 0x5C), new SKColor(0xB8, 0x84, 0xFF), new SKColor(0x4D, 0xE0, 0xE0)
    ];

    public ObservableCollection<DamageMeterFragment> BattleReportMembers
    {
        get => _battleReportMembers;
        set
        {
            _battleReportMembers = value;
            OnPropertyChanged();
        }
    }

    public DamageMeterFragment BattleReportMvp
    {
        get => _battleReportMvp;
        set
        {
            _battleReportMvp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BattleReportMvpVisibility));
        }
    }

    public Visibility BattleReportMvpVisibility => BattleReportMvp == null ? Visibility.Collapsed : Visibility.Visible;

    public long BattleReportTotalDamage
    {
        get => _battleReportTotalDamage;
        set
        {
            _battleReportTotalDamage = value;
            OnPropertyChanged();
        }
    }

    public long BattleReportTotalHealing
    {
        get => _battleReportTotalHealing;
        set
        {
            _battleReportTotalHealing = value;
            OnPropertyChanged();
        }
    }

    public Visibility BattleReportEmptyStateVisibility => BattleReportMembers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public ObservableCollection<ISeries> SeriesBattleReportContribution
    {
        get => _seriesBattleReportContribution;
        set
        {
            _seriesBattleReportContribution = value;
            OnPropertyChanged();
        }
    }

    public Axis[] XAxesBattleReportContribution
    {
        get => _xAxesBattleReportContribution;
        set
        {
            _xAxesBattleReportContribution = value;
            OnPropertyChanged();
        }
    }

    public Axis[] YAxesBattleReportContribution
    {
        get => _yAxesBattleReportContribution;
        set
        {
            _yAxesBattleReportContribution = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Filters the already-tracked DamageMeter (party-scoped - the app only tracks combat for
    /// party members, see EntityController.GetAllEntitiesWithDamageOrHealAndInParty) down to the
    /// subset that also shares the local player's guild, and re-derives the summary/MVP/chart from
    /// that filtered set. No new tracking - every value here is already computed by CombatController.
    /// </summary>
    public void RebuildBattleReport(IEnumerable<DamageMeterFragment> damageMeter, string localGuildName)
    {
        var members = string.IsNullOrWhiteSpace(localGuildName)
            ? []
            : (damageMeter ?? []).Where(x => x.Guild == localGuildName && (x.Damage > 0 || x.Heal > 0)).ToList();

        BattleReportMembers = new ObservableCollection<DamageMeterFragment>(members);
        BattleReportTotalDamage = members.Sum(x => x.Damage);
        BattleReportTotalHealing = members.Sum(x => x.Heal);
        BattleReportMvp = members.OrderByDescending(x => x.Damage).FirstOrDefault();

        var topContributors = members.OrderByDescending(x => x.Damage).Take(5).ToList();

        SeriesBattleReportContribution =
        [
            new ColumnSeries<long>
            {
                Name = LocalizationController.Translation("TOTAL_DAMAGE"),
                Values = topContributors.Select(x => x.Damage).ToList(),
                Fill = new SolidColorPaint(ChartPalette[0])
            },
            new ColumnSeries<long>
            {
                Name = LocalizationController.Translation("TOTAL_HEALING"),
                Values = topContributors.Select(x => x.Heal).ToList(),
                Fill = new SolidColorPaint(ChartPalette[1])
            }
        ];

        XAxesBattleReportContribution =
        [
            new Axis
            {
                Labels = topContributors.Select(x => x.Name).ToArray(),
                LabelsRotation = 15,
                LabelsPaint = AxisLabelPaint,
                SeparatorsPaint = AxisSeparatorPaint
            }
        ];

        YAxesBattleReportContribution =
        [
            new Axis
            {
                LabelsPaint = AxisLabelPaint,
                SeparatorsPaint = AxisSeparatorPaint
            }
        ];

        OnPropertyChanged(nameof(BattleReportEmptyStateVisibility));
    }

    public string TranslationBattleReport => LocalizationController.Translation("BATTLE_REPORT");
    public string TranslationBattleReportDescription => LocalizationController.Translation("BATTLE_REPORT_DESCRIPTION");
    public string TranslationBattleReportMvp => LocalizationController.Translation("BATTLE_REPORT_MVP");
    public string TranslationBattleReportMembers => LocalizationController.Translation("BATTLE_REPORT_MEMBERS");
    public string TranslationBattleReportEmpty => LocalizationController.Translation("BATTLE_REPORT_EMPTY");
    public string TranslationBattleReportTopContributors => LocalizationController.Translation("BATTLE_REPORT_TOP_CONTRIBUTORS");

    #endregion

    #region Guild Overview

    private GameInfoGuildsResponse _guildInfo;
    private ObservableCollection<SearchPlayerResponse> _guildMembers = [];
    private bool _isLoadingGuildOverview;
    private string _guildOverviewStatusText = string.Empty;

    public GameInfoGuildsResponse GuildInfo
    {
        get => _guildInfo;
        set
        {
            _guildInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GuildOverviewEmptyVisibility));
            OnPropertyChanged(nameof(GuildOverviewContentVisibility));
        }
    }

    public ObservableCollection<SearchPlayerResponse> GuildMembers
    {
        get => _guildMembers;
        set
        {
            _guildMembers = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoadingGuildOverview
    {
        get => _isLoadingGuildOverview;
        set
        {
            _isLoadingGuildOverview = value;
            OnPropertyChanged();
        }
    }

    public string GuildOverviewStatusText
    {
        get => _guildOverviewStatusText;
        set
        {
            _guildOverviewStatusText = value;
            OnPropertyChanged();
        }
    }

    public Visibility GuildOverviewEmptyVisibility => GuildInfo == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GuildOverviewContentVisibility => GuildInfo == null ? Visibility.Collapsed : Visibility.Visible;

    public string TranslationGuildOverview => LocalizationController.Translation("GUILD_OVERVIEW");
    public string TranslationGuildOverviewEmpty => LocalizationController.Translation("GUILD_OVERVIEW_EMPTY");
    public string TranslationGuildOverviewRefresh => LocalizationController.Translation("GUILD_OVERVIEW_REFRESH");
    public string TranslationGuildOverviewMembers => LocalizationController.Translation("GUILD_OVERVIEW_MEMBERS");
    public string TranslationGuildOverviewFounded => LocalizationController.Translation("GUILD_OVERVIEW_FOUNDED");
    public string TranslationGuildOverviewFounder => LocalizationController.Translation("GUILD_OVERVIEW_FOUNDER");
    public string TranslationGuildOverviewAlliance => LocalizationController.Translation("GUILD_OVERVIEW_ALLIANCE");
    public string TranslationGuildOverviewKillFame => LocalizationController.Translation("GUILD_OVERVIEW_KILL_FAME");
    public string TranslationGuildOverviewDeathFame => LocalizationController.Translation("GUILD_OVERVIEW_DEATH_FAME");
    public string TranslationGuildOverviewRanking => LocalizationController.Translation("GUILD_OVERVIEW_RANKING");
    public string TranslationGuildOverviewError => LocalizationController.Translation("GUILD_OVERVIEW_ERROR");

    #endregion

    #region Guild Loot Check

    private int _lootCheckResolvedCount;
    private int _lootCheckLostCount;
    private int _lootCheckUnknownCount;
    private int _lootCheckDonatedCount;
    private bool _hasLootCheckData;

    public int LootCheckResolvedCount
    {
        get => _lootCheckResolvedCount;
        set
        {
            _lootCheckResolvedCount = value;
            OnPropertyChanged();
        }
    }

    public int LootCheckLostCount
    {
        get => _lootCheckLostCount;
        set
        {
            _lootCheckLostCount = value;
            OnPropertyChanged();
        }
    }

    public int LootCheckUnknownCount
    {
        get => _lootCheckUnknownCount;
        set
        {
            _lootCheckUnknownCount = value;
            OnPropertyChanged();
        }
    }

    public int LootCheckDonatedCount
    {
        get => _lootCheckDonatedCount;
        set
        {
            _lootCheckDonatedCount = value;
            OnPropertyChanged();
        }
    }

    public bool HasLootCheckData
    {
        get => _hasLootCheckData;
        set
        {
            _hasLootCheckData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LootCheckEmptyVisibility));
            OnPropertyChanged(nameof(LootCheckContentVisibility));
        }
    }

    public Visibility LootCheckEmptyVisibility => HasLootCheckData ? Visibility.Collapsed : Visibility.Visible;
    public Visibility LootCheckContentVisibility => HasLootCheckData ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Aggregates the already-computed loot comparison results from LoggingBindings (Registro tab's Loot Log
    /// Checker) into simple counts for a guild-scoped summary card. No comparison logic is duplicated here -
    /// this only reads LootedItem.Status values that CompareLootLogsAsync already produced.
    /// </summary>
    public void RefreshLootCheckSummary(IEnumerable<LootingPlayer> lootingPlayers)
    {
        var items = (lootingPlayers ?? []).SelectMany(player => player.GetLootedItemsSnapshot()).ToList();

        LootCheckResolvedCount = items.Count(x => x.Status == LootedItemStatus.Resolved);
        LootCheckLostCount = items.Count(x => x.Status == LootedItemStatus.Lost);
        LootCheckUnknownCount = items.Count(x => x.Status == LootedItemStatus.Unknown);
        LootCheckDonatedCount = items.Count(x => x.Status == LootedItemStatus.Donated);
        HasLootCheckData = items.Count > 0;
    }

    public string TranslationGuildLootCheck => LocalizationController.Translation("GUILD_LOOT_CHECK");
    public string TranslationGuildLootCheckResolved => LocalizationController.Translation("GUILD_LOOT_CHECK_RESOLVED");
    public string TranslationGuildLootCheckLost => LocalizationController.Translation("GUILD_LOOT_CHECK_LOST");
    public string TranslationGuildLootCheckUnknown => LocalizationController.Translation("GUILD_LOOT_CHECK_UNKNOWN");
    public string TranslationGuildLootCheckDonated => LocalizationController.Translation("GUILD_LOOT_CHECK_DONATED");
    public string TranslationGuildLootCheckEmpty => LocalizationController.Translation("GUILD_LOOT_CHECK_EMPTY");
    public string TranslationGuildLootCheckRefresh => LocalizationController.Translation("GUILD_LOOT_CHECK_REFRESH");
    public string TranslationGuildLootCheckHint => LocalizationController.Translation("GUILD_LOOT_CHECK_HINT");

    #endregion
}