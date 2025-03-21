﻿using System.Diagnostics;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.Sheets;
using SamplePlugin.Windows;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    
    private const string CommandHelp = "/clogUI";
    private const string CommandName = "/checklogs";


    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FFXIV Logs Check");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName,
                                  new CommandInfo(OnCommand)
                                  {
                                      HelpMessage = "Check target FFXIV Logs"
                                  });

        CommandManager.AddHandler(CommandHelp,
                                  new CommandInfo(OnCommandToggleUi)
                                  {
                                      HelpMessage = "Enable Main UI"
                                  });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        /*PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;*/

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }
   

    public void OnCommand(string command, string args)
    {
       CheckFFLogs();
    }
    public void OnCommandToggleUi(string command, string args)
    {
        MainWindow.Toggle();
    }
    
    public void CheckFFLogs()
    {
        unsafe
        {
            var targetObject = TargetSystem.Instance()->GetTargetObject();
            
            if (targetObject == null|| targetObject->ObjectKind != FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Pc) return;
            
            var playerCharacter = (Character*)targetObject;
            var homeWorldID = playerCharacter->HomeWorld;
            
            var homeWorldName = DataManager.GetExcelSheet<World>().GetRow(homeWorldID).Name.ExtractText();

            var dataCenterName = DataManager.GetExcelSheet<World>().GetRow(homeWorldID).DataCenter.Value.Name.ExtractText();
            
            string regionName = string.Empty;

            if(dataCenterName == "Aether" ||dataCenterName == "Crystal" ||dataCenterName == "Primal" ||dataCenterName == "Dynamis")
            {
                regionName = "NA";
            }
            else if (dataCenterName == "Chaos" || dataCenterName == "Light")
            {
                regionName = "EU";
            }
            else if (dataCenterName == "Elemental" || dataCenterName == "Gaia" || dataCenterName == "Mana" || dataCenterName == "Meteor")
            {
                regionName = "JP";
            }
            else if (dataCenterName == "Materia")
            {
                regionName = "OC";
            }
            
            string url = "https://www.fflogs.com/character/"+ regionName +"/" + homeWorldName +  "/" + targetObject->NameString;
        
            Process.Start(new ProcessStartInfo()
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
    
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
