using raidrecord_v0._5._1.models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace raidrecord_v0._5._1;

// 常量配置

// TODO 全局本地化
public static class Constants
{
    public const double ArchiveCheckJudgeError = 1e-6;
    public const int SendLimit = 491;

    public static readonly Dictionary<string, string> MapNames = new Dictionary<string, string>
    {
        ["factory4_day"] = "工厂(白天)",
        ["factory4_night"] = "工厂(夜晚)",
        ["bigmap"] = "海关",
        ["woods"] = "森林",
        ["shoreline"] = "海岸线",
        ["interchange"] = "立交桥",
        ["rezervbase"] = "储备站",
        ["laboratory"] = "实验室",
        ["lighthouse"] = "灯塔",
        ["tarkovstreets"] = "街区",
        ["sandbox"] = "中心区"
    };
    
    public static readonly Dictionary<ExitStatus, string> ResultNames = new Dictionary<ExitStatus, string>
    {
        [ExitStatus.SURVIVED] = "幸存",
        [ExitStatus.KILLED] = "行动中阵亡",
        [ExitStatus.LEFT] = "离开行动",
        [ExitStatus.MISSINGINACTION] = "行动中失踪",
        [ExitStatus.RUNNER] = "匆匆逃离",
        [ExitStatus.TRANSIT] = "过渡"
    };
    
    public static readonly Dictionary<string, string> ArmorZone = new Dictionary<string, string>
    {
        // QuestCondition/Elimination/Kill/BodyPart/
        { "Chest", "胸腔" },
        { "Head", "头部" },
        { "LeftArm", "左臂" },
        { "LeftLeg", "左腿" },
        { "RightArm", "右臂" },
        { "RightLeg", "右腿" },
        // { "Stomach", "胃部" },
        
        // Collider Type 
        { "Back", "胸部, 背部" },
        { "BackHead", "头部, 脖颈" },
        { "Ears", "头部, 耳部" },
        { "Eyes", "头部, 眼部" },
        { "Groin", "胃部, 股沟" },
        { "HeadCommon", "头部, 脸部" },
        { "Jaw", "头部, 下颚" },
        { "LeftCalf", "左腿, 小腿" },
        { "LeftForearm", "左臂, 前臂" },
        { "LeftSide", "左下身" },
        { "LeftSideChestDown", "胃部, 左侧" },
        { "LeftSideChestUp", "胸部, 左腋下" },
        { "LeftThigh", "左腿, 大腿" },
        { "LeftUpperArm", "左臂, 手臂" },
        { "LowerBack", "胃部, 下背部" },
        { "NeckBack", "胸部, 脖子" },
        { "NeckFront", "胸部, 喉部" },
        { "ParietalHead", "头部, 头顶" },
        { "Pelvis", "胃部, 股沟" },
        { "PelvisBack", "胃部, 臀部" },
        { "Ribcage", "胸腔" },
        { "RibcageLow", "胃部" },
        { "RibcageUp", "胸部" },
        { "RightCalf", "右腿, 小腿" },
        { "RightForearm", "右臂, 前臂" },
        { "RightSide", "右下身" },
        { "RightSideChestDown", "胃部, 右侧" },
        { "RightSideChestUp", "胸部, 右腋下" },
        { "RightThigh", "右腿, 大腿" },
        { "RightUpperArm", "右臂, 手臂" },
        { "SpineDown", "胃部, 下背部" },
        { "SpineTop", "胸腔, 上背部" },
        { "Stomach", "胃部" }
    };

    // Scav角色本地化
    public static readonly Dictionary<string, string> RoleNames = new Dictionary<string, string>
    {
        { "ArenaFighterEvent", "寻血猎犬" },
        { "Boss", "Boss" },
        { "ExUsec", "游荡者" },
        { "Follower", "保镖" },
        { "Marksman", "狙击手" },
        { "PmcBot", "掠夺者" },
        { "Sectant", "???" },
        { "infectedAssault", "感染者" },
        { "infectedCivil", "感染者" },
        { "infectedLaborant", "感染者" },
        { "infectedPmc", "感染者" },
        { "infectedTagilla", "感染者" },
        // 更本地化一点
        { "pmcBot", "人机" },
        { "pmcBEAR", "BearPMC" },
        { "pmcUSER", "UserPMC" }
    };
}

// 命令参数 | 参与命令调用的参数
public class Parametrics
{
    public string SessionId { get; set; }
    public RaidRecordManagerChat? ManagerChat { get; set; }
    public Dictionary<string, string> Paras { get; set; }

    public Parametrics(string sessionId, RaidRecordManagerChat managerChat)
    {
        SessionId = sessionId;
        ManagerChat = managerChat;
        Paras = new Dictionary<string, string>();
    }
}

// 回调函数类型
public delegate string CommandCallback(Parametrics parametrics);

// 参数信息 | 仅用于help获取命令信息
public class ParaInfo
{
    public List<string> Paras { get; set; }
    public Dictionary<string, string> Types { get; set; }
    public Dictionary<string, string> Descs { get; set; }
    public HashSet<string> Optional { get; set; }

    public ParaInfo()
    {
        Paras = new List<string>();
        Types = new Dictionary<string, string>();
        Descs = new Dictionary<string, string>();
        Optional = new HashSet<string>();
    }
}

public class Command
{
    public string? Key { get; set; }
    public string? Desc { get; set; }
    public ParaInfo? ParaInfo { get; set; }
    public Parametrics? Paras { get; set; }
    public CommandCallback? Callback { get; set; }
}

public class ParaInfoBuilder
{
    private ParaInfo _paraInfo;

    public ParaInfoBuilder()
    {
        _paraInfo = new ParaInfo();
    }

    /// <summary>
    /// 添加参数
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="type">参数类型</param>
    /// <param name="desc">参数描述</param>
    public ParaInfoBuilder AddParam(string name, string type, string desc)
    {
        _paraInfo.Paras.Add(name);
        _paraInfo.Types[name] = type;
        _paraInfo.Descs[name] = desc;
        return this;
    }

    public ParaInfoBuilder SetOptional(string[] parameters)
    {
        foreach (var para in parameters)
        {
            _paraInfo.Optional.Add(para);
        }
        return this;
    }

    /// <summary>
    /// 构建参数信息, 返回构建好的实例后重置自身的数据
    /// </summary>
    public ParaInfo Build()
    {
        var info = _paraInfo;
        _paraInfo = new ParaInfo();
        return info;
    }
}


[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 4)]
public class RaidRecordManagerChat(
    MailSendService mailSendService,
    ISptLogger<RaidRecordManagerChat> logger,
    ProfileHelper profileHelper,
    DialogueHelper dialogueHelper,
    LocalizationManager localizationManager,
    DatabaseService databaseService,
    ItemHelper itemHelper,
    ModConfig modConfig,
    RecordCacheManager recordCacheManager) : IDialogueChatBot, IOnLoad
{
    private Dictionary<string, Command> _commands = new Dictionary<string, Command>();
    protected ParaInfoBuilder ParaInfoBuilder = new ParaInfoBuilder();

    private string GetLocalText(string msgId, params object?[] args)
    {
        return localizationManager.GetTextFormat(msgId, args);
    }
    
    public Task OnLoad()
    {
        InitCommands();
        return Task.CompletedTask;
    }
    
    public UserDialogInfo GetChatBot()
    {
        return new UserDialogInfo
        {
            Id = "68e2d45e17ea301214c2596d",
            Aid = 8100860,
            Info = new UserDialogDetails
            {
                Nickname = GetLocalText("RC MC.ChatBot.NickName"),
                Side = "Usec",
                Level = 69,
                MemberCategory = MemberCategory.Sherpa,
                SelectedMemberCategory = MemberCategory.Sherpa
            }
        };
    }

    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        try
        {
            SendAllMessage(sessionId, HandleCommand(request.Text, sessionId));
        } catch (Exception e) {
            // this.error(e.name);
            // this.error(e.message);
            // this.error(e.stack);
            logger.Error($"[RaidRecord]<Chat> {GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message)}");
            modConfig?.LogError(e, "RaidRecordManagerChat.HandleMessage", GetLocalText("RC MC.Chat.HM.error0", sessionId, e.Message));
            SendMessage(sessionId, GetLocalText("RC MC.Chat.HM.error1", request.Text, e.Message));
        }
        return ValueTask.FromResult(request.DialogId);
    }
    
    
    // 注册命令
    
    protected void InitCommands()
    {
        Command[] commands = [
            new Command
            {
                Key = "help",
                Desc = GetLocalText("Command.Help.Desc"),
                ParaInfo = new ParaInfo(),
                Paras = null,
                Callback = GetHelpCommand()
            },
            new Command
            {
                Key = "list",
                Desc = GetLocalText("Command.List.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("limit", "int", GetLocalText("Command.Para.Limit.Desc"))
                    .AddParam("page", "int", GetLocalText("Command.Para.Page.Desc"))
                    .SetOptional(["limit", "page"])
                    .Build(),
                Paras = null,
                Callback = GetListCommand()
            },
            new Command
            {
                Key = "info",
                Desc = GetLocalText("Command.Info.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("serverId", "string", GetLocalText("Command.Para.ServerId.Desc"))
                    .AddParam("index", "int", GetLocalText("Command.Para.Index.Desc"))
                    .SetOptional(["serverId", "index"])
                    .Build(),
                Paras = null,
                Callback = GetInfoCommand()
            },
            new Command
            {
                Key = "items",
                Desc = GetLocalText("Command.Items.Desc"),
                ParaInfo = ParaInfoBuilder
                    .AddParam("serverId", "string", GetLocalText("Command.Para.ServerId.Desc"))
                    .AddParam("index", "int", GetLocalText("Command.Para.Index.Desc"))
                    .SetOptional(["serverId", "index"])
                    .Build(),
                Paras = null,
                Callback = GetItemsCommand()
            },
            new Command
            {
                Key = "cls",
                Desc = GetLocalText("Command.Cls.Desc"),
                ParaInfo = new ParaInfo(),
                Paras = null,
                Callback = GetClsCommand()
            }
        ];
        foreach (var command in commands) {
            Utils.UpdateCommandDesc(command);
            if (command.Key == null) continue;
            _commands[command.Key] = command;
        }
        logger.Info($"[RaidRecord] {GetLocalText("RC MC.Chat.initCmd.info0", string.Join(", ", _commands.Keys.ToArray()))}");
    }

    private string HandleCommand(string command, string sessionId)
    {
        string[] data = Utils.SplitCommand(command.ToLower());
        if (data.Length <= 0) {
            return GetLocalText("RC MC.Chat.handleCmd.error0");
        }

        // logger.Info($"全部命令: {string.Join(", ", _commands.Keys.ToArray())}, 输入的指令: \"{command}\", 检测出的指令: {data[0]}");
        if (!_commands.ContainsKey(data[0]))
        {
            return GetLocalText("RC MC.Chat.handleCmd.error1", data[0], string.Join(",", _commands.Keys.ToArray()));
        }
        Command iCmd = _commands[data[0]];
        iCmd.Paras = new Parametrics(sessionId, this);

        int index = 1;
        while (index >= 1 && index < data.Length) {
            if (!string.IsNullOrEmpty(data[index+1])) {
                iCmd.Paras.Paras[data[index]] = data[index+1];
                index += 1;
            }
            index += 1;
        }
        string result = iCmd.Callback(iCmd.Paras);
        // 垃圾回收 低效 未来再优化
        iCmd.Paras.ManagerChat = null;
        iCmd.Paras.Paras.Clear();
        iCmd.Paras = null;
        return result;
    }
    
    /**
     * 将消息发给对应sessionId的客户端
     * @param sessionId
     * @param msg
     */
    public void SendMessage(string sessionId, string msg)
    {
        SendMessageDetails details = new SendMessageDetails
        {
            RecipientId = sessionId,
            MessageText = msg,
            Sender = MessageType.UserMessage,
            SenderDetails = GetChatBot()
        };
        // (
        //     sessionId,
        //     MessageType.UserMessage,
        //     GetChatBot(),
        //     Message: msg,
        //         );
        mailSendService.SendMessageToPlayer(details);
    }

    public async void SendAllMessage(string sessionId, string message)
    {
        string[] messages = Utils.SplitStringByNewlines(message);
        if (messages.Length == 0) { return; }
        if (messages.Length == 1)
        {
            await Task.Delay(1000);
            SendMessage(sessionId, messages[0]);
            return;
        }
        
        await Task.Delay(750);
        
        for (int i = 0; i < messages.Length; i++)
        {
            SendMessage(sessionId, messages[i]);
            if (i < messages.Length - 1)
            {
                await Task.Delay(1250);
            }
        }
    }
    
    // Command 的工具
    
    protected string? VerifyIParametrics(Parametrics parametrics)
    {
        if (string.IsNullOrEmpty(parametrics.SessionId)) 
        { 
            return GetLocalText("RC MC.Chat.verify.error0"); 
        }

        try
        {
            string playerId = profileHelper.GetPmcProfile(parametrics.SessionId).Id;
            if (string.IsNullOrEmpty(playerId)) throw new Exception(GetLocalText("RC MC.Chat.verify.error1"));
        }
        catch (Exception e)
        {
            modConfig.LogError(e, "RaidRecordManagerChat.VerifyIParametrics", GetLocalText("RC MC.Chat.verify.error2", e.Message));
            return GetLocalText("RC MC.Chat.verify.error2", e.Message); 
        }
        
        if (parametrics.ManagerChat == null) 
        { 
            return GetLocalText("RC MC.Chat.verify.error3"); 
        }
        
        if (parametrics.Paras == null) 
        { 
            return GetLocalText("RC MC.Chat.verify.error4"); 
        }
        
        return null;
    }

    protected PmcData? GetPmcProfile(string sessionId)
    {
        return profileHelper.GetPmcProfile(sessionId);
    }
    
    protected List<RaidArchive> GetArchivesBySession(string sessionId)
    {
        List<RaidArchive> result = [];
        var pmcData = GetPmcProfile(sessionId);
        if (pmcData == null) return result;
        
        if (pmcData.Id == null) return result;
        List<RaidDataWrapper> records = recordCacheManager.GetRecord(pmcData.Id.Value);
        foreach (var record in records)
        {
            if (record.IsArchive)
            {
                result.Add(record.Archive);
            }
        }
        return result;
    }

    protected string DateFormatterFull(long timestamp)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Unix 时间起点
        DateTime date = epoch.AddSeconds(timestamp).ToLocalTime();
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;
        var time = date.ToShortTimeString();
        return GetLocalText("RC MC.Chat.Format.Time", year, month, day, time);
    }
    
    protected string GetArchiveDetails(RaidArchive archive)
    {
        string msg = "";
        var serverId = archive.ServerId;
        var playerId = archive.PlayerId;
        var playerData = profileHelper.GetProfileByPmcId(playerId);
        // 本次对局元数据
        var _timeString = DateFormatterFull(archive.CreateTime);
        string mapName = serverId.Substring(0, serverId.IndexOf('.')).ToLower();
        
        msg += GetLocalText("RC MC.Chat.GAD.info0", 
            _timeString, serverId, playerData.Info.Nickname, playerData.Info.Level, playerData.Id);

        msg += GetLocalText("RC MC.Chat.GAD.info1", 
            localizationManager.GetMapName(mapName), Utils.TimeString(archive?.Results?.PlayTime ?? 0));

        msg += GetLocalText("RC MC.Chat.GAD.info2", 
            (int)(archive.EquipmentValue), (int)(archive.SecuredValue), (int)(archive.PreRaidValue));

        msg += GetLocalText("RC MC.Chat.GAD.info3", 
            (int)(archive?.GrossProfit ?? 0), 
            (int)(archive?.CombatLosses ?? 0), 
            (int)(archive?.GrossProfit - archive?.CombatLosses ?? 0));

        string result = GetLocalText("RC MC.Chat.GAD.unknowResult");

        if (archive?.Results?.Result != null)
        {
            ExitStatus nonNullResult = archive.Results.Result.Value;
            if (Constants.ResultNames.TryGetValue(nonNullResult, out string resultName))
            {
                result = localizationManager.GetText(resultName, resultName);
            }
        }
        
        msg += GetLocalText("RC MC.Chat.GAD.info4", 
            result,
            localizationManager.GetExitName(mapName, archive?.Results?.ExitName ?? GetLocalText("RC MC.Chat.GAD.nullExitPosition")),
            archive?.EftStats?.SurvivorClass ?? GetLocalText("RC MC.Chat.GAD.unknow"));
        
        var victims = archive?.EftStats?.Victims?.ToList() ?? new List<Victim>();
        var _locals = databaseService.GetTables().Locales.Global[localizationManager.CurrentLanguage];
        var locals = _locals.Value;
        
        if (victims.Count > 0)
        {
            msg += GetLocalText("RC MC.Chat.GAD.killed");
            foreach (var victim in victims)
            {
                msg += GetLocalText("RC MC.Chat.GAD.info5",
                    victim.Time,
                    locals.TryGetValue(victim.Weapon, out var value1)
                        ? value1
                        : (victim.Weapon ?? GetLocalText("RC MC.Chat.GAD.unknowWeapon")),
                    localizationManager.GetArmorZoneName(victim.BodyPart ?? ""),
                    (int)(victim.Distance ?? 0),
                    victim.Name,
                    victim.Level,
                    victim.Side,
                    localizationManager.GetRoleName(victim.Role ?? ""));
                // Constants.RoleNames.TryGetValue(victim.Role,  out var value3) ? value3 : victim.Role);
            }
        }

        if (archive.Results.Result == ExitStatus.KILLED)
        {
            var aggressor = archive?.EftStats?.Aggressor;
            if (aggressor != null)
            {
                msg += GetLocalText("RC MC.Chat.GAD.info6",
                    aggressor.Name,
                    aggressor.Side,
                    locals.TryGetValue(aggressor.WeaponName, out var value1)
                        ? value1
                        : (aggressor.WeaponName ?? "未知武器"),
                    // Constants.RoleNames.TryGetValue(aggressor.Role,  out var value3) ? value3 : aggressor.Role);
                    localizationManager.GetRoleName(aggressor.Role ?? ""));
            }
            else
            {
                msg += GetLocalText("RC MC.Chat.GAD.killedLoadError");
            }
        }

        return msg;
    }
    
    protected string GetItemsDetails(RaidArchive archive)
    {
        string msg = "";
        var serverId = archive.ServerId;
        var playerId = archive.PlayerId;
        var playerData = profileHelper.GetProfileByPmcId(playerId);
        // 本次对局元数据
        var _timeString = DateFormatterFull(archive.CreateTime);
        string mapName = serverId.Substring(0, serverId.IndexOf('.')).ToLower();
        
        msg += GetLocalText("RC MC.Chat.GAD.info0", 
            _timeString, serverId, playerData.Info.Nickname, playerData.Info.Level, playerData.Id);

        msg += GetLocalText("RC MC.Chat.GAD.info1", 
            localizationManager.GetMapName(mapName), Utils.TimeString(archive?.Results?.PlayTime ?? 0));

        msg += GetLocalText("RC MC.Chat.GAD.info2", 
            (int)(archive.EquipmentValue), (int)(archive.SecuredValue), (int)(archive.PreRaidValue));

        msg += GetLocalText("RC MC.Chat.GAD.info3", 
            (int)(archive?.GrossProfit ?? 0), 
            (int)(archive?.CombatLosses ?? 0), 
            (int)(archive?.GrossProfit - archive?.CombatLosses ?? 0));

        string result = GetLocalText("RC MC.Chat.GAD.unknow");

        if (archive?.Results?.Result != null)
        {
            ExitStatus nonNullResult = archive.Results.Result.Value;
            if (Constants.ResultNames.TryGetValue(nonNullResult, out string resultName))
            {
                result = localizationManager.GetText(resultName, resultName);
            }
        }
        
        msg += GetLocalText("RC MC.Chat.GAD.info4", 
            result,
            localizationManager.GetExitName(mapName, archive?.Results?.ExitName ?? GetLocalText("RC MC.Chat.GAD.nullExitPosition")),
            archive?.EftStats?.SurvivorClass ?? GetLocalText("RC MC.Chat.GAD.unknow"));

        var itemTpls = databaseService.GetTables().Templates.Items;
        var local = databaseService.GetTables().Locales.Global[localizationManager.CurrentLanguage].Value;
        if (local == null) return GetLocalText("RC MC.Chat.GID.error0");

        if (archive.ItemsTakeIn.Count > 0)
        {
            // "\n\n带入对局物品:\n   物品名称  物品单价  物品修正  物品总价值"
            msg += GetLocalText("RC MC.Chat.GID.info0");
            foreach (var (tpl, modify) in archive.ItemsTakeIn)
            {
                var item = itemTpls[tpl];
                var price = itemHelper.GetItemPrice(tpl) ?? 0;
                msg += string.Format("\n\n - {0}  {1}  {2}  {3} {4}",
                    local[$"{tpl} ShortName"], price, modify, price * modify, local[$"{tpl} Description"]
                );
            }
        }
        
        if (archive.ItemsTakeOut.Count > 0)
        {
            // "\n\n带出对局物品:\n   物品名称  物品单价  物品修正  物品总价值  物品描述"
            msg += GetLocalText("RC MC.Chat.GID.info1");

            foreach (var (tpl, modify) in archive.ItemsTakeOut)
            {
                var item = itemTpls[tpl];
                var price = itemHelper.GetItemPrice(tpl) ?? 0;
                msg += string.Format("\n\n - {0}  {1}  {2}  {3}  {4}",
                    local[$"{tpl} ShortName"], price, modify, price * modify, local[$"{tpl} Description"]
                );
            }
        }
        
        return msg;
    }
    
    // Command 专区
    
    public CommandCallback GetHelpCommand()
    {
        return parametrics =>
        {
            string? verify =  VerifyIParametrics(parametrics);
            if (verify != null) return verify;
            
            // "帮助信息(参数需要按键值对写, 例如\"list index 1\"; 中括号表示可选参数; 指令与参数不区分大小写):"
            string msg = GetLocalText("Command.Help.Head");
            foreach (var cmd in parametrics.ManagerChat._commands.Values)
            {
                msg += $"\n - {cmd.Key}: {cmd.Desc}\n";
            }
            return msg;
        };
    }
    
    private CommandCallback GetClsCommand()
    {
        return parametrics =>
        {
            string? verify = VerifyIParametrics(parametrics);
            if (verify != null) return verify;

            var managerProfile = GetChatBot();

            Dictionary<MongoId, Dialogue> dialogs = dialogueHelper.GetDialogsForProfile(parametrics.SessionId);
            var dialog = dialogs[managerProfile.Id];
            if (dialog != null && dialog.Messages != null)
            {
                int count = dialog.Messages.Count;
                dialog.Messages = [];
                // $"已清除{count}条聊天记录, 重启游戏客户端后生效"
                return GetLocalText("Command.Cls.info0", count);
            }
            // "找不到你的聊天记录"
            return GetLocalText("Command.Cls.error0");
        };
    }
    
    public CommandCallback GetListCommand() {
        return parametrics =>
        {
            string? verify = VerifyIParametrics(parametrics);
            if (verify != null) return verify;

            List<RaidArchive> records = GetArchivesBySession(parametrics.SessionId);
            int numberLimit = 0, page = 0;
            bool showServerId = false;
            try {
                numberLimit = int.TryParse(parametrics.Paras.GetValueOrDefault("limit", "10"), out int _limit) ? _limit : 10;
                page = int.TryParse(parametrics.Paras.GetValueOrDefault("page", "1"), out int _page) ? _page : 1;
            } catch (Exception e) {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }
            numberLimit = Math.Min(20, Math.Max(1, numberLimit));
            page = Math.Max(1, page);

            int indexLeft = Math.Max(numberLimit *  (page - 1), 0);
            int indexRight = Math.Min(numberLimit * page, records.Count);
            // if (records.Count <= 0) return "您没有任何历史战绩, 请至少对局一次后再来查询吧";
            if (records.Count <= 0) return GetLocalText("Command.List.error0");
            List<RaidArchive> results = [];
            for (int i = indexLeft; i < indexRight; i++) {
                results.Add(records[i]);
            }
            // if (results.Count <= 0) return $"未查询到您第{indexLeft+1}到{indexRight}条历史战绩";
            if (results.Count <= 0) return GetLocalText("Command.List.error1", indexLeft+1, indexRight);
            
            // string msg = $"历史战绩(共{results.Count}/{records.Count}条):\n - serverId                 序号 地图 入场总价值 带出收益 战损 游戏时间 结果\n";
            string msg = GetLocalText("Command.List.info0", results.Count, records.Count);

            int jump = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] == null || string.IsNullOrEmpty(results[i].ServerId)) { jump++; continue; }

                string result = GetLocalText("Command.List.unknownEnding");
                try
                {
                    if (results[i].Results == null || results[i].Results.Result == null) throw new Exception();
                    var resultName = Constants.ResultNames[results[i].Results.Result.Value];
                    result = localizationManager.GetText(resultName, resultName);
                }
                catch (Exception e)
                {
                    modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", "尝试从本地数据库获取对局结果信息时出错");
                }
                
                msg += $" - {results[i].ServerId} {indexLeft + i} {localizationManager.GetMapName(results[i].ServerId.Substring(0, results[i].ServerId.IndexOf('.')).ToLower())} {results[i].PreRaidValue} {results[i].GrossProfit} {results[i].CombatLosses} {Utils.TimeString(results[i].Results.PlayTime)} {result}\n";
            }
            // if (jump > 0) msg += $"跳过{jump}条无效数据";
            if (jump > 0) msg += GetLocalText("Command.List.info1", jump);
            return msg;
        };
    }
    
    public CommandCallback GetInfoCommand() {
        return parametrics =>
        {
            string? verify = VerifyIParametrics(parametrics);
            if (verify != null) return verify;

            string serverId; int index;
            try {
                serverId = parametrics.Paras.GetValueOrDefault("serverid", "");
                index = int.TryParse(parametrics.Paras.GetValueOrDefault("index", "-1"), out int _index) ? _index : -1;
            } catch (Exception e) {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }

            if (!string.IsNullOrEmpty(serverId)) {
                var records = GetArchivesBySession(parametrics.SessionId);
                var record = records.Find(x => x.ServerId.ToString() == serverId);
                if (record != null) {
                    return GetArchiveDetails(record);
                } else {
                    // return $"serverId为{serverId}的对局不存在, 请检查你的输入";
                    return GetLocalText("Command.Para.ServerId.NotExist", serverId);
                }
            } 
            if (index >= 0) {
                var records = GetArchivesBySession(parametrics.SessionId);
                // if (index >= records.Count) return $"索引{index}超出范围: [0, {records.Count})";
                if (index >= records.Count) return GetLocalText("Command.Para.Index.OutOfRange", index, records.Count);
                return GetArchiveDetails(records[index]);
            }
            var records2 = GetArchivesBySession(parametrics.SessionId);
            // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
            return GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
        };
    } 
    
    public CommandCallback GetItemsCommand() {
        return parametrics =>
        {
            string? verify = VerifyIParametrics(parametrics);
            if (verify != null) return verify;

            string serverId; int index;
            try {
                serverId = parametrics.Paras.GetValueOrDefault("serverid", "");
                index = int.TryParse(parametrics.Paras.GetValueOrDefault("index", "-1"), out int _index) ? _index : -1;
            } catch (Exception e) {
                // return $"参数解析时出现错误: {e.Message}";
                modConfig.LogError(e, "RaidRecordManagerChat.ListCommand", GetLocalText("Command.Para.Parse.error0", e.Message));
                return GetLocalText("Command.Para.Parse.error0", e.Message);
            }
            
            // TODO: 显示新获得/遗失/更改的物品
            
            if (!string.IsNullOrEmpty(serverId)) {
                var records = GetArchivesBySession(parametrics.SessionId);
                var record = records.Find(x => x.ServerId.ToString() == serverId);
                if (record != null) {
                    return GetItemsDetails(record);
                } else {
                    // return $"serverId为{serverId}的对局不存在, 请检查你的输入";
                    return GetLocalText("Command.Para.ServerId.NotExist", serverId);
                }
            } 
            if (index >= 0) {
                var records = GetArchivesBySession(parametrics.SessionId);
                // if (index >= records.Count) return $"索引{index}超出范围: [0, {records.Count})";
                if (index >= records.Count) return GetLocalText("Command.Para.Index.OutOfRange", index, records.Count);
                return GetItemsDetails(records[index]);
            }
            
            var records2 = GetArchivesBySession(parametrics.SessionId);
            // return $"请输入正确的serverId(当前: {serverId})或index(当前: {index} not in [0, {records2.Count}))";
            return GetLocalText("Command.Para.Presentation", serverId, index, records2.Count);
        };
    } 
}