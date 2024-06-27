using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ChitChit.Controllers.ChatManagement
{
    /// <summary>
    /// 聊天室管理
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "ChatManagement")]
    public class ChatManagementController : ControllerBase
    {

        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public ChatManagementController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }


        /// <summary>
        /// 查詢用戶的所有好友
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("FriendsByUser")]
        public IActionResult FriendsByUser([FromQuery]string userId)
        {
            string funcFrom = "Controllers/FriendManagementController/FriendsByUser";
            try
            {
                string message = "";

                // 查詢 UserId1 為用戶的好友
                var friend1 = from friend in _chitChitContext.FriendRel
                              where friend.UserId1 == userId && friend.RelationStatus == Status.Success
                              select friend;
                // 查詢 UserId2 為用戶的好友
                var friend2 = from friend in _chitChitContext.FriendRel
                              where friend.UserId2 == userId && friend.RelationStatus == Status.Success
                              select friend;
                // 合併資料
                var responseList = friend1.Union(friend2).ToList();


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
    }
}
