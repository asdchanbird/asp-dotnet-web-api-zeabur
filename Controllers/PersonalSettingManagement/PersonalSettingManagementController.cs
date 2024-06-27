using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using System.Text;

namespace ChitChit.Controllers.PersonalSettingManagement
{
    /// <summary>
    /// 個人化設定
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "PersonalSettingManagement")]
    public class PersonalSettingManagementController : ControllerBase
    {
        // Identity - 使用者管理器
        private readonly UserManager<ChitChitUser> _userManager;
        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        public PersonalSettingManagementController(chitchitContext chitChitContext, UserManager<ChitChitUser> userManager)
        {
            _chitChitContext = chitChitContext;
            _userManager = userManager;
        }


        /// <summary>
        /// 查詢個人簡介
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("Profile/{id}")]
        public IActionResult Profile(string id)
        {
            string funcFrom = "Controllers/PersonalSettingManagementController/Profile/{id}:[GET]";
            try
            {
                string message = "";

                // 依據用戶id查詢個人資訊
                var target = _chitChitContext.ChitChitUser.Where(u => u.Id == id).FirstOrDefault();
                if (target != null)
                {
                    var user = new ProfileModel
                    {
                        FullUserName = target.UserName,
                        PhoneNumber = target.PhoneNumber,
                        Birthday = target.Birthday,
                        Avatar = target.Avatar,
                        StatusMessage = target.StatusMessage,
                    };

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = user });
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
            string funcFrom = "Controllers/PersonalSettingManagementController/Profile/{id}:[PATCH]";
            try
            {
                string message = "";

                // 依據用戶id查詢該名用戶
                var target = _chitChitContext.ChitChitUser.Where(u => u.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 更新資料
                    target.FullUserName = data.FullUserName;
                    target.PhoneNumber = data.PhoneNumber;
                    target.Birthday = data.Birthday;    
                    target.Avatar = data.Avatar;
                    target.StatusMessage = data.StatusMessage;
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

        /// <summary>
        /// 重設密碼
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("ChangePasswordModel")]
        public async Task<IActionResult> ResetPassword([FromBody] ChangePasswordModel data)
        {
            string funcFrom = "Controllers/PersonalSettingManagementController/ChangePasswordModel:[POST]";
            try
            {
                string message = "";

                // 尋找相符的email
                var user = await _userManager.FindByEmailAsync(data.Email);
                if (user != null)
                {
                    // 確認密碼與確認密碼相同
                    if (data.NewPassword == data.ConfirmPassword)
                    {

                        // 重設密碼
                        var resetPasswordResult = await _userManager.ChangePasswordAsync(user, data.OldPassword, data.NewPassword);
                        Console.WriteLine(resetPasswordResult);
                        if (resetPasswordResult.Succeeded)
                        {
                            // 回傳結果 - 重設密碼成功
                            message = "Success";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { status = "normal", message, result = new { } });

                        }
                        // 回傳結果 - 重設密碼失敗
                        message = "ResetFail";
                        Log.Information($"{funcFrom}->{message}");
                        return Ok(new { status = "normal", message, result = new { } });
                    }
                    // 回傳結果 - 密碼與確認密碼不符
                    message = "PasswordNotMatch";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }
                // 回傳結果 - 信箱錯誤
                message = "EmailFail";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { status = "normal", message, result = new { } });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }
        
        
        /// <summary>
        /// 查詢用戶的黑名單列表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("BlockedUsers")]    
        public IActionResult BlockedUsers([FromQuery] string userId)
        {
            string funcFrom = "Controllers/PersonalSettingManagementController/BlockedUsers:[GET]";
            try
            {
                string message = "";

                // 檢查有無該名使用者
                var exist = _chitChitContext.ChitChitUser.Any(u => u.Id == userId);
                if (exist)
                {
                    var response = _chitChitContext.FriendBlocked.Where(b => b.BlockerId == userId).ToList();

                    

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = response });

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
        /// 新增黑名單
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("BlockedUsers")]
        public IActionResult BlockedUsers([FromBody] BlockedModel data)
        {
            string funcFrom = "Controllers/PersonalSettingManagementController/BlockedUsers:[POST]";
            try
            {
                string message = "";

                var exist1 = _chitChitContext.ChitChitUser.Any(x => x.Id == data.BlockedId);
                var exist2 = _chitChitContext.ChitChitUser.Any(x => x.Id == data.BlockerId);

                // 檢查用戶1與2都存在
                if (exist1 && exist2)
                {
                    // 建立新封鎖物件
                    var newBlocked = new FriendBlocked
                    {
                        BlockerId = data.BlockerId,
                        BlockedId = data.BlockedId,
                        RecordTime = DateTimeOffset.Now
                    };
                    // 新增資料
                    _chitChitContext.FriendBlocked.Add(newBlocked);
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

        /// <summary>
        /// 解除某用戶的黑名單
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("BlockedUsers/{id}")]
        public IActionResult BlockedUsers(int id)
        {
            string funcFrom = "Controllers/PersonalSettingManagementController/BlockedUsers/{id}:[DELETE]";
            try
            {
                string message = "";

                // 依據封鎖id查詢有無該資料
                var target = _chitChitContext.FriendBlocked.Where(b => b.Id == id).FirstOrDefault();
                if (target != null)
                {
                    // 移除資料
                    _chitChitContext.FriendBlocked.Remove(target);
                    // 儲存資料庫
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { state = "normal", message, result = new { } });
                }

                // 回傳結果 - 查無資料
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

    public class ProfileModel
    {
        public string? FullUserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Birthday { get; set; }
        public string? Avatar { get; set; }
        public string? StatusMessage { get; set; }

    }

    /// <summary>
    /// 重設密碼Model
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// 信箱
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// 舊密碼
        /// </summary>
        public string OldPassword { get; set; } = null!;

        /// <summary>
        /// 密碼
        /// </summary>
        public string NewPassword { get; set; } = null!;

        /// <summary>
        /// 確認密碼
        /// </summary>
        public string ConfirmPassword { get; set; } = null!;

    }

    public class BlockedModel
    {
        public string BlockerId { get; set; } = null!;
        public string BlockedId { get; set; } = null!;


    }
}
