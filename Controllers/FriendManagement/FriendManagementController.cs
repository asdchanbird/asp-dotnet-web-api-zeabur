using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ChitChit.Controllers.FriendManagement
{
    /// <summary>
    /// 好友管理
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "FriendManagement")]
    public class FriendManagementController : ControllerBase
    {
        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public FriendManagementController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }

        /// <summary>
        /// 查詢用戶的所有好友清單
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("Friends")]
        public IActionResult Friends([FromQuery] string userId)
        {
            string funcFrom = "Controllers/FriendManagementController/Friends:[GET]";
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
                                  where friendRel.UserId1 == userId && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id) && friendRel.RelationStatus == Status.Success
                                  select new
                                  {
                                      Id = user.Id,
                                      FullUserName = user.FullUserName,
                                      Avatar = user.Avatar,
                                      Email = user.Email,
                                      StatusMessage = user.StatusMessage
                                  };
                    // 用戶為 UserId 2的好友 排除黑名單的好友
                    var friend2 = from friendRel in _chitChitContext.FriendRel
                                  join user in _chitChitContext.ChitChitUser on friendRel.UserId1 equals user.Id
                                  where friendRel.UserId2 == userId && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id) && friendRel.RelationStatus == Status.Success
                                  select new
                                  {
                                      Id = user.Id,
                                      FullUserName = user.FullUserName,
                                      Avatar = user.Avatar,
                                      Email = user.Email,
                                      StatusMessage = user.StatusMessage
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

                //// 查詢用戶是 UserId 1時的好友
                //var friend1 = _chitChitContext.FriendRel.Where(u => u.UserId1 == userId).Join(_chitChitContext.ChitChitUser, f => f.UserId2, c => c.Id, (x, y) => new
                //{
                //    FriendRel_Id = x.Id,
                //    FrinedRel_Info = y,
                //    FriendRel_Time = x.RecordTime
                //}).ToList();

                //// 查詢用戶是 UserId 2時的好友
                //var friend2 = _chitChitContext.FriendRel.Where(u => u.UserId2 == userId).Join(_chitChitContext.ChitChitUser, f => f.UserId1, c => c.Id, (x, y) => new
                //{
                //    FriendRel_Id = x.Id,
                //    FrinedRel_Info = y,
                //    FriendRel_Time = x.RecordTime
                //}).ToList();

                //// 合併兩個好友列表並返回
                //var allFrinedList = friend1.Union(friend2).ToList();


                //// 回傳結果 - 成功
                //message = "Success";
                //Log.Information($"{funcFrom}->{message}");
                //return Ok(new { state = "normal", message, result = allFrinedList });

            }
            catch(Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 查詢用戶
        /// </summary>
        /// <returns></returns>
        [HttpGet("Friends/{id}")]
        public IActionResult SearchFriends(string id)
        {
            string funcFrom = "Controllers/FriendManagementController/Friends/{id}:[GET]";
            try
            {
                string message = "";

                var targetUser = _chitChitContext.ChitChitUser.Where(u => u.Id == id).Select(u => new {
                    Id = u.Id,
                    FullUserName = u.FullUserName,
                    Email = u.Email,    
                    Avatar = u.Avatar,
                    StatusMessage = u.StatusMessage
                }).FirstOrDefault();

                if (targetUser != null)
                {
                    
                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = targetUser });
                }
                // 回傳結果 - 查無用戶
                message = "UnFoundUser";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new { } });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 查詢請求好友邀請的用戶
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("SearchRequestFriends")]
        public IActionResult SearchRequestFriends([FromQuery]string userId)
        {
            string funcFrom = "Controllers/FriendManagementController/SearchRequestFriends:[GET]";
            try
            {
                string message = "";

                // 查巡請求好友邀請的用戶
                var requestUser = from request in _chitChitContext.FriendRequest
                                   join user in _chitChitContext.ChitChitUser on request.RequesterId equals user.Id
                                   where request.ReceiverId == userId && request.RelationStatus == Status.UnAccepted
                                   select new
                                   {
                                       RequestId = request.Id,
                                       RequestSenderId = request.RequesterId,
                                       RequestSenderName = user.FullUserName,
                                       RequestEmail = user.Email,
                                       RequestStatusMessage = user.StatusMessage,
                                       RequestAvatar = user.Avatar,
                                       RequestTime = request.RecordTime,
                                       RequestStatus = request.RelationStatus
                                   };

                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = requestUser });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 申請好友
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("RequestFriends")]
        public IActionResult RequestFriends([FromBody] RequestBody data)
        {
            string funcFrom = "Controllers/FriendManagementController/RequestFriends:[POST]";
            try
            {
                string message = "";
                
                // 檢查有無好友關係
                var target = _chitChitContext.FriendRel.Where(f => f.UserId1 == data.RequesterId && f.UserId2 == data.RequesterId).FirstOrDefault();
                if (target == null)
                {
                    // 無好友關係
                    // 建立新的好友申請
                    FriendRequest newRequest = new FriendRequest()
                    {
                        RequesterId = data.RequesterId,
                        ReceiverId = data.ReceiverId,
                        RecordTime = DateTimeOffset.Now,
                        RelationStatus = Status.UnAccepted
                    };
                    // 建立新好友關係物件
                    FriendRel newfriendRel = new FriendRel()
                    {
                        UserId1 = data.RequesterId,
                        UserId2 = data.ReceiverId,
                        RecordTime = DateTimeOffset.Now,
                        RelationStatus = Status.UnAccepted
                    };
                    
                    // 新增好友申請
                    _chitChitContext.FriendRequest.Add(newRequest);
                    _chitChitContext.FriendRel.Add(newfriendRel);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }
                else
                {

                    // 有好友關係
                    switch (target.RelationStatus)
                    {
                        case Status.UnAccepted:
                            // 回傳結果 - 尚未接受狀態
                            message = "StatusUnAccepted";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { state = "normal", message, result = new { } });
                        case Status.Reject:
                            // 回傳結果 - 拒絕狀態
                            message = "StatusReject";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { state = "normal", message, result = new { } });
                        case Status.Success:
                            // 回傳結果 - 好友狀態
                            message = "StatusSuccess";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { state = "normal", message, result = new { } });
                        case Status.Blocked:
                            // 回傳結果 - 黑名單狀態
                            message = "StatusBlocked";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { state = "normal", message, result = new { } });
                        default:
                            // 回傳結果 - 查無狀態
                            message = "NotFound";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { state = "normal", message, result = new { } });

                    }
                }

            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 改變申請好友狀態
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("ChangeFriendStatus")]
        public IActionResult ChangeFriendStatus([FromBody] RequestStatusBody data)
        {
            string funcFrom = "Controllers/FriendManagementController/ChangeFriendStatus:[PATCH]";
            try
            {
                string message = "";

                // 搜尋目標申請
                var targetFriendRel = _chitChitContext.FriendRel.Where(f => f.UserId1 == data.RequesterId && f.UserId2 == data.ReceiverId).OrderByDescending(f => f.RecordTime).FirstOrDefault();
                if (targetFriendRel != null)
                {
                    // 改變申請狀態
                    targetFriendRel.RelationStatus = data.RequestStatus;

                    // 如果是接受 建立私聊
                    if (targetFriendRel.RelationStatus == Status.Success)
                    {
                        // 檢查是否已有私聊
                        var exist = _chitChitContext.PrivateChat.Any(p => p.FriendRelId == targetFriendRel.Id);
                        if (!exist)
                        {
                            // 建立私聊
                            var newChatPrivate = new PrivateChat()
                            {
                                FriendRelId = targetFriendRel.Id,
                                RecordTime = DateTimeOffset.Now,
                                IsPinned = false,
                            };

                            // 新增資料
                            _chitChitContext.PrivateChat.Add(newChatPrivate);
                            // 儲存資料庫
                            _chitChitContext.SaveChanges();

                        }
                    }

                    // 好友申請更新
                    var targetRequest = _chitChitContext.FriendRequest.Where(f => f.RequesterId == data.RequesterId && f.ReceiverId == data.ReceiverId).FirstOrDefault();
                    if (targetRequest != null)
                    {
                        // 更新資料
                        targetRequest.RelationStatus = data.RequestStatus;
                        targetRequest.ResponseTime = DateTimeOffset.Now;

                        // 儲存資料庫
                        _chitChitContext.SaveChanges();
                    }
                    else
                    {
                        // 回傳結果 - 查無好友申請
                        message = "NoFound";
                        Log.Information($"{funcFrom}->{message}");
                        return Ok(new { state = "normal", message, result = new { } });
                    }



                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }
                // 回傳結果 - 查無好友關係
                message = "NoFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new { } });

            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 刪除好友關係
        /// </summary>
        /// <param name="id"></param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        [HttpDelete("Friends/{id}")]
        public IActionResult Friends(string id, [FromQuery] string friendId)     
        {
            string funcFrom = "Controllers/FriendManagementController/Friends/{id}:[DELETE]";
            try
            {
                string message = "";

                // 查詢用戶是 UserId 1時的好友
                var friend1 = (from rel in _chitChitContext.FriendRel
                              where rel.UserId1 == id && rel.UserId2 == friendId
                               select rel).FirstOrDefault();

                //// 查詢用戶是 UserId 2時的好友
                var friend2 = (from rel in _chitChitContext.FriendRel
                              where rel.UserId2 == id && rel.UserId1 == friendId
                               select rel).FirstOrDefault();
                

                if (friend1 != null)
                {
                    _chitChitContext.FriendRel.Remove(friend1);
                }

                if (friend2 != null)
                {
                    _chitChitContext.FriendRel.Remove(friend2);
                }

                // 查詢用戶是 UserId 1時的好友
                var friendRequest1 = (from request in _chitChitContext.FriendRequest
                                      where request.RequesterId == id && request.ReceiverId == friendId
                                      select request).FirstOrDefault();
                //// 查詢用戶是 UserId 2時的好友
                var friendRequest2 = (from request in _chitChitContext.FriendRequest
                                      where request.ReceiverId == id && request.ReceiverId == friendId
                                      select request).FirstOrDefault();
                if (friendRequest1 != null)
                {
                    _chitChitContext.FriendRequest.Remove(friendRequest1);
                }

                if (friendRequest2 != null)
                {
                    _chitChitContext.FriendRequest.Remove(friendRequest2);
                }


                // 儲存資料庫
                _chitChitContext.SaveChanges();


                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new { } });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }
    }

    public class RequestBody
    {
        public string RequesterId { get; set; } = null!;
        
        public string ReceiverId { get; set;} = null!;
    }

    public class RequestStatusBody: RequestBody
    {
        public Status RequestStatus { get; set; }
    }

}
