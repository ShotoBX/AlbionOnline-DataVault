using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.Properties;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace StatisticsAnalysisTool.Guild;

public class GuildController
{
    private readonly TrackingController _trackingController;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private int _currentTabId;

    public GuildController(TrackingController trackingController, MainWindowViewModel mainWindowViewModel)
    {
        _trackingController = trackingController;
        _mainWindowViewModel = mainWindowViewModel;
    }

    public void SetTabId(int id)
    {
        _currentTabId = id;
    }

    public void AddSiphonedEnergyEntry(string username, FixPoint quantity, long timestamp, bool isManualEntry = false)
    {
        AddSiphonedEnergyEntries(new List<string> { username }, new List<FixPoint> { quantity }, new List<long> { timestamp }, isManualEntry);
    }

    public void AddSiphonedEnergyEntries(List<string> usernames, List<FixPoint> quantities, List<long> timestamps, bool isManualEntry = false)
    {
        if (_mainWindowViewModel.TrackingActivityBindings.TrackingActivityType != TrackingIconType.On)
        {
            return;
        }

        // Siphoned Energy tab is 2
        if ((_currentTabId == 2 && (usernames.Count == quantities.Count && usernames.Count == timestamps.Count)) || isManualEntry)
        {
            var tempList = new List<SiphonedEnergyItem>();

            for (int i = 0; i < quantities.Count; i++)
            {
                string username = usernames[i];
                FixPoint quantity = quantities[i];
                DateTime timestamp = new DateTime(timestamps[i]);

                var siphonedEnergyEntry = new SiphonedEnergyItem()
                {
                    GuildName = _trackingController.EntityController.LocalUserData.GuildName,
                    CharacterName = username,
                    Quantity = quantity,
                    Timestamp = timestamp
                };

                if (!_mainWindowViewModel.GuildBindings.SiphonedEnergyList.Any(x =>
                        x.CharacterName == siphonedEnergyEntry.CharacterName
                        && x.Quantity.InternalValue == siphonedEnergyEntry.Quantity.InternalValue
                        && x.Timestamp == siphonedEnergyEntry.Timestamp))
                {
                    tempList.Add(siphonedEnergyEntry);
                }
            }

            foreach (var item in tempList)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _mainWindowViewModel.GuildBindings.SiphonedEnergyList.Add(item);
                    UpdateSiphonedEnergyOverview();
                });
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _mainWindowViewModel.GuildBindings.SiphonedEnergyLastUpdate = DateTime.UtcNow;
                _mainWindowViewModel.GuildBindings.SiphonedEnergyLastUpdateVisibility
                    = _mainWindowViewModel.GuildBindings?.SiphonedEnergyLastUpdate.Ticks <= 1 ? Visibility.Hidden : Visibility.Visible;
            });
        }
    }

    public void UpdateSiphonedEnergyOverview()
    {
        var siphonedEnergies = _mainWindowViewModel.GuildBindings.SiphonedEnergyList.ToList();

        var grouped = siphonedEnergies
            .Where(x => x.IsDisabled == false)
            .GroupBy(x => x.CharacterName)
            .Select(item => new SiphonedEnergyItem()
            {
                CharacterName = item.Key,
                Quantity = FixPoint.FromInternalValue(item.Sum(x => x.Quantity.InternalValue)),
                Timestamp = item.Max(x => x.Timestamp)
            })
            .OrderByDescending(x => x.Quantity.IntegerValue)
            .ToList();

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _mainWindowViewModel.GuildBindings.SiphonedEnergyOverviewList = new ObservableRangeCollection<SiphonedEnergyItem>(grouped);
            _mainWindowViewModel.GuildBindings.TotalSiphonedEnergyQuantity = grouped.Sum(x => x.Quantity.IntegerValue);
        });
    }

    #region Guild Overview

    /// <summary>
    /// Fetches the local player's own guild from Albion's public gameinfo API (roster + kill/death fame) - unlike
    /// Battle Report and Siphoned Energy, this doesn't depend on locally-observed packets, so it's available even
    /// when not currently in combat. Resolves the guild name (from locally observed player data) to a guild id via
    /// the same search endpoint already used for player search, then loads guild info + members in parallel.
    /// </summary>
    public async Task LoadGuildOverviewAsync()
    {
        var guildBindings = _mainWindowViewModel.GuildBindings;

        if (guildBindings.IsLoadingGuildOverview)
        {
            return;
        }

        var guildName = _trackingController.EntityController.LocalUserData.GuildName;

        if (string.IsNullOrWhiteSpace(guildName))
        {
            guildBindings.GuildInfo = null;
            guildBindings.GuildMembers = [];
            guildBindings.GuildOverviewStatusText = LocalizationController.Translation("GUILD_OVERVIEW_EMPTY");
            return;
        }

        guildBindings.IsLoadingGuildOverview = true;

        try
        {
            var searchResponse = await ApiController.GetGameInfoSearchFromJsonAsync(guildName);
            var guildId = searchResponse?.SearchGuilds?.FirstOrDefault(x => string.Equals(x.Name, guildName, StringComparison.OrdinalIgnoreCase))?.Id;

            if (string.IsNullOrWhiteSpace(guildId))
            {
                guildBindings.GuildInfo = null;
                guildBindings.GuildMembers = [];
                guildBindings.GuildOverviewStatusText = LocalizationController.Translation("GUILD_OVERVIEW_ERROR");
                return;
            }

            var guildInfoTask = ApiController.GetGameInfoGuildFromJsonAsync(guildId);
            var membersTask = ApiController.GetGameInfoGuildMembersFromJsonAsync(guildId);
            await Task.WhenAll(guildInfoTask, membersTask);

            var members = (membersTask.Result ?? []).OrderByDescending(x => x.KillFame ?? 0).ToList();

            guildBindings.GuildInfo = guildInfoTask.Result;
            guildBindings.GuildMembers = new ObservableCollection<SearchPlayerResponse>(members);
            guildBindings.GuildOverviewStatusText = string.Empty;
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            guildBindings.GuildOverviewStatusText = LocalizationController.Translation("GUILD_OVERVIEW_ERROR");
        }
        finally
        {
            guildBindings.IsLoadingGuildOverview = false;
        }
    }

    #endregion

    #region Guild Loot Check

    /// <summary>
    /// Recomputes the guild-scoped loot verification summary from LoggingBindings.LootingPlayers - the same data
    /// already produced by the Registro tab's Loot Log Checker (LoggingBindings.CompareLootLogsAsync). This is a
    /// read-only projection, not a second comparator, so the two views can never drift out of sync.
    /// </summary>
    public void RefreshLootCheckSummary()
    {
        _mainWindowViewModel.GuildBindings.RefreshLootCheckSummary(_mainWindowViewModel.LoggingBindings.LootingPlayers);
    }

    #endregion

    public async Task RemoveTradesByIdsAsync(IEnumerable<int> hashCodes)
    {
        await Task.Run(async () =>
        {
            var itemToRemove = _mainWindowViewModel?.GuildBindings?.SiphonedEnergyList?.ToList().Where(x => hashCodes.Contains(x.GetHashCode())).ToList();
            var newList = _mainWindowViewModel?.GuildBindings?.SiphonedEnergyList?.ToList();

            if (itemToRemove != null && itemToRemove.Any())
            {
                foreach (var item in itemToRemove)
                {
                    newList?.Remove(item);
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateSiphonedEnergyList(newList);
            });
        });
    }

    private void UpdateSiphonedEnergyList(IEnumerable<SiphonedEnergyItem> updatedList)
    {
        var guildBindings = _mainWindowViewModel.GuildBindings;
        guildBindings.SiphonedEnergyList.Clear();
        guildBindings.SiphonedEnergyList.AddRange(updatedList);
        guildBindings.SiphonedEnergyCollectionView = CollectionViewSource.GetDefaultView(guildBindings.SiphonedEnergyList) as ListCollectionView;

        UpdateSiphonedEnergyOverview();
    }

    public string GetSiphonedEnergyListAsCsv()
    {
        try
        {
            const string csvHeader = "timestamp_utc;guild_name;character_name;quantity\n";

            var siphonedEnergyList = _mainWindowViewModel.GuildBindings.SiphonedEnergyList;

            var csvRows = siphonedEnergyList
                .Where(x => !x.IsDisabled && !x.IsSelectedForDeletion)
                .Select(item =>
                    $"{item.Timestamp.ToUniversalTime():yyyy-MM-dd HH:mm:ss};" +
                    $"{item.GuildName};" +
                    $"{item.CharacterName};" +
                    $"{item.Quantity.IntegerValue}"
                );

            return csvHeader + string.Join(Environment.NewLine, csvRows);
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            return string.Empty;
        }
    }

    #region Save / Load data

    public async Task LoadFromFileAsync()
    {
        var dto = await FileController.LoadAsync<GuildDto>(
            AppDataPaths.UserDataFile(Settings.Default.GuildFileName));
        var guild = GuildMapping.Mapping(dto);

        _mainWindowViewModel.GuildBindings.SiphonedEnergyList.Clear();
        _mainWindowViewModel.GuildBindings.SiphonedEnergyList.AddRange(guild.SiphonedEnergies);
        UpdateSiphonedEnergyOverview();
    }

    public async Task SaveInFileAsync()
    {
        if (!AppDataPaths.TryEnsureUserDataDirectory())
        {
            return;
        }

        await FileController.SaveAsync(new GuildDto()
        {
            SiphonedEnergies = _mainWindowViewModel.GuildBindings.SiphonedEnergyList.Select(GuildMapping.Mapping).ToList()
        },
            AppDataPaths.UserDataFile(Settings.Default.GuildFileName));
        Log.Information("Guild data saved");
    }

    #endregion
}