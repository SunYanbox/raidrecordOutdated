using System.Reflection;
using System.Text.Json.Serialization;
using raidrecord_v0._5._1.models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace raidrecord_v0._5._1;

public class RaidDataWrapper
{
    // 缓存
    [JsonPropertyName("info")]
    public RaidInfo? Info { get; set; }
    // 存档
    [JsonPropertyName("archive")]
    public RaidArchive? Archive { get; set; }
    
    public bool IsInfo => Info != null;
    public bool IsArchive => Archive != null;

    public void Zip(ItemHelper itemHelper)
    {
        if (IsInfo)
        {
            Archive = new RaidArchive();
            Archive.Zip(Info, itemHelper);
            Info = null;
        }
    }
}

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 3)]
public class RecordCacheManager(
    ISptLogger<RecordCacheManager>  logger,
    LocalizationManager  localManager,
    JsonUtil jsonUtil,
    ModConfig modConfig,
    ModHelper modHelper
    ): IOnLoad
{
    private string? _RecordDbPath;
    private Dictionary<MongoId, List<RaidDataWrapper>> raidRecordCache = new Dictionary<MongoId, List<RaidDataWrapper>>();
    
    public void Info(string message) { logger.Info($"[RaidRecord] {message}"); }
    public void Error(string message) { logger.Error($"[RaidRecord] {message}"); }
     
    public Task OnLoad()
    {
        
        var localsDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        // 跨平台路径组合，避免 Linux 下反斜杠导致目录不存在
        _RecordDbPath = Path.Combine(localsDir, "db", "records");

        if (!Directory.Exists(_RecordDbPath)) Directory.CreateDirectory(_RecordDbPath);
        
        foreach (var file in Directory.GetFiles(_RecordDbPath))
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.EndsWith(".json")) continue;
            try
            {
                var data = jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(file);
                if (data == null) throw new Exception(string.Format("反序列化文件{0}时获取不到数据", file));
                raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), data);
            }
            catch (Exception e)
            {
                modConfig.LogError(e, "RaidRecordManager.OnLoad.foreach.try-catch", file);
                // 备份原文件为 .err，带序号避免重复
                string originalFilePath = Path.Combine(_RecordDbPath, fileName);
                string backupBaseName = Path.GetFileNameWithoutExtension(fileName) + ".json.err";
                string backupDir = _RecordDbPath; // 备份在同一目录下，也可指定其他路径
                string backupPath = Path.Combine(backupDir, backupBaseName);

                int counter = 0;
                while (File.Exists(backupPath))
                {
                    backupPath = Path.Combine(backupDir, $"{Path.GetFileNameWithoutExtension(fileName)}.json.err.{counter}");
                    counter++;
                }

                try
                {
                    File.Copy(originalFilePath, backupPath);
                    Info(string.Format("序列化记录时出现问题: {0}, 已备份损坏文件至: {1}", e.Message, backupPath));
                    // Console.WriteLine($"[RaidRecord] DEBUG: {e.StackTrace}");
                }
                catch (Exception copyEx)
                {
                    modConfig.LogError(e, "RaidRecordManager.OnLoad.foreach.try-catch.try-catch", file);
                    Error(string.Format("备份文件过程中发生错误: {0}", copyEx.Message));
                }
                //
                // Console.WriteLine($"[RaidRecord] 记录文件{fileName}的Json格式错误");
                
                raidRecordCache.Add(new MongoId(fileName.Replace(".json", "")), []);
            }
            
        }
        return Task.CompletedTask;
    }

    public void SaveRecord(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            Create(playerId);
        }
        
        string jsonString = jsonUtil.Serialize<List<RaidDataWrapper>>(raidRecordCache[playerId]) ?? "[]";
        
        if (_RecordDbPath == null)
        {
            Error("保存记录数据库时数据库文件路径意外不存在, 请确保`db\\records`文件夹路径存在");
            return;
        }
        File.WriteAllTextAsync(Path.Combine(_RecordDbPath, $"{playerId}.json"), jsonString);
    }

    public List<RaidDataWrapper> GetRecord(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            if (_RecordDbPath == null)
            {
                Error("记录数据库文件路径未正确获取, 请确保`db\\records`文件夹路径存在");
                return [];
            }

            if (Path.Exists(Path.Combine(_RecordDbPath, $"{playerId}.json")))
            {
                raidRecordCache.Add(playerId, jsonUtil.DeserializeFromFile<List<RaidDataWrapper>>(Path.Combine(_RecordDbPath, $"{playerId.ToString()}.json")));
            }
            else
            {
                raidRecordCache.Add(playerId, new List<RaidDataWrapper>());
            }
        }
        var records = raidRecordCache[playerId];
        // Console.WriteLine($"DEBUG RecordCacheManager.GetRecord > 玩家{playerId}的记录有{records.Count}条");
        return records;
    }

    public void Create(MongoId playerId)
    {
        if (!raidRecordCache.ContainsKey(playerId))
        {
            raidRecordCache.Add(playerId, new List<RaidDataWrapper>());
        }
        SaveRecord(playerId);
    }

    public RaidDataWrapper CreateRecord(MongoId playerId)
    {
        try
        {
            if (playerId == null)
            {
                Console.WriteLine($"DEBUG RecordCacheManager.CreateRecord > 玩家playerId为null");
                throw new Exception($"{nameof(playerId)} is null");
            }
            List<RaidDataWrapper> records = GetRecord(playerId);
            // Console.WriteLine($"DEBUG RecordCacheManager.CreateRecord > 玩家{playerId}的记录records为{records}, {records?.Count}条");
            // 检查 records 是否为 null
            if (records == null)
            {
                throw new Exception($"GetRecord中获取的records为null playId: {playerId}");
            }
            var wrapper = new RaidDataWrapper();
            records.Add(wrapper);
            wrapper.Info = new RaidInfo();
            // Console.WriteLine($"DEBUG RecordCacheManager.CreateRecord > 返回值: {wrapper}, Info: {wrapper.Info}, Archive:  {wrapper.Archive}");
            return wrapper;
        }
        catch (Exception e)
        {
            Console.WriteLine($"RecordCacheManager.CreateRecord: {e.Message}\nstack: {e.StackTrace}");
            modConfig.LogError(e, "RaidRecordManager.CreateRecord.try-catch", "创建记录实例时出错");
            throw;
        }
    }

    public void Delete(MongoId playerId)
    {
        if (raidRecordCache.ContainsKey(playerId))
        {
            raidRecordCache.Remove(playerId);
        }
        
        SaveRecord(playerId);
    }

    public void ZipAll(ItemHelper itemHelper)
    {
        foreach (var playerId in raidRecordCache.Keys)
        {
            ZipAll(itemHelper, playerId);
        }
    }
    
    public void ZipAll(ItemHelper itemHelper, MongoId playerId)
    {
        foreach (var raidDataWrapper in raidRecordCache.GetValueOrDefault(playerId, []))
        {
            raidDataWrapper.Zip(itemHelper);
        }
    }
}