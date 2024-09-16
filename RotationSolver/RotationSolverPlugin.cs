using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using Lumina.Excel.GeneratedSheets;
using RotationSolver.Basic.Configuration;
using RotationSolver.Basic.IPC;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.Helpers;

using RotationSolver.UI;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using WelcomeWindow = RotationSolver.UI.WelcomeWindow;

namespace RotationSolver;

public sealed class RotationSolverPlugin : IDalamudPlugin, IDisposable
{
    private readonly WindowSystem windowSystem;

    static RotationConfigWindow? _rotationConfigWindow;
    static ControlWindow? _controlWindow;
    static NextActionWindow? _nextActionWindow;
    static CooldownWindow? _cooldownWindow;
    static WelcomeWindow? _changelogWindow;
    static OverlayWindow? _overlayWindow;

    static readonly List<IDisposable> _dis = [];
    public static string Name => "Rotation Solver Reborn";
    internal static readonly List<DrawingHighlightHotbarBase> _drawingElements = [];

    public static DalamudLinkPayload OpenLinkPayload { get; private set; } = null!;
    public static DalamudLinkPayload? HideWarningLinkPayload { get; private set; }

    internal IPCProvider IPCProvider;
    public RotationSolverPlugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
        ThreadLoadImageHandler.TryGetIconTextureWrap(0, true, out _);
        IconSet.InIt();

        _dis.Add(new Service());
        try
        {
            // Check if the config file exists before attempting to read and deserialize it
            if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
            {
                var oldConfigs = JsonConvert.DeserializeObject<Configs>(
                    File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName))
                    ?? new Configs();

                // Check version and migrate or reset if necessary
                var newConfigs = Configs.Migrate(oldConfigs);
                if (newConfigs.Version != Configs.CurrentVersion)
                {
                    newConfigs = new Configs(); // Reset to default if versions do not match
                }
                Service.Config = newConfigs;
            }
            else
            {
                Service.Config = new Configs();
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Failed to load config");
            Service.Config = new Configs();
        }

        IPCProvider = new();

        _rotationConfigWindow = new();
        _controlWindow = new();
        _nextActionWindow = new();
        _cooldownWindow = new();
        _changelogWindow = new();
        _overlayWindow = new();

        windowSystem = new WindowSystem(Name);
        windowSystem.AddWindow(_rotationConfigWindow);
        windowSystem.AddWindow(_controlWindow);
        windowSystem.AddWindow(_nextActionWindow);
        windowSystem.AddWindow(_cooldownWindow);
        windowSystem.AddWindow(_changelogWindow);
        windowSystem.AddWindow(_overlayWindow);
        Notify.Success("Overlay Window was added!");

        Svc.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.Draw += OnDraw;

        //HotbarHighlightDrawerManager.Init();
        HotbarHighlightManager.Init();

        MajorUpdater.Enable();
        Watcher.Enable();
        OtherConfiguration.Init();

        Svc.DutyState.DutyStarted += DutyState_DutyStarted;
        Svc.DutyState.DutyWiped += DutyState_DutyWiped;
        Svc.DutyState.DutyCompleted += DutyState_DutyCompleted;
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);


        static async void DutyState_DutyCompleted(object? sender, ushort e)
        {
            await Task.Delay(new Random().Next(4000, 6000));

            Service.Config.DutyEnd.AddMacro();

            if (Service.Config.AutoOffWhenDutyCompleted)
            {
                RSCommands.CancelState();
            }
        }

        static void ClientState_TerritoryChanged(ushort id)
        {
            DataCenter.ResetAllRecords();

            var territory = Service.GetSheet<TerritoryType>().GetRow(id);

            DataCenter.Territory = territory;

            try
            {
                DataCenter.RightNowRotation?.OnTerritoryChanged();
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Failed on Territory changed.");
            }
        }

        static void DutyState_DutyStarted(object? sender, ushort e)
        {
            if (!Player.Available) return;
            if (!Player.Object.IsJobCategory(JobRole.Tank) && !Player.Object.IsJobCategory(JobRole.Healer)) return;

            if (DataCenter.IsInHighEndDuty)
            {
                var warning = string.Format(UiString.HighEndWarning.GetDescription(), DataCenter.ContentFinderName);
#pragma warning disable CS0436
                WarningHelper.AddSystemWarning(warning);
            }
        }

        static void DutyState_DutyWiped(object? sender, ushort e)
        {
            if (!Player.Available) return;
            DataCenter.ResetAllRecords();
        }

        ChangeUITranslation();

        OpenLinkPayload = pluginInterface.AddChatLinkHandler(0, (id, str) =>
        {
            if (id == 0) OpenConfigWindow();
        });
        HideWarningLinkPayload = pluginInterface.AddChatLinkHandler(1, (id, str) =>
        {
            if (id == 1)
            {
                Service.Config.HideWarning.Value = true;
                Svc.Chat.Print("Warning has been hidden.");
            }
        });
        Task.Run(async () =>
        {
            await DownloadHelper.DownloadAsync();
            if (Service.Config.AutoLoadRotations) await RotationUpdater.GetAllCustomRotationsAsync(DownloadOption.Download);
        });
    }

    private void OnDraw()
    {
        if (Svc.GameGui.GameUiHidden) return;
        windowSystem.Draw();
    }

    internal static void ChangeUITranslation()
    {
        _rotationConfigWindow!.WindowName = UiString.ConfigWindowHeader.GetDescription()
            + typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?";

        RSCommands.Disable();
        RSCommands.Enable();
    }

    public async void Dispose()
    {
        RSCommands.Disable();
        Watcher.Disable();

        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Svc.PluginInterface.UiBuilder.Draw -= OnDraw;

        foreach (var item in _dis)
        {
            item.Dispose();
        }
        _dis?.Clear();

        MajorUpdater.Dispose();
        //HotbarHighlightDrawerManager.Dispose();
        HotbarHighlightManager.Dispose();
        await OtherConfiguration.Save();

        ECommonsMain.Dispose();

        Service.Config.Save();
    }

    private void OnOpenConfigUi()
    {
        OpenConfigWindow();
    }

    internal static void OpenConfigWindow()
    {
        _rotationConfigWindow?.Toggle();
    }

    static RandomDelay validDelay = new(() => (0.2f, 0.2f));

    internal static void UpdateDisplayWindow()
    {
        var isValid = validDelay.Delay(MajorUpdater.IsValid
            && DataCenter.RightNowRotation != null
            && !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
            && !Svc.Condition[ConditionFlag.Occupied38] //Treasure hunt.
            && !Svc.Condition[ConditionFlag.WaitingForDuty]
            && (!Svc.Condition[ConditionFlag.UsingParasol] || Player.Object.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.WeaponOut))
            && !Svc.Condition[ConditionFlag.OccupiedInQuestEvent]);

        _nextActionWindow!.IsOpen = isValid && Service.Config.ShowNextActionWindow;

        isValid &= !Service.Config.OnlyShowWithHostileOrInDuty
                || Svc.Condition[ConditionFlag.BoundByDuty]
                || DataCenter.AllHostileTargets.Any(o => o.DistanceToPlayer() <= 25);

        _controlWindow!.IsOpen = isValid && Service.Config.ShowControlWindow;
        _cooldownWindow!.IsOpen = isValid && Service.Config.ShowCooldownWindow;
        _overlayWindow!.IsOpen = isValid && Service.Config.TeachingMode;
    }
}
