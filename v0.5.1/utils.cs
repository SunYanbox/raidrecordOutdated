using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using System.Text.Json;
using raidrecord_v0._5._1.models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace raidrecord_v0._5._1;

public static class Utils
{
    // 精确到h-min-s的格式化时间
    public static string TimeString(long time)
    {
        return $"{time / 3600}h {(time % 3600) / 60}min {time % 60}s";
    }
    
    // 用所有空白字符（空格、制表符、换行符等）分隔命令字符串, 并过滤掉空字符串元素
    public static string[] SplitCommand(string cmd)
    {
        // 根据指定的分隔字符和选项将字符串拆分成子串。
        return cmd.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }
    
    // 根据 ParaInfo 属性更新一条命令使用指南, 追加到原有的 desc 后
    public static void UpdateCommandDesc(Command command)
    {
        string desc = $"> {command.Key}";
    
        if (command.ParaInfo == null || command.ParaInfo.Paras.Count <= 0)
        {
            command.Desc += desc;
            return;
        }
    
        foreach (var para in command.ParaInfo.Paras)
        {
            bool isOptional = command.ParaInfo.Optional.Contains(para);
            string type = command.ParaInfo.Types.ContainsKey(para) ? command.ParaInfo.Types[para] : "undefined";
            string centerString = $"{para}: {type}";
            desc += " " + (isOptional ? $"[{centerString}]" : centerString);
        }
    
        if (command.ParaInfo.Descs.Count > 0)
        {
            foreach (var para in command.ParaInfo.Paras)
            {
                if (command.ParaInfo.Descs.ContainsKey(para))
                {
                    desc += $"\n\t> {para}: {command.ParaInfo.Descs[para]}";
                }
            }
        }
    
        command.Desc += desc;
    }

    // 分隔要发送的字符串, 避免客户端无法完整显示
    public static string[] SplitStringByNewlines(string str)
    {
        // 如果字符串长度不超过限制，直接返回包含原字符串的数组
        if (str.Length <= Constants.SendLimit)
        {
            return new string[] { str };
        }

        // 用换行符分割
        string[] segments = str.Split('\n');
        List<string> result = new List<string>();
        string currentSegment = "";

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            string potentialSegment = currentSegment != "" ? currentSegment + "\n" + segment : segment;

            if (potentialSegment.Length < Constants.SendLimit)
            {
                currentSegment = potentialSegment;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSegment))
                {
                    result.Add(currentSegment);
                }
                currentSegment = segment;
            }
        }

        // 添加最后一个分段
        if (!string.IsNullOrEmpty(currentSegment))
        {
            result.Add(currentSegment);
        }

        return result.ToArray();
    }
    
    /**
     * 获取字典的子集
     * @param dict 原字典
     * @param keys 键的列表
     * @returns
     */
    public static Dictionary<TKey, TValue> GetSubDict<TKey, TValue>(
        Dictionary<TKey, TValue> dict,
        IEnumerable<TKey> keys) 
        // 限制泛型类型参数 TKey 必须是非空类型
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out TValue value))
            {
                result[key] = value;
            }
        }
        return result;
    }

    public static RaidInfo Copy(RaidInfo raidInfo)
    {
        RaidInfo copy = raidInfo with {};
        return copy;
    }

    /**
     * 根据进入/离开突袭前后的仓库, 返回所有物品
     * @param pmcData
     * @param itemHelper
     * @returns Dictionary&lt;MongoId, Item&gt;
     */
    public static Dictionary<MongoId, Item> GetInventoryInfo(PmcData pmcData, ItemHelper itemHelper)
    {
        var result = new Dictionary<MongoId, Item>();
        if (pmcData.Inventory == null || pmcData.Inventory.Equipment == null || pmcData.Inventory.Items == null) return result;
        var inventory = pmcData.Inventory;
        
        // 物品信息在仓库内是正确的, 但是使用JSON序列化和反序列化后不正确了
        // Console.WriteLine(
        //         $"When GetInventoryInfo \n\tpmcData.Inventory: {inventory}"
        //     );
        // var itemData = inventory.Items.Select<Item, string[]>(x =>
        //     [x.Id.ToString(), x.Template.ToString(), x.ParentId != null ? x.ParentId.ToString() : null]).ToArray();
        // foreach (var idtplparent in itemData)
        // {
        //     Console.WriteLine($"\t {string.Join(", ", idtplparent)}");
        // }
        
        // 获取玩家进入/离开突袭时的所有物品
        var aroundRaidItems = itemHelper.FindAndReturnChildrenByAssort(inventory.Equipment.Value, inventory.Items);
        var copyError = "";
        // 转换为映射
        foreach (var item in aroundRaidItems)
        {
            try
            {
                // result[item.Id] = Utils.Copy<Item>(item);
                result[item.Id] = item with {};
                if (item.Template != result[item.Id].Template) throw new Exception($"record类型使用with复制构造改变了Template的MongoId!!!: {item.Template} -> {result[item.Id].Template}");
            }
            catch (Exception e)
            {
                copyError += $"物品{itemHelper.GetItem(item.Template).Value?.Properties?.Name ?? item.Template}复制构造失败({e.Message}), ";
            }
        }
        if (copyError.Length > 0)
        {
            Console.WriteLine($"[RaidRecord] GetInventoryInfo过程中出现问题: {copyError}");
        }
        return result;
    }

    public static T Copy<T>(T source)
    {
        var json = JsonSerializer.Serialize(source);
        var copy = JsonSerializer.Deserialize<T>(json);
        if (copy == null)
        {
            throw new Exception($"复制数据出错");
        }
        return copy;
    }
    
    // 获取物品价值(排除默认物品栏的容器, 如安全箱)
    public static long GetItemValue(Item item, ItemHelper itemHelper)
    {
        // TODO: 更完善的无效物品判断
        // 刀, 安全箱(安全箱不能用parentId, 因为那个是所有容器的基类), 口袋可能很贵, 会影响入场价值
        if (item.SlotId == "SecuredContainer" || item.SlotId == "Scabbard" || item.SlotId == "Dogtag") return 0;
        // 父类是口袋的所有口袋
        HashSet<string> parentIds = [
            "557596e64bdc2dc2118b4571", // 口袋基类
        ];
        if (parentIds.Contains(item?.ParentId ?? "")) return 0;
        
        if (!itemHelper.IsValidItem(item?.Template ?? "")) return 0;
        
        var price = itemHelper.GetItemPrice(item.Template);
        if (price == null)
        {
            Console.WriteLine($"\t{item.Template}没有价格");
            return 0;
        }

        if ((item.Upd ?? null) != null)
        {
            // 可维修物品
            if ((item.Upd?.Repairable ?? null) != null)
            {
                // 暂时不清楚如何处理可维修物品的修正以跳过itemHelper的错误
            }
        }
        // Console.WriteLine($"\t{item.Template}价格: {price.Value} 修正: {itemHelper.GetItemQualityModifier(item)} 返回值: {Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price.Value))}");
        // 修复了错误计算护甲值为0的物品的价值的问题
        return Convert.ToInt64(Math.Max(0.0, itemHelper.GetItemQualityModifier(item) * price.Value));
    }

    // 获取物资列表内所有物资的总价值
    public static long GetItemsValueAll(Item[] items, ItemHelper itemHelper)
    {
        long value = 0;
        foreach (var item in items.Where(i => i.ParentId != "68e2c9a23d4d3dc9e403545f"))
        {
            value += Utils.GetItemValue(item, itemHelper);
        }
        return value;
    }
    
    // 计算库存inventory中所有id处于filter中物品价格
    public static long CalculateInventoryValue(Dictionary<MongoId, Item> inventory, MongoId[] filter,  ItemHelper itemHelper)
    {
        return Utils.GetItemsValueAll(
            inventory.Values.Where(x => filter.Contains(x.Id)).ToArray(),
            itemHelper);
    }
    
    /// <summary>
    /// 计算具有提供的任何基类的所有物品价值
    /// </summary>
    /// <param name="items">物品列表</param>
    /// <param name="baseClasses">基类列表</param>
    /// <param name="itemHelper">物品助手</param>
    /// <returns>物品价值</returns>
    public static long GetItemsValueWithBaseClasses(Item[] items, IEnumerable<MongoId> baseClasses, ItemHelper itemHelper)
    {
        var filteredItems = items.Where(x => itemHelper.IsOfBaseclasses(x.Template, baseClasses)).ToArray();
        return Convert.ToInt64(GetItemsValueAll(filteredItems, itemHelper));
    }

    /// <summary>
    /// 获取物品列表中所有处于指定槽位下的所有物品
    /// </summary>
    /// <param name="desiredContainerSlotId">所希望的容器槽ID</param>
    /// <param name="items">物品列表</param>
    /// <returns>指定容器槽内的所有物品</returns>
    public static Item[] GetAllItemsInContainer(string desiredContainerSlotId, Item[] items)
    {
        List<Item> containerItems = new List<Item>();
        var pushTag = new HashSet<string>();
    
        foreach (var item in items)
        {
            var currentItem = item;
        
            // 递归向上查找父级
            while (currentItem.ParentId != null)
            {
                var parent = Array.Find(items, x => x.Id == currentItem.ParentId);
                // var parent = items.FirstOrDefault(i => i != null && i.Id == currentItem.ParentId, null);
            
                // 如果找不到父级，跳出循环
                if (parent == null) break;

                if (parent.SlotId == desiredContainerSlotId)
                {
                    if (!pushTag.Contains(item.Id))
                    {
                        containerItems.Add(item);
                        pushTag.Add(item.Id);
                    }
                    break;
                }
            
                currentItem = parent;
            }
        }
    
        return containerItems.ToArray();
    }
    
    
}