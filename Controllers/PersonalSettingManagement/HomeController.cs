using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ChitChit.Controllers.PersonalSettingManagement
{
    /// <summary>
    /// 首頁
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    //[Authorize]
    [ApiExplorerSettings(GroupName = "PersonalSettingManagement")]
    
    public class HomeController : ControllerBase
    {
        // 資料庫上下文
        private readonly chitchitContext _chitChitContext;
        public HomeController(chitchitContext chitChitContext)
        {
            _chitChitContext = chitChitContext;
        }
        //[Authorize]
        [HttpGet("Read")]
        public IActionResult Read()
        {
            string funcFrom = "Controllers/HomeController/Read:[GET]";

            try
            {
                
                // 回傳結果 - 成功
                string message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = "" });
            }
            catch(Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });

            } 
        }

        /// <summary>
        /// 查詢最新消息
        /// </summary>
        /// <returns></returns>
        [HttpGet("News")]
        public IActionResult News()
        {
            string funcFrom = "Controllers/HomeController/News:[GET]";

            try
            {
                string message = "";

                int sizes = 10;
                
                var news = _chitChitContext.News.OrderByDescending(n => n.RecordTime).Take(sizes).ToList();
                if (news != null)
                {

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = news });
                }
               

                // 回傳結果 - 查無此群組
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new{} });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 新增最新消息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("News")]
        public IActionResult News([FromBody] NewsModel data) 
        {
            string funcFrom = "Controllers/HomeController/News:[POST]";

            try
            {
                string message = "";

                // 建立新的最新消息物件
                var newNews = new News
                {
                    NewTitle = data.NewsTitle,
                    NewsContent = data.NewsContent,
                    NewsImgByJson = data.NewsImg,
                    RecordTime = DateTimeOffset.Now
                };

                // 新增資料
                _chitChitContext.News.Add(newNews);
                // 儲存資料庫
                _chitChitContext.SaveChanges();


                // 回傳結果 - 成功
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new{} });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }


        /// <summary>
        /// 編輯最新消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPatch("News/{id}")]
        public IActionResult News(int id, [FromBody] NewsModel data)
        {
            string funcFrom = "Controllers/HomeController/News/{id}:[PATCH]";

            try
            {
                string message = "";

                // 查詢有無Id的最新消息
                var target = _chitChitContext.News.Where(n => n.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 更新資料
                    target.NewTitle = data.NewsTitle;
                    target.NewsContent = data.NewsContent;
                    target.NewsImgByJson = data.NewsImg;
                    target.RecordTime = DateTimeOffset.Now;

                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new{} });
                }

                // 回傳結果 - 查無最新消息
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { state = "normal", message, result = new{} });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 刪除最新消息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("News/{id}")]
        public IActionResult News(int id)
        {
            string funcFrom = "Controllers/HomeController/News/{id}:[DELETE]";

            try
            {
                string message = "";

                // 查詢有無該id最新消息
                var target = _chitChitContext.News.Where(N => N.Id == id).FirstOrDefault();
                if (target != null)
                {

                    // 刪除資料
                    _chitChitContext.News.Remove(target);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }
               

                // 回傳結果 - 查無最新消息
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

    public class NewsModel
    {
        public string? NewsTitle { get; set;}

        public string? NewsContent { get; set; }

        public List<string> NewsImg { get; set; } = new List<string>();

    }

}
