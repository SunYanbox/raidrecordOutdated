using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using Path = System.IO.Path;
using System.Text.Json;

namespace raidrecord_v0._5._1.models;

// 主本地化数据类
public class LocalizationData
{
    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    [JsonPropertyName("armorZone")]
    public Dictionary<string, string> ArmorZone { get; set; } = new Dictionary<string, string>();
    [JsonPropertyName("roleNames")]
    public Dictionary<string, string> RoleNames { get; set; } = new Dictionary<string, string>();
}

// 第一时间初始化本地数据
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class LocalizationManager(
    ISptLogger<LocalizationManager> logger, 
    ModHelper modHelper, 
    ModConfig  modConfig,
    DatabaseServer databaseServer): IOnLoad
{
    public readonly Dictionary<string, string> MapNames = new Dictionary<string,string>();
    public readonly Dictionary<string, Dictionary<string, string>> ExitNames = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, LocalizationData> _allLocalizations = new Dictionary<string, LocalizationData>();
    private string _currentLanguage = "ch"; // 默认语言

    public Task OnLoad()
    {
        var localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // 使用跨平台路径组合，避免 Linux 下出现 db\locals 目录不存在
        localsDir = Path.Combine(localsDir, "db", "locals");
        if (Directory.Exists(localsDir))
        {
            // logger.Info($"本地化数据库存在: {localsDir}");
            foreach (var file in Directory.GetFiles(localsDir))
            {
                // eg: ch.json
                
                var fileName = Path.GetFileNameWithoutExtension(file);
                // logger.Info($"尝试加载{localsDir}文件夹下的{fileName}, file: {file}");
                if (file.EndsWith(".json") && fileName.Length == 2)
                {
                    // logger.Info($"> {fileName}成功加载");
                    _allLocalizations[fileName] = modHelper.GetJsonDataFromFile<LocalizationData>(localsDir, $"{fileName}.json");
                }
            }
            
            logger.Info(
                $"[RaidRecord] {GetTextFormat("models.local.LM.onload.info0", string.Join(", ", AvailableLanguages))}");
        }
        else
        {
            logger.Error($"[RaidRecord] 本地化数据库不存在: {localsDir}");
            logger.Error($"[RaidRecord] Localisation database does not exist: {localsDir}");
        }

        CurrentLanguage = modConfig.Configs.Local;
        
        var tables = databaseServer.GetTables();
        InitLocalization(tables.Locations, tables.Locales);
        return Task.CompletedTask;
    }

    protected void InitLocalization(Locations locations, LocaleBase locales)
    {
        // 所有的地图名称
        string[] mapNames = [
            "woods", 
            "bigmap", 
            // "develop",
            "factory4_day", // factory4_day
            "factory4_night", // factory4_night
            // "hideout",
            "interchange",
            "laboratory",
            "labyrinth",
            "lighthouse",
            // "privatearea",
            "rezervbase",
            "sandbox",
            "sandboxhigh",
            "shoreline",
            // "suburbs",
            "tarkovstreets",
            // "terminal",
            // "town"
        ];
        var localesMap = locales.Global[CurrentLanguage].Value;
        var errorMsg = "";

        // 获取地图的本地化表示
        foreach (var mapName in mapNames)
        {
            MapNames[mapName.Replace("_", "")] = localesMap?.TryGetValue(mapName, out var name) ?? false ? name : mapName;
        }
        
        foreach (var mapName in mapNames)
        {
            ExitNames.Add(mapName, new Dictionary<string, string>());
            var dictKey = mapName.Replace("_", "");
            if (!locations.GetDictionary().ContainsKey(dictKey)) continue;

            var map = locations.GetDictionary()[dictKey];
            var mapType = map.GetType();
            // 兼容不同版本属性：AllExtracts / Exits / AllExits
            System.Collections.IEnumerable? exitsEnum =
                mapType.GetProperty("AllExtracts")?.GetValue(map) as System.Collections.IEnumerable ??
                mapType.GetProperty("Exits")?.GetValue(map) as System.Collections.IEnumerable ??
                mapType.GetProperty("AllExits")?.GetValue(map) as System.Collections.IEnumerable;

            if (exitsEnum == null)
            {
                errorMsg += GetTextFormat("models.local.LM.init.warn2", mapName) + "\n"; // 未找到任何出口属性
                continue;
            }

            foreach (var exitObj in exitsEnum)
            {
                if (exitObj == null) continue;
                var exitType = exitObj.GetType();
                var nameProp = exitType.GetProperty("Name");
                var exitName = nameProp?.GetValue(exitObj) as string;
                if (string.IsNullOrEmpty(exitName)) continue;

                if (!localesMap.ContainsKey(exitName))
                {
                    errorMsg += GetTextFormat("models.local.LM.init.warn0", exitName) + "\n";
                    continue;
                }
                if (ExitNames[mapName].ContainsKey(exitName))
                {
                    errorMsg += GetTextFormat("models.local.LM.init.warn1", exitName, mapName) + "\n";
                    continue;
                }
                ExitNames[mapName].Add(exitName, localesMap[exitName]);
            }
        }
        
        // var options = new JsonSerializerOptions
        // {
        //     Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //     WriteIndented = true
        // };
        // Console.WriteLine(JsonSerializer.Serialize(ExitNames, options));
        // Console.WriteLine("[RaidRecord] " + errorMsg);
        modConfig.Log("Info", errorMsg);
        Console.WriteLine($"[RaidRecord] {GetTextFormat("models.local.LM.init.info0")}");
    }
    
    public string CurrentLanguage 
    { 
        get => _currentLanguage;
        set
        {
            if (_allLocalizations.ContainsKey(value))
                _currentLanguage = value;
        }
    }
    
    // 优先key, 备用en(ch百分百提供), 最终默认值: 未定义
    public string GetText(string key, string fallback = "未定义")
    {
        _allLocalizations.TryGetValue(CurrentLanguage, out var localization);
        if (localization == null) { return fallback; }
        localization.Translations.TryGetValue(key, out var result);
        if (result == null)
        {
            localization.Translations.TryGetValue("en", out result);
        }
        return result ?? fallback;
    }

    public string GetTextFormat(string msgId, params object?[] args)
    {
        return string.Format(GetText(msgId), args);
    }

    public string GetMapName(string map)
    {
        return MapNames.TryGetValue(map.Replace("_", "").ToLower(), out var mapExits) ? mapExits: map;
    }
    
    public string GetExitName(string map, string key)
    {
        return ExitNames.TryGetValue(map.Replace("_", "").ToLower(), out var mapExits) ? (mapExits.TryGetValue(key, out var name) ? name : key) : key;
    }
    
    public string GetArmorZoneName(string key) => _allLocalizations[CurrentLanguage].ArmorZone.GetValueOrDefault(key, key);

    public string GetRoleName(string key) => _allLocalizations[CurrentLanguage].RoleNames.GetValueOrDefault(key, key);

    // 只读属性, 查看支持的语言
    public List<string> AvailableLanguages => _allLocalizations.Keys.ToList<string>();
}