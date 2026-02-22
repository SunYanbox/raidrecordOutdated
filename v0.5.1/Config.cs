using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;

namespace raidrecord_v0._5._1;

public record ModConfigData
{
    // 本地语言
    [JsonPropertyName("local")]
    public required string Local { get; set; }
    // 模组日志位置
    [JsonPropertyName("logPath")]
    public required string LogPath { get; set; }
}

// [Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ModConfig(ModHelper modHelper, 
    ISptLogger<ModConfig> logger): IOnLoad
{
    public required ModConfigData Configs;
    public required StreamWriter? LogFile;
    private readonly Lock _logLock = new();
    
    
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // logger.Info($"pathToMod: {pathToMod}");
        Configs = modHelper.GetJsonDataFromFile<ModConfigData>(pathToMod, "config.json");
        var logPath = Path.Combine(pathToMod, Configs.LogPath);
        try
        {
            LogFile = new StreamWriter(logPath);
        }
        catch (Exception ex)
        {
            LogFile = null;
            logger.Error($"由于{ex.Message}, 无法获取模组日志流");
        }
        
        // logger.Info($"读取到的配置: {jsonUtil.Serialize(_configs)}");
        return Task.CompletedTask;
    }

    public void Log(string mode, string message)
    {
        if (LogFile == null)
        {
            
        }
        else
        {
            lock (_logLock)
            {
                using (var sw = new StreamWriter(LogFile.BaseStream, LogFile.Encoding, 1024, true))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mode} - {message}");
                    // using 语句结束时自动调用 Dispose 和 Flush
                }
            }
        }
    }

    public void LogError(Exception e, string where, string? message = null)
    {
        Log("Error", $"where: {where}\nmessage: {e.Message}\nstack: {e.StackTrace}\nLogMessage: {message}");
    }
}