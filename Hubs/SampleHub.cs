using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChitChit.Areas.Identity.Data;
using ChitChit.Controllers.ChatManagement;
using ChitChit.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;


namespace ChitChit.Hubs
{

    [Authorize]
    /// <summary>
    /// 這邊所定義的方法，可在JavaScript中調用
    /// </summary>
    public class SampleHub : Hub
    {
        private readonly IConnectionMultiplexer _redisService;
        private readonly chitchitContext _chitChitContext;
        // 連線字典 - 用於辨認使用這id對應到的connectionId
        private static Dictionary<string, string> _connections = new Dictionary<string, string>();


        public SampleHub(IConnectionMultiplexer redisService, chitchitContext chitChitContext)
        {
            _redisService = redisService;
            _chitChitContext = chitChitContext;
        }

        public override async Task OnConnectedAsync()
        {

            try
            {
                // 取得連接客戶端http請求相關的查詢字符串參數 - access_token
                var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
                // 建立 JWT 處理器
                var tokenHandler = new JwtSecurityTokenHandler();
                // 解析JWT令牌
                IEnumerable<Claim> j = tokenHandler.ReadJwtToken(token).Claims;
                // 取得用戶聲明的 UserId 
                var userIdList = from s in j
                                 where s.Type == "UserId"
                                 select s;
                // 取得第一個資料
                var userId = userIdList.First();
                // 放入連線字典中
                _connections[userId.Value] = Context.ConnectionId;
            }
            catch (Exception ex) 
            {
                // signalR 連線失效 要緊急處理
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            // 等待連線
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 加入群組
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public async Task AddToGroup(string groupName)
        {
            try
            {
                // 加入群組
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                // 發送訊給此群組的人
                //await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", $"{Context.ConnectionId} has joined the group {groupName}.");

            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/AddToGroup->{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 客戶端從群組中移除
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public async Task RemoveGroup(string groupName)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                //await Clients.Group(groupName).SendAsync("RemoveGroup", $"{Context.ConnectionId} has left the group {groupName}.");
            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/RemoveGroup->{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 將訊息發送給所有客戶端
        /// </summary>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessageToAll(string user, string message)
        {
            try
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/SendMessageToAll->{ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 將訊息發送給指定的群組
        /// </summary>
        /// <param name="groupChatId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendMessageToGroup(int groupChatId, string data)
        {
            string funcFrom = "Hubs/SampleHub/SendMessageToGroup";
            try
            {
                string message = "";
                
                // 群組名稱
                var groupName = $"GroupChatId_{groupChatId}";

                // 解析字串
                var clientMessage = JsonConvert.DeserializeObject<MessageModel>(data);
                if (clientMessage != null )
                {
                    //// 發送訊息給指定群組
                    await Clients.OthersInGroup(groupName).SendAsync("ReceiveGroupMessage", groupChatId, data);

                    // 反序列化
                    var redisKey = JsonConvert.SerializeObject(new SentGroupMessage
                    {
                        MessageContent = clientMessage.MessageContent,
                        GroupChatId = groupChatId,
                        UserId = clientMessage.UserId,
                        FullUserName = clientMessage.UserName,
                        RecordTime = DateTimeOffset.Now
                    });
                    // 轉換時間戳
                    var redisValue = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    // 儲存redis
                    await _redisService.GetDatabase().SortedSetAddAsync($"GroupChat:GroupChatId_{groupChatId}", redisKey, redisValue);

                    // 更新時間資料庫時間
                    var targetChat = _chitChitContext.GroupChat.Where(x => x.Id == groupChatId).FirstOrDefault();
                    if (targetChat != null)
                    {
                        Console.WriteLine("更新已讀時間");
                        targetChat.RecordTime = DateTimeOffset.Now;
                        _chitChitContext.SaveChanges();

                        await Clients.OthersInGroup(groupName).SendAsync("ReceiveGroupNotification", groupChatId);

                        // 回傳結果 - 成功
                        message = "Success";
                        Log.Information($"{funcFrom}->{message}");
                        return;
                    }

                    // 回傳結果 - 成功
                    message = "NotFound";
                    Log.Information($"{funcFrom}->{message}");
                    return;
                }

                // 回傳結果 - 訊息未找到
                message = "MessageNotFound";
                Log.Information($"{funcFrom}->{message}");
                return;

            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/SendMessageToGroup->{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 傳送特定訊息給特定用戶
        /// </summary>
        /// <param name="privateChatId"></param>
        /// <param name="friendId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendMessageToUser(int privateChatId, string friendId, string data)
        {
            string funcFrom = "Hubs/SampleHub/SendMessageToUser";
            try
            {
                string message = "";
                // 解析字串
                var clientMessage = JsonConvert.DeserializeObject<MessageModel>(data);
                if (clientMessage != null)
                {
                    // 檢查字典中是否存放該用戶Id
                    if (_connections.TryGetValue(friendId, out var connectionId))
                    {
                        // 有該Id 傳送特定訊息給特定用戶
                        await Clients.Client(connectionId).SendAsync("ReceiveMessage", privateChatId, data);
                        // 反序列化
                        var redisKey = JsonConvert.SerializeObject(new SentPrivateMessage
                        {
                            MessageContent = clientMessage.MessageContent,
                            PrivateChatId = privateChatId,
                            UserId = clientMessage.UserId,
                            FullUserName = clientMessage.UserName,
                            RecordTime = DateTimeOffset.Now,
                        });
                        // 轉換時間戳
                        var redisValue = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                        // 儲存redus
                        await _redisService.GetDatabase().SortedSetAddAsync($"PrivateChat:PrivateChatId_{privateChatId}", redisKey, redisValue);

                        // 更新時間資料庫時間
                        var targetChat = _chitChitContext.PrivateChat.Where(x => x.Id == privateChatId).FirstOrDefault();
                        if (targetChat != null)
                        {

                            targetChat.RecordTime = DateTimeOffset.Now;
                            _chitChitContext.SaveChanges();

                            await Clients.Client(connectionId).SendAsync("ReceivePrivateNotification", privateChatId);

                            // 回傳結果 - 成功
                            message = "Success";
                            Log.Information($"{funcFrom}->{message}");
                            return;
                        }

                        // 回傳結果 - 成功
                        message = "NotFound";
                        Log.Information($"{funcFrom}->{message}");
                        return;
                    }
                    
                    // 回傳結果 - key未找到
                    message = "KeyNotFound";
                    Log.Information($"{funcFrom}->{message}");
                    return;
                }

                // 回傳結果 - 訊息錯誤
                message = "MessageNotFound";
                Log.Information($"{funcFrom}->{message}");
                return;
            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/SendMessageToGroup->{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 歷史紀錄儲存
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="chatType"></param>
        /// <param name="chatId"></param>
        /// <param name="messageContent"></param>
        /// <returns></returns>
        public async Task SaveHistoryMessage(string userId, ChatType chatType, int chatId, string messageContent)
        {
            string funcFrom = "Hubs/SampleHub/SaveHistoryMessage";
            try
            {
                string message = "";
                
                // 歷史紀錄儲存
                // 儲存redis
                // 更新已讀時間
                if (chatType == ChatType.PrivateChat)
                {
                    await _redisService.GetDatabase().StringSetAsync($"HistoryMessage:HistoryPrivateChat:HistoryPrivateChat_{chatId}_{userId}", messageContent);
                    var target = _chitChitContext.PrivateReadMessage.Where(p => p.PrivateChatId == chatId && p.UserId == userId).FirstOrDefault();
                    if (target != null)
                    {
                        target.ReadByTime = DateTimeOffset.Now;
                    }
                }
                if (chatType == ChatType.GroupChat)
                {
                    await _redisService.GetDatabase().StringSetAsync($"HistoryMessage:HistoryGroupChat:HistoryGroupChat_{chatId}_{userId}", messageContent);
                    var target = _chitChitContext.GroupReadMessage.Where(p => p.GroupChatId == chatId && p.UserId == userId).FirstOrDefault();
                    if (target != null)
                    {
                        target.ReadByTime = DateTimeOffset.Now;
                    }
                }

                _chitChitContext.SaveChanges();
                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/SendMessageToGroup->{ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 刪除時間區間的資料
        /// </summary>
        /// <returns></returns>
        public async Task DeletMessageByTimeInterval()
        {
            try
            {
                // 起始時間
                DateTimeOffset mintime = new DateTimeOffset(2024, 5, 23, 9, 20, 0, new TimeSpan(8, 0, 0));
                // 終止時間
                DateTimeOffset maxtime = new DateTimeOffset(2024, 5, 23, 9, 38, 0, new TimeSpan(8, 0, 0));
                // 刪除時間區間的資料
                await _redisService.GetDatabase().SortedSetRemoveRangeByScoreAsync("PrivateChat:PmId_1", mintime.ToUnixTimeMilliseconds(), maxtime.ToUnixTimeMilliseconds());


            }
            catch (Exception ex)
            {
                Log.Error($"Hub/SampleHub/DeletMessageByTimeInterval->{ex.Message}");
                throw;
            }
        }




    }

    public class SentMessageGroupHub
    {
        public string UserId { get; set; } = null!;

        public string FullUserName { get; set; } = null!;

        public int ChatPrivate_Id { get; set; }
    }

    public class SentGroupMessage
    {
        public string MessageContent { get; set; } = "";

        public int GroupChatId { get; set; }

        public string UserId { get; set; } = null!;

        public string? FullUserName { get; set; }

        public DateTimeOffset RecordTime { get; set; }
    }
    public class SentPrivateMessage
    {
        public string MessageContent { get; set; } = "";

        public int PrivateChatId { get; set; }

        public string UserId { get; set; } = null!;

        public string? FullUserName { get; set; }

        public DateTimeOffset RecordTime { get; set; }
    }


    public class RedisMessageModel: SentMessageGroupHub
    {
        public DateTimeOffset RecordTime {  get; set; }
    }

}
