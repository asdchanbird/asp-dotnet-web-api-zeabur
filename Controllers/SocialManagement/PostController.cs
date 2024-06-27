using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ChitChit.Controllers.SocialManagement
{
    /// <summary>
    /// 貼文管理    
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "SocialManagement")]
    public class PostController : ControllerBase
    {
        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public PostController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }


        /// <summary>
        /// 查詢用戶的所有好友貼文
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [HttpGet("Posts")]
        public IActionResult Posts([FromQuery]string userId, int pageNumber)
        {
            string funcFrom = "Controllers/SocialManagementController/Posts:[GET]";
            try
            {
                string message = "";
                // 顯示筆數
                int pageSize = 10;
                // 回傳串列
                var responseList = new List<GetPostModel>();

                // 用戶為 UserId 1的好友 排除黑名單的好友
                var friend1 = from friendRel in _chitChitContext.FriendRel
                              join user in _chitChitContext.ChitChitUser on friendRel.UserId2 equals user.Id
                              where friendRel.UserId1 == userId && friendRel.RelationStatus == Status.Success && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                              select friendRel.UserId2;
                // 用戶為 UserId 2的好友 排除黑名單的好友
                var friend2 = from friendRel in _chitChitContext.FriendRel
                              join user in _chitChitContext.ChitChitUser on friendRel.UserId1 equals user.Id
                              where friendRel.UserId2 == userId && friendRel.RelationStatus == Status.Success && !_chitChitContext.FriendBlocked.Any(b => b.BlockerId == userId && b.BlockedId == user.Id)
                              select friendRel.UserId1;
                // 合併為一個陣列
                var friendList = friend1.Union(friend2).ToList();

                //// 查詢 UserId1 為用戶的好友
                //var friend1 = from friend in _chitChitContext.FriendRel
                //              where friend.UserId1 == userId && friend.RelationStatus == Status.Success
                //              select friend.UserId2;
                //// 查詢 UserId2 為用戶的好友
                //var friend2 = from friend in _chitChitContext.FriendRel
                //              where friend.UserId2 == userId && friend.RelationStatus == Status.Success
                //              select friend.UserId1;
                //// 合併兩個好友列表並返回
                //var allFrinedList = friend1.Union(friend2).ToList();
               

                // 查詢貼文中是否有包含好友Id的文章
                var posts = (from post in _chitChitContext.Post
                             join user in _chitChitContext.ChitChitUser on post.UserId equals user.Id
                             where friendList.Contains(post.UserId) 
                             orderby post.RecordTime descending
                             select new
                             {
                                 PostId = post.Id,
                                 UserID = user.Id,
                                 FullUserName = user.FullUserName,
                                 Avatar = user.Avatar,
                                 PostContent = post.PostContent,
                                 HashTag = post.HashtagByJson,
                                 RecordTime = post.RecordTime
                             })
                            .Skip(pageSize * (pageNumber - 1))
                            .Take(pageSize)
                            .ToList();
                
                // 處裡每個貼文的留言數. 點讚數
                foreach(var post in posts)
                {
                    var isLike = false;
                    
                    // 留言數
                    var commentCount = (from comment in _chitChitContext.PostComment
                                       where comment.PostId == post.PostId
                                       select comment).Count();
                    // 點讚數
                    var likeCount = (from like in _chitChitContext.PostLike
                                    where like.PostId == post.PostId
                                    select like).Count();
                    // 貼文圖片儲放區
                    var postImgs = (from img in _chitChitContext.PostImageRepo
                                   where img.PostId == post.PostId
                                   select img.ImgRepoImagesByJson).FirstOrDefault();
                    // 是否點讚
                    var targetLike = _chitChitContext.PostLike.Where(l => l.PostId == post.PostId && l.UserId == userId).FirstOrDefault();
                    if (targetLike != null)
                    {
                        isLike = true;
                    }

                    // 加入回傳串列中
                    responseList.Add(new GetPostModel
                    {
                        PostId = post.PostId,
                        PostContent = post.PostContent,
                        UserId = post.UserID,
                        FullUserName = post.FullUserName,
                        Avatar = post.Avatar,
                        Hashtag = post.HashTag,
                        PostImg = postImgs ?? new List<string>(),
                        CommentCount = commentCount,
                        LikeCount = likeCount,
                        IsLike = isLike,
                        RecordTime = post.RecordTime,
                    });
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

        /// <summary>
        /// 查詢特定貼文
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("Posts/{id}")]
        public IActionResult Posts(int id)
        {
            string funcFrom = "Controllers/SocialManagementController/Posts/{id}:[GET]";
            try
            {
                string message = "";

                // 根據文章 Id 查詢該文章
                var targetPost = (from post in _chitChitContext.Post
                                  join user in _chitChitContext.ChitChitUser on post.UserId equals user.Id
                                  where post.Id == id
                                  orderby post.RecordTime descending
                                  select new
                                  {
                                      PostId = post.Id,
                                      UserID = user.Id,
                                      FullUserName = user.FullUserName,
                                      Avatar = user.Avatar,
                                      PostContent = post.PostContent,
                                      HashTag = post.HashtagByJson,
                                      RecordTime = post.RecordTime
                                  }).FirstOrDefault();
                // 確認有無該貼文
                if (targetPost != null)
                {
                    // 留言數
                    var commentCount = (from comment in _chitChitContext.PostComment
                                        where comment.PostId == targetPost.PostId
                                        select comment).Count();
                    // 點讚數
                    var likeCount = (from like in _chitChitContext.PostLike
                                     where like.PostId == targetPost.PostId
                                     select like).Count();
                    // 貼文圖片儲放區
                    var postImgs = (from img in _chitChitContext.PostImageRepo
                                    where img.PostId == targetPost.PostId
                                    select img.ImgRepoImagesByJson).FirstOrDefault();
                    // 建立新貼文物件
                    var response = new GetPostModel
                    {
                        PostId = targetPost.PostId,
                        PostContent = targetPost.PostContent,
                        UserId = targetPost.UserID,
                        FullUserName = targetPost.FullUserName,
                        Avatar = targetPost.Avatar,
                        Hashtag = targetPost.HashTag,
                        CommentCount = commentCount,
                        LikeCount = likeCount,
                        PostImg = postImgs ?? new List<string>(),
                        RecordTime = targetPost.RecordTime,
                    };

                      // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = response });
                }


                // 回傳結果 - 查無貼文
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

        /// <summary>
        /// 新增貼文
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("Posts")]
        public IActionResult Posts([FromBody] PostModel data)
        {
            string funcFrom = "Controllers/SocialManagementController/Posts:[POST]";
            try
            {
                string message = "";

                // 建立新貼文物件
                var newPost = new Post
                {
                    UserId = data.UserId,
                    PostContent = data.PostContent,
                    HashtagByJson = data.Hashtag,
                    RecordTime = DateTimeOffset.Now
                };

                // 新增貼文
                _chitChitContext.Post.Add(newPost);
                // 資料庫儲存更新
                _chitChitContext.SaveChanges();

                // 建立新貼文圖片存放區
                var newImgRepo = new PostImageRepo
                {
                    PostId = newPost.Id,
                    ImgRepoImagesByJson = data.PostImgs
                };
                // 新增貼文圖片存放區
                _chitChitContext.PostImageRepo.Add(newImgRepo);


                // 遍歷hashtag
                foreach(var tag in data.Hashtag)
                {
                    // 查詢該tag是否存在資料庫
                    var result = _chitChitContext.PostHashtag.Where(t => t.TagName == tag).FirstOrDefault();
                    if(result != null)
                    {
                        // 有存在
                        // 使用次數加 1
                        result.TagCount += 1;
                    }
                    else
                    {
                        //// 不存在
                        //// 建立新的資料
                        var newHashtag = new PostHashtag
                        {
                            TagName = tag,
                            TagCount = 1,
                            RecordTime = DateTimeOffset.Now
                        };
                        // 新增
                        _chitChitContext.PostHashtag.Add(newHashtag);
                    }
                }

                // 資料庫儲存更新
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
        /// 編輯貼文
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("Posts/{id}")]
        public IActionResult Post(int id, [FromBody] PostModel data)
        {
            string funcFrom = "Controllers/SocialManagementController/Posts/{id}:[PATCH]";
            try
            {
                string message = "";
                
                // 依據該Id尋找貼文
                var target = _chitChitContext.Post.Where(p => p.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 變更資料
                    target.PostContent = data.PostContent;
                    target.HashtagByJson = data.Hashtag;

                    // 更新圖片資料
                    var targetImgRepo = _chitChitContext.PostImageRepo.Where(p => p.PostId == id).FirstOrDefault();
                    if (targetImgRepo != null)
                    {
                        // 更新圖片
                        targetImgRepo.ImgRepoImagesByJson = data.PostImgs;
                    }
                    else
                    {
                        // 沒有就建立資料
                        var newImgRepo = new PostImageRepo()
                        {
                            PostId = id,
                            ImgRepoImagesByJson = data.PostImgs
                        };
                        // 加入資料
                        _chitChitContext.PostImageRepo.Add(newImgRepo);
                    }
                    
                    
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
        /// 刪除貼文
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("Posts/{id}")]
        public IActionResult DeletePosts(int id)
        {
            string funcFrom = "Controllers/SocialManagementController/Posts/{id}:[DELETE]";
            try
            {
                string message = "";
                
                // 依據貼文id查詢該貼文
                var target = _chitChitContext.Post.Where(p => p.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 刪除貼文
                    _chitChitContext.Post.Remove(target);
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
        /// 查詢特定用戶的所有貼文
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("PostByUser")]
        public IActionResult PostByUser([FromQuery] string userId)
        {
            string funcFrom = "Controllers/SocialManagementController/PostByUser:[GET]";
            try
            {
                string message = "";

                var response = new List<GetPostModel>();
                
                // 檢測有無貼文
                var exist = _chitChitContext.Post.Any(p => p.UserId == userId);
                if (exist)
                {
                    // 根據用戶id查詢所有貼文(排序由新到舊)
                    var target = from post in _chitChitContext.Post
                                 join img in _chitChitContext.PostImageRepo on post.Id equals img.PostId
                                 join user in _chitChitContext.ChitChitUser on post.UserId equals user.Id
                                 where post.UserId == userId
                                 orderby post.RecordTime descending
                                 select new GetPostModel
                                 {
                                     PostId = post.Id,
                                     PostContent = post.PostContent,
                                     PostImg = img.ImgRepoImagesByJson,
                                     Hashtag = post.HashtagByJson,
                                     UserId = userId,
                                     FullUserName = user.FullUserName,
                                     Avatar = user.Avatar,
                                     RecordTime = post.RecordTime
                                 };
                    // 遍歷
                    foreach (var post in target)
                    {
                    
                        // 貼文評論數
                        int commentCount = (from comment in _chitChitContext.PostComment
                                            where comment.PostId == post.PostId
                                            select comment).Count();
                        // 貼文按讚數
                        int likeCount = (from like in _chitChitContext.PostLike
                                         where like.PostId == post.PostId
                                         select like).Count();

                        // 更新資料
                        post.CommentCount = commentCount;
                        post.LikeCount = likeCount;

                        response.Add(post);
                    }
                    

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = response });

                }

                // 回傳結果 - 查無貼文
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

        /// <summary>
        /// 查詢某用戶個人簡介
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("Profile/{id}")]
        public IActionResult Profile(string id)
        {
            string funcFrom = "Controllers/SocialManagementController/Profile/{id}:[GET]";
            try
            {
                string message = "";

                var target = _chitChitContext.ChitChitUser.Where(c => c.Id == id).Select(u => new ProfileModel
                {
                    Avatar = u.Avatar,
                    FullUserName = u.FullUserName,
                    StatusMessage = u.StatusMessage
                }).FirstOrDefault();

                if (target != null)
                {
                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = target });
                }

                // 回傳結果 - 查無用戶
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

        /// <summary>
        /// 編輯個人簡介
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("Profile/{id}")]
        public IActionResult Profile(string id, [FromBody] ProfileModel data)
        {
            string funcFrom = "Controllers/SocialManagementController/Profile/{id}:[PATCH]";
            try
            {
                string message = "";

                // 依據用戶id查詢該使用者資訊
                var user = _chitChitContext.ChitChitUser.Where(u => u.Id == id).FirstOrDefault();  
                if (user != null)
                {
                    // 有用戶
                    user.FullUserName = data.FullUserName;
                    user.Avatar = data.Avatar;
                    user.StatusMessage = data.StatusMessage;

                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }

                // 回傳結果 - 查無用戶
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

    public class PostModel
    {
        public string UserId { get; set; } = null!;

        public string? PostContent { get; set; }

        public List<string> Hashtag { get; set; } = new List<string>();

        public List<string> PostImgs { get; set; } = new List<string>();

    }

    public class ProfileModel
    {

        public string? FullUserName { get; set; }
    
        public string? Avatar { get; set; }

        public string? StatusMessage { get; set; }

    }
    
    public class GetPostModel
    {
        public int PostId { get; set; }
        public string? PostContent { get; set;}
        public string UserId { get; set; } = null!;
        public string? FullUserName { get; set;}
        public string? Avatar { get; set;}
        public List<string> Hashtag { get; set; } = new List<string>();
        public List<string> PostImg { get; set; } = new List<string>();
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        public Boolean IsLike { get; set; }
        public DateTimeOffset RecordTime { get; set; }
    }


}
