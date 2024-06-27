using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ChitChit.Controllers.SocialManagement
{

    /// <summary>
    /// 貼文按讚
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "SocialManagement")]
    public class PostLikeController : ControllerBase
    {

        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public PostLikeController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }

        /// <summary>
        /// 新增某貼文的按讚
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("Likes")]
        public IActionResult Likes([FromBody] LikeModel data)
        {
            string funcFrom = "Controllers/SocialManagementController/Likes:[POST]";
            try
            {
                string message = "";

                // 先確認有無該貼文id
                bool exist = _chitChitContext.Post.Any(p => p.Id == data.PostId);
                if (exist)
                {
                    // 建立新物件
                    var newLike = new PostLike
                    {
                        PostId = data.PostId,
                        UserId = data.UserId,
                        RecordTime = DateTimeOffset.Now
                    };
                    // 新增按讚
                    _chitChitContext.PostLike.Add(newLike);
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = "" });
                }

                // 回傳結果 - 查無貼文
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
        /// 刪除某貼文的按讚
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("Likes")]
        public IActionResult DeleteLikes([FromQuery] int postId, string userId )
        {
            string funcFrom = "Controllers/SocialManagementController/Likes:[DELETE]";
            try
            {
                string message = "";

                // 依據貼文id與用戶Id查詢該按讚
                var target = _chitChitContext.PostLike.Where(l => l.PostId == postId && l.UserId == userId).FirstOrDefault();
                if (target != null)
                {
                    // 刪除目標
                    _chitChitContext.PostLike.Remove(target);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();


                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = "" });
                }
                
                // 回傳結果 - 查無貼文
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

    public class LikeModel
    {
        public int PostId { get; set; }

        public string UserId { get; set; } = null!;
    }
}
