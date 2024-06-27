using ChitChit.Areas.Identity.Data;
using ChitChit.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using ChitChit.Models;
using Serilog;
using Newtonsoft.Json;
using MimeKit;
using System.Collections.Generic;

namespace ChitChit.Controllers.ChatManagement
{
    /// <summary>
    /// 群組聊天
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "ChatManagement")]
    public class GroupChatroomController : ControllerBase
    {

        private readonly chitchitContext _chitChitContext;

        private readonly IHubContext<SampleHub> _hubContext;

        private readonly IConnectionMultiplexer _redisService;
        public GroupChatroomController(chitchitContext chitChitContext, IHubContext<SampleHub> hubContext, IConnectionMultiplexer redisService)
        {
            _chitChitContext = chitChitContext;
            _hubContext = hubContext;
            _redisService = redisService;
        }

        /// <summary>
        /// 查詢用戶的所有群組
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("GroupChats/ByUserId")]
        public async Task<IActionResult> GroupChats([FromQuery] string userId)
        {
            string funcFrom = "Controllers/GroupChatroomController/GroupChats/ByUserId:[GET]";

            try
            {
                string message = "";
                // redis 資料庫
                var redisDB = _redisService.GetDatabase();
                // 回傳串列
                var responseList = new List<ReadGroupChatsModel>();

                // 取得與用戶關聯的群組
                var target = from groupRel in _chitChitContext.GroupRel
                             join groupchat in _chitChitContext.GroupChat on groupRel.GroupChatId equals groupchat.Id
                             where groupRel.UserId == userId
                             select groupchat;

                // 確認不為null
                if (target != null)
                {
                    // 遍歷所有相關群組資料
                    foreach (var group in target)
                    {
                        // 最新訊息
                        string lastMessage = "";
                        // 未讀訊息數
                        long unReadCount;


                        // 取得每個群組最新訊息 ---------------------------------------------------------------------------------------------------------------------------------------------------
                        // 取得最新一筆的訊息資料
                        var messages = await redisDB.SortedSetRangeByRankWithScoresAsync($"GroupChat:GroupChatId_{group.Id}", 0, 0, Order.Descending);
                        // 確認有無最新訊息資料
                        if (messages.Length > 0)
                        {
                            // 有訊息
                            // 序列化
                            var jsonValue = JsonConvert.DeserializeObject<GroupMessageModel>(messages[0].Element.ToString());
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

                        // 取得每個群組未讀訊息 ---------------------------------------------------------------------------------------------------------------------------------------------------
                        // 取得用戶對該私聊的已讀時間
                        var readMessage = _chitChitContext.GroupReadMessage.Where(r => r.GroupChatId == group.Id && r.UserId == userId).FirstOrDefault();
                        // 確認有無已讀資料
                        if (readMessage != null)
                        {
                            // 有已讀資料
                            // 起始時間
                            var startStamp = readMessage.ReadByTime.ToUnixTimeMilliseconds();
                            // 未讀訊息數
                            //unReadCount = await redisDB.SortedSetLengthAsync($"GroupChat:GroupChatId_{group.Id}", startStamp, double.PositiveInfinity);
                            var sortedSet = await redisDB.SortedSetRangeByScoreAsync($"GroupChat:GroupChatId_{group.Id}", startStamp, double.PositiveInfinity);

                            unReadCount = sortedSet.Select(value => JsonConvert.DeserializeObject<RedisMessageModel>(value.ToString())).Count(m => m.UserId != userId);

                        }
                        else
                        {
                            // 完全沒有已讀資料
                            // 未讀訊息數
                            unReadCount = await redisDB.SortedSetLengthAsync($"GroupChat:GroupChatId_{group.Id}");
                        }

                        responseList.Add(new ReadGroupChatsModel
                        {
                            ChatId = group.Id,
                            ChatName = group.GroupName,
                            ChatType = ChatType.GroupChat,
                            RecordTime = group.RecordTime,
                            LastMessage = lastMessage,
                            UnReadMessage = unReadCount
                        });


                    }
                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = responseList });

                }



                // 回傳結果 - 查無此群組
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }


        ///// <summary>
        ///// 查詢特定群組的聊天內容
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="userId"></param>
        ///// <param name="pageNumber"></param>
        ///// <returns></returns>
        //[HttpGet("GroupChats/{id}/GroupMessages")]
        //public async Task<IActionResult> GroupMessages(int id, [FromQuery] string userId, int pageNumber)
        //{
        //    string funcFrom = "Controllers/GroupChatroomController/GroupChats/{id}/GroupMessages:[GET]";

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
        //        var targetChat = _chitChitContext.GroupChat.Where(g => g.Id == id).FirstOrDefault();
        //        if (targetChat == null)
        //        {
        //            message = "NotFound";
        //            Log.Information($"{funcFrom}->{message}");
        //            return Ok(new { state = "normal", message, result = new { } });
        //        }
        //        // 取得 redis 資料
        //        var sortedSet = await _redisService.GetDatabase().SortedSetRangeByRankWithScoresAsync($"GroupChat:GroupChatId_{id}", start: start, stop: stop, order: Order.Descending);
        //        // json串列
        //        var messageList = new List<MessageModel>();
        //        // 遍歷串列
        //        foreach (var item in sortedSet)
        //        {
        //            // 序列化
        //            var jsonValue = JsonConvert.DeserializeObject<RedisMessageModel>(item.Element.ToString());
        //            if (jsonValue != null)
        //            {
        //                // 加入串列
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
        //            ChatType = ChatType.GroupChat,
        //            Message = messageList
        //        };

        //        // 進行寫入或更新已讀回執時間
        //        var targetReadMessage = _chitChitContext.GroupReadMessage.Where(r => r.GroupChatId == id && r.UserId == userId).FirstOrDefault();

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

        //            // 確認有無該群組id
        //            var groupExist = _chitChitContext.GroupChat.Any(g => g.Id == id);
        //            if (groupExist)
        //            {
        //                // 新增資料
        //                var newReadMessageObj = new GroupReadMessage
        //                {
        //                    UserId = userId,
        //                    GroupChatId = id,
        //                    ReadByTime = DateTimeOffset.Now
        //                };

        //                // 新增資料
        //                _chitChitContext.GroupReadMessage.Add(newReadMessageObj);
        //                // 儲存資料庫
        //                _chitChitContext.SaveChanges();
        //            }
        //            else
        //            {
        //                // 回傳結果 - 查無群組
        //                message = "NotFoundGroup";
        //                Log.Information($"{funcFrom}->{message}");
        //                return Ok(new { state = "normal", message, result = responseList });
        //            }

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
        /// 查詢特定群組的聊天訊息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("GroupChats/{id}/Messages")]
        public async Task<IActionResult> GroupMessages(int id, [FromQuery] string userId)
        {
            string funcFrom = "Controllers/GroupChatroomController/GroupChats/{id}/GroupMessages:[GET]";

            try
            {
                string message = "";

                // 先確認有無該聊天室
                var targetChat = _chitChitContext.GroupChat.Where(g => g.Id == id).FirstOrDefault();
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
                var historyMessageByStr = await redisDb.StringGetAsync($"HistoryMessage:HistoryGroupChat:HistoryGroupChat_{id}_{userId}");
                var historyMessage = JsonConvert.DeserializeObject<List<MessageModel>>(historyMessageByStr.ToString());
                if (historyMessage != null)
                {
                    messageList.AddRange(historyMessage);
                }

                //------------------------------------------------------------------------------------------------------------------------
                // 取得未讀訊息
                var viewedTime = _chitChitContext.GroupReadMessage.Where(r => r.GroupChatId == id && r.UserId == userId).FirstOrDefault();
                if (viewedTime != null)
                {
                    // 已有已讀
                    lastViewedTime = viewedTime.ReadByTime;
                    // 起始時間
                    var startStamp = viewedTime.ReadByTime.ToUnixTimeMilliseconds();
                    // 未讀訊息
                    var sortedSet = await redisDb.SortedSetRangeByScoreAsync($"GroupChat:GroupChatId_{id}", startStamp, double.PositiveInfinity);

                    foreach (var r in sortedSet)
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
                    // 沒有已讀資料現在建立
                    lastViewedTime = DateTimeOffset.Now;
                    var newReadMessageObj = new GroupReadMessage
                    {
                        UserId = userId,
                        GroupChatId = id,
                        ReadByTime = DateTimeOffset.Now
                    };

                    // 新增資料
                    _chitChitContext.GroupReadMessage.Add(newReadMessageObj);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();


                    // 每次取得數量
                    int pageSize = 50;
                    // 起始值
                    int start = (1 - 1) * pageSize;
                    // 結束值
                    int stop = start + pageSize - 1;
                    // 取最新50筆放進去
                    var sortedSet = await _redisService.GetDatabase().SortedSetRangeByRankWithScoresAsync($"GroupChat:GroupChatId_{id}", start: start, stop: stop);
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
                    await redisDb.StringSetAsync($"HistoryMessage:HistoryGroupChat:HistoryGroupChat_{id}", jsonStr);
                }
                
                // 回傳資料
                var response = new ChatMessageModel
                {
                    ChatId = id,
                    ChatType = ChatType.GroupChat,
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
        /// 查詢歷史聊天訊息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="historyTime"></param>
        /// <returns></returns>
        [HttpGet("HistoryGroupChats/{id}")]
        public async Task<IActionResult> HistoryGroupChats(int id, [FromQuery] DateTimeOffset historyTime)
        {
            string funcFrom = "Controllers/GroupChatroomController/HistoryGroupChats/{id}:[GET]";
            try
            {
                string message = "";
                // 先確認有無該聊天室
                var targetChat = _chitChitContext.GroupChat.Where(g => g.Id == id).FirstOrDefault();
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
                var result = await _redisService.GetDatabase().SortedSetRangeByScoreAsync($"GroupChat:GroupChatId_{id}", start: minScore, stop: maxScore, exclude: Exclude.Stop, order: Order.Descending, skip: 0, take: count);
                foreach (var item in result)
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

        ///// <summary>
        ///// 查詢特定群組的聊天內容
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="userId"></param>
        ///// <param name="pageNumber"></param>
        ///// <returns></returns>
        //[HttpGet("GroupChats/{id}/GroupMessages")]
        //public async Task<IActionResult> GroupMessages(int id, [FromQuery] string userId, int pageNumber)
        //{
        //    string funcFrom = "Controllers/GroupChatroomController/GroupChats/{id}/GroupMessages:[GET]";

        //    try
        //    {
        //        string message = "";



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
        /// 建立群組聊天
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("GroupChats")]
        public IActionResult GroupChats([FromBody] PostGroupChat data)
        {
            string funcFrom = "Controllers/GroupChatroomController/GroupChats:[POST]";

            try
            {
                string message = "";
                // 建立群組資料
                var newGroup = new GroupChat
                {
                    GroupName = data.GroupName ?? "未命名群組",
                    UserId = data.UserId,
                    RecordTime = DateTimeOffset.Now
                };
                // 加入資料庫並儲存更新
                _chitChitContext.GroupChat.Add(newGroup);
                _chitChitContext.SaveChanges();

                // 遍歷使用者串列加入群組
                foreach (var user in data.JoinUserList)
                {
                    // 辨認是否為創辦人 
                    if (user != data.UserId)
                    {
                        // 一般使用者
                        var newGroupRel = new GroupRel
                        {
                            UserId = user,
                            GroupChatId = newGroup.Id,
                            JoinGroupTime = DateTimeOffset.Now,
                            Role = GroupRole.User
                        };
                        _chitChitContext.GroupRel.Add(newGroupRel);
                    }
                    else
                    {
                        // 創建者
                        // 一般使用者
                        var newGroupRel = new GroupRel
                        {
                            UserId = user,
                            GroupChatId = newGroup.Id,
                            JoinGroupTime = DateTimeOffset.Now,
                            Role = GroupRole.Admin
                        };
                        _chitChitContext.GroupRel.Add(newGroupRel);
                    }
                }

                _chitChitContext.SaveChanges();


                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }

        }


        /// <summary>
        /// 修改群組
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("GroupChats/{id}")]
        public IActionResult GroupChats(int id, [FromBody] PatchGroupChat data)
        {
            string funcFrom = "Controllers/GroupChatroomController/GroupChats/{id}:[PATCH]";

            try
            {
                string message = "";


                // 查詢資料庫與相符合的id
                var targetGroupChat = _chitChitContext.GroupChat.Where(c => c.Id == id).FirstOrDefault();
                if (targetGroupChat != null)
                {
                    targetGroupChat.GroupName = data.GroupName;

                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = "" });
                }

                // 回傳結果 - 查無此群組
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }

        }


        /// <summary>
        /// 刪除群組
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("GroupChats/{id}")]
        public IActionResult GroupChats(int id)
        {
            string funcFrom = "Controllers/GroupChatroomController/GroupChats/{id}:[DELETE]";

            try
            {
                string message = "";

                // 查詢資料庫與相符合的id
                var targetGroupChat = _chitChitContext.GroupChat.Where(c => c.Id == id).FirstOrDefault();
                if (targetGroupChat != null)
                {
                    _chitChitContext.GroupChat.Remove(targetGroupChat);

                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }

                // 回傳結果 - 查無此群組
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }

        }

        /// <summary>
        /// 查詢用戶的所有好友清單
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("Friends")]
        public IActionResult Friends([FromQuery] string userId)
        {
            string funcFrom = "Controllers/GroupChatroomController/Friends:[GET]";

            try
            {
                string message = "";

                // 檢查有無該用戶
                var exist = _chitChitContext.ChitChitUser.Any(u => u.Id == userId);
                if (exist)
                {
                    // 用戶為 UserId 1的好友 排除黑名單的好友
                    var friend1 = from friendRel in _chitChitContext.FriendRel
                                  join user in _chitChitContext.ChitChitUser on friendRel.UserId2 equals user.Id
                                  where friendRel.UserId1 == userId && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                                  select new
                                  {
                                      Id = user.Id,
                                      FullUserName = user.FullUserName,
                                      Avatar = user.Avatar
                                  };
                    // 用戶為 UserId 2的好友 排除黑名單的好友
                    var friend2 = from friendRel in _chitChitContext.FriendRel
                                  join user in _chitChitContext.ChitChitUser on friendRel.UserId1 equals user.Id
                                  where friendRel.UserId2 == userId && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                                  select new
                                  {
                                      Id = user.Id,
                                      FullUserName = user.FullUserName,
                                      Avatar = user.Avatar
                                  };
                    // 合併為一個陣列
                    var friendList = friend1.Union(friend2).ToList();



                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = friendList });
                }

                // 回傳結果 - 查無此用戶
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }
    }
    public class PostGroupChat
    {

        public string? GroupName { get; set; }

        public string UserId { get; set; } = null!;

        public List<string> JoinUserList { get; set; } = new List<string>();

    }

    public class ReadGroupChatsModel
    {
        public int ChatId { get; set; }
        
        public string? ChatName { get; set; }

        public ChatType ChatType { get; set; }

        public DateTimeOffset RecordTime { get; set; }

        public string? LastMessage { get; set; }

        public long UnReadMessage { get; set; }
    }

    public class GroupMessageModel
    {
        public string MessageContent { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string FullUserName { get; set; } = null!;

        public int GroupChatId { get; set; }

        public DateTimeOffset RecordTime { get; set; }
    }

    public class PatchGroupChat
    {
        public string? GroupName { get; set; }
    }
}
