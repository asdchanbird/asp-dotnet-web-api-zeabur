using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ChitChit.Controllers.SocialManagement
{
    /// <summary>
    /// 貼文評論
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "SocialManagement")]
    public class PostCommentController : ControllerBase
    {
        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public PostCommentController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }

        /// <summary>
        /// 新增留言
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("Comments")]
        public IActionResult Comments([FromBody] CommentModel data)
        {
            string funcFrom = "Controllers/PostCommentController/Comments:[POST]";
            try
            {
                string message = "";

                // 檢查有無該貼文id
                var exist = _chitChitContext.Post.Any(c => c.Id == data.PostId);
                if (exist)
                {
                    // 建立新留言物件
                    var newComment = new PostComment
                    {
                        UserId = data.UserId,
                        FullUserName = data.FullUserName,
                        PostId = data.PostId,   
                        CommentContent = data.CommentContent
                    };
                
                    // 新增留言
                    _chitChitContext.PostComment.Add(newComment);
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

        /// <summary>
        /// 編輯留言
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("Comments/{id}")]
        public IActionResult Comments(int id, [FromBody] PatchCommentModel data)
        {
            string funcFrom = "Controllers/PostCommentController/Comments:[PATCH]";
            try
            {
                string message = "";
                
                // 依據留言id查詢該留言
                var target = _chitChitContext.PostComment.Where(c => c.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 更改內容
                    target.CommentContent = data.CommentContent;
                    
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = "" });
                }
            

                // 回傳結果 - 查無留言
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
        /// 查詢特定貼文的所有留言
        /// </summary>
        /// <returns></returns>
        [HttpGet("CommentsByPost")]
        public IActionResult CommentsByPost([FromQuery]int postId)
        {
            string funcFrom = "Controllers/PostCommentController/CommentsByPost:[GET]";
            try
            {
                string message = "";

                // 檢查有無該貼文id
                var exist = _chitChitContext.Post.Any(c => c.Id == postId);
                if (exist)
                {
                    // 查詢特定貼文的所有留言(排序由新到舊)
                    var response = from comment in _chitChitContext.PostComment
                                   join post in _chitChitContext.Post on comment.PostId equals post.Id
                                   join user in _chitChitContext.ChitChitUser on comment.UserId equals user.Id
                                   where comment.PostId == postId
                                   orderby comment.RecordTime descending
                                   select new
                                   {
                                       Id = comment.Id,
                                       PostId = comment.PostId,
                                       UserId = comment.UserId,
                                       Avatar = user.Avatar,
                                       FullUserName = comment.FullUserName,
                                       CommentContent = comment.CommentContent,
                                       RecordTime = comment.RecordTime
                                   };

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = response });
                }

                // 回傳結果 - 查無留言
                message = "NotFound";
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

    public class CommentModel
    {
        public string UserId { get; set; } = null!;

        public string? FullUserName { get; set; }

        public int PostId { get; set; }

        public string? CommentContent { get; set; }
    }

    public class PatchCommentModel
    {
        public string? CommentContent { get; set; }
    }
}
