using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;

namespace raidrecord_v0._5._1.models;

public record RaidResultData 
{
    // 结果字符串
    [JsonPropertyName("result")]
    public ExitStatus? Result { get;  set; } = null;
    // 击杀玩家的ID
    [JsonPropertyName("killerId")] public string? KillerId { get; set; } = null;
    // 击杀玩家的名称
    [JsonPropertyName("killerAid")] public string? KillerAid { get; set; } = null;
    // 撤离点名称
    [JsonPropertyName("exitName")] public string? ExitName { get; set; } = null;
    // 本局游玩时间
    [JsonPropertyName("playTime")] public long PlayTime { get; set; } = 0;
}

public record RaidInfo
{
    // 对局ID
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;
    // 玩家ID
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;
    // 对局创建时间
    [JsonPropertyName("createTime")] public long CreateTime { get; set; } = 0;
    // 存档状态
    [JsonPropertyName("state")] public string State { get; set; } = string.Empty;
    // 玩家阵营(PMC, SCAV)
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    // 带入物品
    [JsonPropertyName("itemsTakeIn")]
    public Dictionary<MongoId, Item> ItemsTakeIn { get; set; } = new();
    // 带出物品
    [JsonPropertyName("itemsTakeOut")]
    public Dictionary<MongoId, Item> ItemsTakeOut { get; set; } = new();
    // 对局结束后带出的物品
    [JsonPropertyName("addition")] public List<MongoId> Addition { get; set; } = [];
    // 对局结束后移除的物品
    [JsonPropertyName("remove")] public List<MongoId> Remove { get; set; } = [];
    // 对局结束后变化的物品
    [JsonPropertyName("changed")] public List<MongoId> Changed { get; set; } = [];
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

    /**
     * 根据ID变换信息, 更新IRaidInfo
     * 将更新ItemsTakeIn, ItemsTakeOut字典
     * 与Addition, Remove, Changed列表的与id有关字段
     * @param replaceInfo { 旧ID -> 新ID }
     */
    public void UpdateByReplaceIDs(Dictionary<MongoId, MongoId> replaceInfo)
    {
        foreach (var map in new[] {this.ItemsTakeIn, this.ItemsTakeOut})
        {
            foreach (var oldId in map.Keys)
            {
                if (replaceInfo.TryGetValue(new MongoId(oldId), out var newId))
                {
                    if (newId == oldId) continue;
                    var itemInstance = map[oldId];
                    if (!map.Remove(oldId))
                    {
                        Console.WriteLine($"[RaidRecord] 警告 从字典删除{oldId}的过程中出错");
                    }
                    itemInstance.Id = newId;
                    map[newId] = itemInstance;
                }
            }
        }
        
        List<MongoId>[] lists = [this.Addition, this.Remove, this.Changed];
        foreach (var list in lists) {
            for (var i = 0; i < list.Count; i++)
            {
                MongoId oldId = list[i];
                var newId = replaceInfo[oldId];
                if (newId != null && oldId != newId) {
                    list[i] = newId;
                }
            }
        }
    }

    // 根据开局请求初始化数据
    public void HandleRaidStart(StartLocalRaidResponseData data, MongoId sessionId, ItemHelper itemHelper, ProfileHelper profileHelper)
    {
        var judge = VerifyStartLocalRaidResponseData(data);
        if (judge != null)
        {
            Console.WriteLine($"[RaidRecord] 使用RaidInfo.HandleRaidStart过程中参数错误: {judge}");
            return;
        }
        ServerId = data.ServerId;
        State = "未归档";
        Side = ServerId.Contains("Pmc") ? "Pmc" : "Savage";
        var pmcProfile = profileHelper.GetPmcProfile(sessionId);
        PlayerId = pmcProfile.Id;
        CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        ItemsTakeIn = Utils.GetInventoryInfo(pmcProfile, itemHelper);
        // Console.WriteLine($"获取到的物品:");
        // foreach (var item in ItemsTakeIn.Values)
        // {
        //     Console.WriteLine($"\t{item}");
        // }
        
        var itemsTakeIn = ItemsTakeIn.Values.ToArray();
        PreRaidValue = Utils.GetItemsValueAll(itemsTakeIn, itemHelper);
        EquipmentValue = Utils.GetItemsValueWithBaseClasses(itemsTakeIn, _equipments, itemHelper);
        SecuredValue = Utils.GetItemsValueAll(Utils.GetAllItemsInContainer("SecuredContainer", itemsTakeIn), itemHelper);
        // Console.WriteLine($"itemsTakeIn.Length: {itemsTakeIn.Length}\n\tPreRaidValue: {PreRaidValue}\n\tEquipmentValue: {EquipmentValue}\n\tSecuredValue: {SecuredValue}");
    }

    // 根据结束请求载入数据
    public void HandleRaidEnd(EndLocalRaidRequestData data, MongoId sessionId, ItemHelper itemHelper,
        ProfileHelper profileHelper)
    {
        var pmcProfile = profileHelper.GetPmcProfile(sessionId);
        if (PlayerId != pmcProfile.Id)
        {
            Console.WriteLine($"[RaidRecord] 错误: 尝试修改不属于{pmcProfile.Id}的对局数据");
            return;
        }
        ItemsTakeOut = Utils.GetInventoryInfo(pmcProfile, itemHelper);
        HandleRaidEndInventoryAndValue(pmcProfile, itemHelper);
        
        if (data == null) throw new Exception($"HandleRaidEnd的参数data意外为null");

        if (data.Results != null)
        {
            Results = new RaidResultData();
            Results.Result = data.Results.Result;
            Results.KillerId = data.Results.KillerId;
            Results.KillerAid = data.Results.KillerAid;
            Results.ExitName = data.Results.ExitName;
            Results.PlayTime = Convert.ToInt64(data.Results.PlayTime);
        }
    }
    
    private string? VerifyStartLocalRaidResponseData(StartLocalRaidResponseData data)
    {
        if (data == null) return "StartLocalRaidResponseData为null";
        if (data.ServerId == null) return "参数ServerId意外为null";

        return null;
    }
    
    /*
     * 根据对局结束的数据(变化量, 结果)归档到本RaidInfo
     */
    private void HandleRaidEndInventoryAndValue(PmcData pmcData, ItemHelper itemHelper)
    {
        Addition.Clear();
        Remove.Clear();
        Changed.Clear();
        if (pmcData.Stats == null || pmcData.Stats.Eft == null)
        {
            Console.WriteLine($"[RaidInfo] 错误尝试获取对局结束数据时, 获取到的数据全部为null");
            return;
        }
        State = State == "推测对局" ? "推测对局" : "已归档";
        // 处理对局结果
        // var resultStats = Utils.Copy(pmcData.Stats.Eft);
        EftStats = pmcData.Stats.Eft with
        {
            SessionCounters=null,
            OverallCounters = null,
            DroppedItems = null,
            DamageHistory = null
        };
        // 处理价值相关数据
        if (ItemsTakeIn.Count == 0 && ItemsTakeOut.Count == 0)
        {
            PreRaidValue = EquipmentValue = SecuredValue = GrossProfit = CombatLosses = 0;
            return;
        }
        // 记录获取/变化的物资
        foreach (var (itemId, item) in ItemsTakeIn)
        {
            if (ItemsTakeOut.TryGetValue(itemId, out var newItem))
            {
                if (Math.Abs(itemHelper.GetItemQualityModifier(item) - itemHelper.GetItemQualityModifier(newItem)) < 1e-6) continue;
                Changed.Add(itemId);
            }
            else
            {
                Remove.Add(itemId);
            }
        }
        foreach (var (itemId, _) in ItemsTakeOut)
        {
            if (!ItemsTakeIn.ContainsKey(itemId)) Addition.Add(itemId);
        }
        // 收益, 战损记录
        GrossProfit = Utils.CalculateInventoryValue(ItemsTakeOut, Addition.ToArray(), itemHelper);
        CombatLosses = Utils.CalculateInventoryValue(ItemsTakeIn, Remove.ToArray(), itemHelper);
        foreach (var (itemId, oldItem) in Utils.GetSubDict(ItemsTakeIn, Changed))
        {
            var oldValue = Utils.GetItemValue(oldItem, itemHelper);
            if (ItemsTakeOut.TryGetValue(itemId, out var newItem))
            {
                var newValue = Utils.GetItemValue(newItem, itemHelper);
                if (newValue >  oldValue) GrossProfit += newValue -  oldValue;
                else CombatLosses += oldValue - newValue;
            }
            else
            {
                Console.WriteLine($"[RaidRecord] 警告: 本应同时存在于ItemsTakeIn和ItemsTakeOut中的物品({itemId})不存在于第二者中");
            }
        }
    }
    
    // 被视为战备的基类(枪械, 胸挂, 背包, 护甲, 头盔等)
    [JsonIgnore]
    private static MongoId[] _equipments =
    [
        BaseClasses.WEAPON,
        // BaseClasses.UBGL,
        BaseClasses.ARMOR,
        BaseClasses.ARMORED_EQUIPMENT,
        BaseClasses.HEADWEAR,
        BaseClasses.FACE_COVER,
        BaseClasses.VEST,
        BaseClasses.BACKPACK,
        BaseClasses.VISORS,
        BaseClasses.GASBLOCK,
        BaseClasses.RAIL_COVERS,
        BaseClasses.MOD,
        BaseClasses.FUNCTIONAL_MOD,
        BaseClasses.GEAR_MOD,
        BaseClasses.STOCK,
        BaseClasses.FOREGRIP,
        BaseClasses.MASTER_MOD,
        BaseClasses.MOUNT,
        BaseClasses.MUZZLE,
        BaseClasses.SIGHTS,
        BaseClasses.ASSAULT_SCOPE,
        BaseClasses.TACTICAL_COMBO,
        BaseClasses.FLASHLIGHT,
        BaseClasses.MAGAZINE,
        BaseClasses.LIGHT_LASER,
        BaseClasses.FLASH_HIDER,
        BaseClasses.COLLIMATOR,
        BaseClasses.IRON_SIGHT,
        BaseClasses.COMPACT_COLLIMATOR,
        BaseClasses.COMPENSATOR,
        BaseClasses.OPTIC_SCOPE,
        BaseClasses.SPECIAL_SCOPE,
        BaseClasses.SILENCER,
        BaseClasses.AUXILIARY_MOD,
        BaseClasses.BIPOD,
        BaseClasses.BUILT_IN_INSERTS,
        BaseClasses.ARMOR_PLATE,
        BaseClasses.HANDGUARD,
        BaseClasses.PISTOL_GRIP,
        BaseClasses.RECEIVER,
        BaseClasses.BARREL,
        // BaseClasses.CHARGING_HANDLE,
        BaseClasses.MUZZLE_COMBO,
        BaseClasses.TACTICAL_COMBO
    ];
}

