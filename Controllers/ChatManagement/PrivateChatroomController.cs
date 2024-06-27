using ChitChit.Areas.Identity.Data;
using ChitChit.Hubs;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using MimeKit;
using Newtonsoft.Json;
using Org.BouncyCastle.Tls;
using Serilog;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;

namespace ChitChit.Controllers.ChatManagement
{
    /// <summary>
    /// 私聊聊天
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "ChatManagement")]

    public class PrivateChatroomController : ControllerBase
    {
        private readonly chitchitContext _chitChitContext;

        private readonly IHubContext<SampleHub> _hubContext;

        private readonly IConnectionMultiplexer _redisService;

        public PrivateChatroomController(chitchitContext chitChitContext, IHubContext<SampleHub> hubContext, IConnectionMultiplexer redisService)
        {
            _chitChitContext = chitChitContext;
            _hubContext = hubContext;
            _redisService = redisService;
        }

        /// <summary>
        /// 取得該用戶的所有私聊
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("PrivateChats")]
        public async Task<IActionResult> PrivateChats([FromQuery] string userId)
        {
            string funcFrom = "Controllers/PrivateChatroomController/PrivateChats:[GET]";
            try
            {
                string message = "";
                // 回傳串列
                var responseList = new List<ReadPrivateChatsModel>();
                // redis 資料庫
                var redisDB = _redisService.GetDatabase();

                // 用戶為使用者 1
                var friend1 = from friend in _chitChitContext.FriendRel
                              join privateChat in _chitChitContext.PrivateChat on friend.Id equals privateChat.FriendRelId
                              join user in _chitChitContext.ChitChitUser on friend.UserId2 equals user.Id
                              where userId == friend.UserId1 && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                              select new
                              {
                                 PrivatChatId = privateChat.Id,
                                 FriendRelId = friend.Id,
                                 FullUserName = user.FullUserName,
                                 TargetId = user.Id,
                                 Avatar = user.Avatar,
                                 FriendRelReocrdTime = friend.RecordTime,
                                 PrivateChatRecordTime = privateChat.RecordTime
                              };
                // 用戶為使用者 2
                var friend2 = from friend in _chitChitContext.FriendRel
                              join privateChat in _chitChitContext.PrivateChat on friend.Id equals privateChat.FriendRelId
                              join user in _chitChitContext.ChitChitUser on friend.UserId1 equals user.Id
                              where userId == friend.UserId2 && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                              select new
                              {
                                  PrivatChatId = privateChat.Id,
                                  FriendRelId = friend.Id,
                                  FullUserName = user.FullUserName,
                                  TargetId = user.Id,
                                  Avatar = user.Avatar,
                                  FriendRelReocrdTime = friend.RecordTime,
                                  PrivateChatRecordTime = privateChat.RecordTime
                              };
                // 合併集合 (排序由新到舊)
                var allFrinedList = friend1.Union(friend2).OrderByDescending(f => f.PrivateChatRecordTime).ToList();

                // 遍歷
                foreach (var friendChat in allFrinedList)
                {
                    // 最新消息
                    string lastMessage = "";
                    // 未讀訊息數
                    long unReadCount;

                    // 處理最新訊息 ---------------------------------------------------------------------------------------------------------------------------------------------------
                    // 取得最新一筆的訊息資料
                    var messages = await redisDB.SortedSetRangeByRankWithScoresAsync($"PrivateChat:PrivateChatId_{friendChat.PrivatChatId}", 0, 0, Order.Descending);
                    // 確認有無最新訊息資料
                    if (messages.Length > 0)
                    {
                        // 有訊息
                        // 序列化
                        var jsonValue = JsonConvert.DeserializeObject<PrivateMessage>(messages[0].Element.ToString());
                        if (jsonValue != null)
                        {
                            // 帶入最新消息
                            lastMessage = jsonValue.MessageContent ?? "";
                        }
                    }
                    else
                    {
                        // 無任何訊息
                        lastMessage = "";
                    }



                    // 處理未讀訊息 ---------------------------------------------------------------------------------------------------------------------------------------------------
                    // 取得用戶對該私聊的已讀時間
                    var readMessage = _chitChitContext.PrivateReadMessage.Where(r => r.PrivateChatId == friendChat.PrivatChatId && r.UserId == userId).FirstOrDefault();
                    // 確認有無已讀資料
                    if (readMessage != null)
                    {
                        // 有已讀資料
                        // 起始時間
                        var startStamp = readMessage.ReadByTime.ToUnixTimeMilliseconds();
                        // 未讀訊息數
                        //unReadCount = await redisDB.SortedSetLengthAsync($"PrivateChat:PrivateChatId_{friendChat.PrivatChatId}", startStamp, double.PositiveInfinity);

                        // 未讀訊息
                        var sortedSet = await redisDB.SortedSetRangeByScoreAsync($"PrivateChat:PrivateChatId_{friendChat.PrivatChatId}", startStamp, double.PositiveInfinity);
                        unReadCount = sortedSet.Select(value => JsonConvert.DeserializeObject<RedisMessageModel>(value.ToString())).Count(m => m.UserId != userId);

                    }
                    else
                    {
                        // 完全沒有已讀資料
                        // 未讀訊息數
                        unReadCount = await redisDB.SortedSetLengthAsync($"PrivateChat:PrivateChatId_{friendChat.PrivatChatId}");
                    }

                    // 處理最後回傳資料 ---------------------------------------------------------------------------------------------------------------------------------------------------
                    // 回傳資料加入串列中
                    responseList.Add(new ReadPrivateChatsModel
                    {
                        ChatId = friendChat.PrivatChatId,
                        ChatName = friendChat.FullUserName,
                        ChatType = ChatType.PrivateChat,
                        TargetId = friendChat.TargetId,
                        RecordTime = friendChat.PrivateChatRecordTime,
                        LastMessage = lastMessage,
                        UnReadMessage = unReadCount,
                    });

                };



                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = responseList });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }


        ///// <summary>
        ///// 取得該私聊的聊天內容
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="userId"></param>
        ///// <param name="pageNumber"></param>
        ///// <returns></returns>
        //[HttpGet("PrivateChats/{id}")]
        //public async Task<IActionResult> PrivateChats(int id, [FromQuery]string userId, int pageNumber)
        //{
        //    string funcFrom = "Controllers/PrivateChatroomController/PrivateChats/{id}:[GET]";
        //    try
        //    {
        //        string message = "";
        //        // 每次取得數量
        //        int pageSize = 20;
        //        // 起始值
        //        int start = (pageNumber - 1) * pageSize;
        //        // 結束值
        //        int stop = start + pageSize - 1;

        //        // 先確認有無該聊天室
        //        var targetChat = _chitChitContext.PrivateChat.Where(g => g.Id == id).FirstOrDefault();
        //        if (targetChat == null)
        //        {
        //            message = "NotFound";
        //            Log.Information($"{funcFrom}->{message}");
        //            return Ok(new { state = "normal", message, result = new { } });
        //        }

        //        // 取得 redis 資料
        //        var sortedSet = await _redisService.GetDatabase().SortedSetRangeByRankWithScoresAsync($"PrivateChat:PrivateChatId_{id}", start: start, stop: stop,order: Order.Descending);
             
        //        // json串列
        //        var messageList = new List<MessageModel>();
        //        // 遍歷串列
        //        foreach(var item in sortedSet )
        //        {
        //            // 序列化
        //            var jsonValue = JsonConvert.DeserializeObject<RedisMessageModel>(item.Element.ToString());
        //            if (jsonValue != null)
        //            {
        //                var chatMessage = new MessageModel
        //                {
        //                    UserName = jsonValue.FullUserName,
        //                    MessageContent = jsonValue.MessageContent,
        //                    RecordTime = jsonValue.RecordTime,
        //                };
        //                // 加入串列
        //                messageList.Add(chatMessage);
        //            } 
        //        }

        //        var responseList = new ChatMessageModel
        //        {
        //            ChatId = targetChat.Id,
        //            ChatType = ChatType.PrivateChat,
        //            Message = messageList
        //        };

        //        // 進行寫入或更新已讀回執時間
        //        var targetReadMessage = _chitChitContext.PrivateReadMessage.Where(r => r.PrivateChatId == id && r.UserId == userId).FirstOrDefault();

        //        // 確認有無已讀表
        //        if (targetReadMessage != null)
        //        {
        //            // 有已讀資料
        //            // 更新已讀時間
        //            targetReadMessage.ReadByTime = DateTimeOffset.Now;
        //        }
        //        else
        //        {
        //            // 無已讀資料
        //            // 新增資料
        //            var newReadMessageObj = new PrivateReadMessage
        //            {
        //                UserId = userId,
        //                PrivateChatId = id,
        //                ReadByTime = DateTimeOffset.Now
        //            };

        //            // 新增資料
        //            _chitChitContext.PrivateReadMessage.Add(newReadMessageObj);
        //            // 儲存資料庫
        //            _chitChitContext.SaveChanges();
                    
        //        }
                


        //        // 回傳結果 - 成功
        //        message = "Success";
        //        Log.Information($"{funcFrom}->{message}");
        //        return Ok(new { state = "normal", message, result = responseList });
        //    }
        //    catch (Exception ex)
        //    {
        //        // 回傳結果 - 伺服器錯誤
        //        Log.Error($"{funcFrom}->{ex.Message}");
        //        return Ok(new { status = "error", message = ex.Message, result = new { } });
        //    }
        //}

        /// <summary>
        /// 取得該私聊的聊天訊息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("PrivateChats/{id}/Messages")]
        public async Task<IActionResult> PrivateChats(int id, [FromQuery] string userId)
        {
            string funcFrom = "Controllers/PrivateChatroomController/PrivateChats/{id}/Messages:[GET]";
            try
            {
                string message = "";

                // 先確認有無該聊天室
                var targetChat = _chitChitContext.PrivateChat.Where(g => g.Id == id).FirstOrDefault();
                if (targetChat == null)
                {
                    message = "NotFound";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }
               
                
                // redis資料庫
                var redisDb = _redisService.GetDatabase();
                // 回傳串列
                var messageList = new List<MessageModel>();
                DateTimeOffset lastViewedTime;
                // 取得歷史緩存訊息
                var historyMessageByStr = await redisDb.StringGetAsync($"HistoryMessage:HistoryPrivateChat:HistoryPrivateChat_{id}_{userId}");
                var historyMessage = JsonConvert.DeserializeObject<List<MessageModel>>(historyMessageByStr.ToString());
                if (historyMessage != null)
                {
                    messageList.AddRange(historyMessage);
                }

                //------------------------------------------------------------------------------------------------------------------------
                // 取得未讀訊息
                var viewedTime = _chitChitContext.PrivateReadMessage.Where(r => r.PrivateChatId == id && r.UserId == userId).FirstOrDefault();
                if (viewedTime != null)
                {
                    // 已有已讀
                    lastViewedTime = viewedTime.ReadByTime;
                    // 起始時間
                    var startStamp = viewedTime.ReadByTime.ToUnixTimeMilliseconds();
                    // 未讀訊息
                    var sortedSet = await redisDb.SortedSetRangeByScoreAsync($"PrivateChat:PrivateChatId_{id}", startStamp, double.PositiveInfinity);
                        
                    foreach(var r in sortedSet)
                    {
                        var jsonValue = JsonConvert.DeserializeObject<RedisMessageModel>(r.ToString());
                        if (jsonValue != null)
                        {
                            var obj = new MessageModel
                            {
                                UserId = jsonValue.UserId,
                                UserName = jsonValue.FullUserName,
                                MessageContent = jsonValue.MessageContent,
                                RecordTime = jsonValue.RecordTime
                            };
                            messageList.Add(obj);
                        }
                    }
                    // 更新已讀時間
                    viewedTime.ReadByTime = DateTimeOffset.Now;
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();
                }
                else
                {
                    lastViewedTime = DateTimeOffset.Now;
                    // 沒有已讀資料現在建立
                    var newReadMessageObj = new PrivateReadMessage
                    {
                        UserId = userId,
                        PrivateChatId = id,
                        ReadByTime = DateTimeOffset.Now
                    };

                    // 新增資料
                    _chitChitContext.PrivateReadMessage.Add(newReadMessageObj);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();


                    // 每次取得數量
                    int pageSize = 50;
                    // 起始值
                    int start = (1 - 1) * pageSize;
                    // 結束值
                    int stop = start + pageSize - 1;
                    // 取最新50筆放進去
                    var sortedSet = await _redisService.GetDatabase().SortedSetRangeByRankWithScoresAsync($"PrivateChat:PrivateChatId_{id}", start: start, stop: stop);
                    foreach (var r in sortedSet)
                    {
                        // 序列化
                        var jsonValue = JsonConvert.DeserializeObject<RedisMessageModel>(r.Element.ToString());
                        if (jsonValue != null)
                        {
                            var obj = new MessageModel
                            {
                                UserId = jsonValue.UserId,
                                UserName = jsonValue.FullUserName,
                                MessageContent = jsonValue.MessageContent,
                                RecordTime = jsonValue.RecordTime
                            };
                            // 加入串列
                            messageList.Add(obj);
                        }
                    }
                }
                
                messageList = messageList.OrderBy(x => x.RecordTime).ToList(); 
                // 緩存資料
                var jsonStr = JsonConvert.SerializeObject(messageList);
                if (jsonStr != null)
                {
                    await redisDb.StringSetAsync($"HistoryMessage:HistoryPrivateChat:HistoryPrivateChat_{id}", jsonStr);
                }

                // 回傳資料
                var response = new ChatMessageModel
                {
                    ChatId = id,
                    ChatType = ChatType.PrivateChat,
                    LastViewedTime = lastViewedTime,
                    Message = messageList
                };

                
            

                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = response });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 傳送訊息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("SentMessage")]
        public IActionResult SentMessage([FromBody] MessageModel data)
        {
            string funcFrom = "Controllers/PrivateChatroomController/SentMessage";
            try
            {
                string message = "";

                //// 先確認有無該私聊 Id
                //var existChatPrivateId = _chitChitContext.PrivateChat.Any(p => p.Id == data.PrivateChatId);
                //if (existChatPrivateId)
                //{

                //    // 取得現在時間
                //    DateTimeOffset now = DateTimeOffset.Now;
                //    // 反序列化
                //    var saveData = JsonConvert.SerializeObject(new PrivateMessage()
                //    {
                //        UserId = data.UserId,
                //        FullUserName = data.FullUserName,
                //        PrivateChatId = data.PrivateChatId,
                //        MessageContent = data.MessageContent,
                //        RecordTime = now,
                //    }); 

                //    // 儲存redis
                //    await _redisService.GetDatabase().SortedSetAddAsync($"PrivateChat:PrivateChatId_{data.PrivateChatId}", saveData, now.ToUnixTimeMilliseconds());
                    
                //    // 發送訊息給指定群組
                //    await _hubContext.Clients.Group($"PrivateChatId_{data.PrivateChatId}").SendAsync("ReceiveMessage", data.UserId, data.MessageContent);
                
                //    // 回傳結果 - 成功
                //    message = "Success";
                //    Log.Information($"{funcFrom}->{message}");
                //    return Ok(new { state = "normal", message, result = string.Empty });
                //}

                // 回傳結果 - 無此私聊
                message = "NoPrivateChat";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = string.Empty });

            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }



        /// <summary>
        /// 查詢歷史聊天訊息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="historyTime"></param>
        /// <returns></returns>
        [HttpGet("HistoryPrivateChats/{id}")]
        public async Task<IActionResult> HistoryPrivateChats(int id, [FromQuery] DateTimeOffset historyTime)
        {
            string funcFrom = "Controllers/PrivateChatroomController/HistoryPrivateChats/{id}:[GET]";
            try
            {
                string message = "";
                // 先確認有無該聊天室
                var targetChat = _chitChitContext.PrivateChat.Where(g => g.Id == id).FirstOrDefault();
                if (targetChat == null)
                {
                    message = "NotFound";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }
                
                
                // 取得數量
                int count = 50;
                // 回傳串列
                var responseList = new List<MessageModel>();
                // 最小區間 (無限小)
                double minScore = double.NegativeInfinity;
                // 最大時間戳
                var maxScore = historyTime.ToUnixTimeMilliseconds();
                // 取得資料
                var result = await _redisService.GetDatabase().SortedSetRangeByScoreAsync($"PrivateChat:PrivateChatId_{id}", start: minScore, stop: maxScore, exclude: Exclude.Stop, order: Order.Descending, skip: 0, take: count);
                foreach(var item in result )
                {
                    var jsonValue = JsonConvert.DeserializeObject<RedisMessageModel>(item.ToString());

                    if (jsonValue != null)
                    {
                        responseList.Add(new MessageModel
                        {
                            UserName = jsonValue.FullUserName,
                            UserId = jsonValue.UserId,
                            MessageContent = jsonValue.MessageContent,
                            RecordTime = jsonValue.RecordTime
                        });
                    }
                }
                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = responseList });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }


        [HttpGet("SignalR")]
        public async Task<IActionResult> SignalR()
        {
            string funcFrom = "Controllers/PrivateChatroomController/SignalR";
            try
            {
                string message = "";

                var data = new Test()
                {
                    id = "2324124124214",
                    name = funcFrom,
                };
                

                // 序列化
                var value = JsonConvert.SerializeObject(data);
                // 儲存字串 
                //_redisService.GetDatabase().StringSet("test:No1", value);
                // RedisJson 儲存方式
                //_redisService.GetDatabase().ExecuteAsync("JSON.SET", "test:No1", ".", value);
                
                // 起始時間
                DateTimeOffset mintime = new DateTimeOffset(2024, 5, 23, 9, 20, 0, new TimeSpan(8, 0, 0));
                // 終止時間
                DateTimeOffset maxtime = new DateTimeOffset(2024, 5, 23, 9, 38, 0, new TimeSpan(8, 0, 0));
                // 刪除時間區間的資料
                await _redisService.GetDatabase().SortedSetRemoveRangeByScoreAsync("PrivateChat:PmId_1", mintime.ToUnixTimeMilliseconds(), maxtime.ToUnixTimeMilliseconds());

                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = string.Empty });
            }
            catch(Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }
        
    } 
    public class Test
    {
        public string id {  get; set; }
        public string name { get; set; }
    }



    public class ReadPrivateChatsModel
    {
        public int ChatId { get; set; }

        public string? ChatName { get; set; }

        public ChatType ChatType { get; set; }

        public string TargetId { get; set; } = null!;

        public DateTimeOffset RecordTime { get; set; }

        public string? LastMessage { get; set; }

        public long UnReadMessage { get; set; }
    }

    public class MessageModel
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string MessageContent { get; set; } = null!;
        public DateTimeOffset RecordTime { get; set; }
    }

    public class ChatMessageModel
    {
        public int ChatId { get; set; }

        public ChatType ChatType { get; set; }

        public DateTimeOffset LastViewedTime { get; set; }

        public List<MessageModel> Message { get; set; } = new List<MessageModel>();

    }

    public class RedisMessageModel
    {
        public int PrivateChatId { get; set; }

        public string MessageContent { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string FullUserName { get; set; } = null!;

        public DateTimeOffset RecordTime { get; set; }

    }

    public enum ChatType
    {
        [Description("私聊")]
        PrivateChat = 0,

        [Description("群聊")]
        GroupChat = 1,
    }
}
