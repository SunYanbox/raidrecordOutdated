using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace raidrecord_v0._5._1.models;

public record RaidArchive
{
    // 对局ID
    [JsonPropertyName("serverId")] public string ServerId { get; set; } = string.Empty;
    // 玩家ID
    [JsonPropertyName("playerId")] public string PlayerId { get; set; } = string.Empty;
    // 对局创建时间
    [JsonPropertyName("createTime")] public long CreateTime { get; set; } = 0;
    // 存档状态
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    // 玩家阵营(PMC, SCAV)
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    // 带入物品
    [JsonPropertyName("itemsTakeIn")] public Dictionary<MongoId, double> ItemsTakeIn { get; set; } = new Dictionary<MongoId, double>();
    // 带出物品
    [JsonPropertyName("itemsTakeOut")]
    public Dictionary<MongoId, double> ItemsTakeOut { get; set; } = new Dictionary<MongoId, double>();
    // // 对局结束后带出的物品
    // [JsonPropertyName("addition")] public readonly Dictionary<MongoId, double> Addition = new Dictionary<MongoId, double> {};
    // // 对局结束后移除的物品
    // [JsonPropertyName("remove")] public readonly Dictionary<MongoId, double> Remove = new Dictionary<MongoId, double> {};
    // // 对局结束后变化的物品
    // [JsonPropertyName("changed")] public readonly Dictionary<MongoId, double> Changed = new Dictionary<MongoId, double> {};
    // 入场价值
    [JsonPropertyName("preRaidValue")] public long PreRaidValue { get; set; } = 0;
    // 装备价值
    [JsonPropertyName("equipmentValue")] public long EquipmentValue { get; set; } = 0;
    // 安全箱价值
    [JsonPropertyName("securedValue")] public long SecuredValue { get; set; } = 0;
    // 毛收益
    [JsonPropertyName("grossProfit")] public long GrossProfit { get; set; } = 0;
    // 战损
    [JsonPropertyName("combatLosses")] public long CombatLosses { get; set; } = 0;
    // 战局结果状态(包括详细击杀数据
    [JsonPropertyName("eftStats")] public EftStats? EftStats { get; set; } = null;
    // 突袭实际结果
    [JsonPropertyName("results")] public RaidResultData? Results { get; set; } = null;

    // 压缩战斗记录信息
    public void Zip(RaidInfo raidInfo, ItemHelper itemHelper)
    {
        ServerId  = raidInfo.ServerId;
        PlayerId  = raidInfo.PlayerId;
        CreateTime = raidInfo.CreateTime;
        State = raidInfo.State;
        Side = raidInfo.Side;
        if (raidInfo.EftStats == null)
        {
            State = "中途退出";
        }
        
        // Console.WriteLine("RaidInfo.ItemsTakeIn");
        // foreach (var (_, item) in raidInfo.ItemsTakeIn)
        // {
        //     Console.WriteLine($"\t{item.Template} {item.SlotId}");
        // }
        // Console.WriteLine("RaidInfo.ItemsTakeOut");
        // foreach (var (_, item) in raidInfo.ItemsTakeOut)
        // {
        //     Console.WriteLine($"\t{item.Template} {item.SlotId}");
        // }
        
        ItemsTakeIn = new Dictionary<MongoId, double>();
        ItemsTakeOut = new Dictionary<MongoId, double>();
        foreach (var (_, item) in raidInfo.ItemsTakeIn)
        {
            // 过滤掉无效物品
            if (!itemHelper.IsValidItem(item.Template)) continue;
            if (!ItemsTakeIn.ContainsKey(item.Template)) ItemsTakeIn.Add(item.Template, 0);
            var count = item.Upd?.StackObjectsCount ?? 1;
            // if (count > 1) Console.WriteLine($"\t物品{item.Template.ToString()}堆叠{count}个");
            ItemsTakeIn[item.Template] += itemHelper.GetItemQualityModifier(item) * count;
        }
        foreach (var (_, item) in raidInfo.ItemsTakeOut)
        {
            // 过滤掉无效物品
            if (!itemHelper.IsValidItem(item.Template)) continue;
            if (!ItemsTakeOut.ContainsKey(item.Template)) ItemsTakeOut.Add(item.Template, 0);
            var count = item.Upd?.StackObjectsCount ?? 1;
            // if (count > 1) Console.WriteLine($"\t物品{item.Template.ToString()}堆叠{count}个");
            ItemsTakeOut[item.Template] += itemHelper.GetItemQualityModifier(item) * count;
        }
        
        // Console.WriteLine("RaidArchive.ItemsTakeIn");
        // foreach (var (tpl, modify) in ItemsTakeIn)
        // {
        //     Console.WriteLine($"\t{tpl}x{modify}");
        // }
        // Console.WriteLine("RaidArchive.ItemsTakeOut");
        // foreach (var (tpl, modify) in ItemsTakeOut)
        // {
        //     Console.WriteLine($"\t{tpl}x{modify}");
        // }
        
        // 其他属性
        PreRaidValue = raidInfo.PreRaidValue;
        EquipmentValue = raidInfo.EquipmentValue;
        SecuredValue = raidInfo.SecuredValue;
        GrossProfit = raidInfo.GrossProfit;
        CombatLosses = raidInfo.CombatLosses;
        EftStats = raidInfo.EftStats;
        Results = raidInfo.Results;
    }
}