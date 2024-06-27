using ChitChit.Areas.Identity.Data;
using ChitChit.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;


namespace ChitChit.Controllers.PersonalSettingManagement
{
    /// <summary>
    /// 登入
    /// </summary>
    [Route("ChitChit/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "PersonalSettingManagement")]
    public class LoginController : ControllerBase
    {
        // Identity - 使用者管理器
        private readonly UserManager<ChitChitUser> _userManager;
        // Identity - 登入管理器
        private readonly SignInManager<ChitChitUser> _signInManager;
        // ChitChit資料庫
        private readonly chitchitContext _chitChitContext;
        // redis 資料庫
        private readonly IConnectionMultiplexer _redisService;


        public LoginController(chitchitContext chitChitContext, UserManager<ChitChitUser> userManager, SignInManager<ChitChitUser> signInManager, IHttpContextAccessor httpContextAccessor, IConnectionMultiplexer redisService)
        {
            _chitChitContext = chitChitContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _redisService = redisService;
        }

        /// <summary>
        /// 註冊會員
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("RegisterMember")]
        public IActionResult RegisterMember([FromBody] RegisterMemberModel data)
        {
            string funcFrom = "Controllers/LoginController/RegisterMember:[POST]";
            try
            {
                string message = "";

                // 判斷註冊帳號是否重複




                // 確認輸入的帳號不為 null
                if (data.UserName != null)
                {
                    // 建立新用戶
                    ChitChitUser newUser = new ChitChitUser
                    {
                        FullUserName = data.FullIUserName,
                        UserName = data.UserName,
                        Email = data.UserName,
                        PhoneNumber = data.PhoneNumber,
                        Avatar = data.Avatar,
                        IsEnable = data.IsEnable,
                        NormalizedUserName = data.UserName.ToUpper(),
                        NormalizedEmail = data.UserName,
                        EmailConfirmed = true,
                        LockoutEnabled = true,
                        AccessFailedCount = 0,
                        TwoFactorEnabled = false,
                        PhoneNumberConfirmed = false
                    };
                    // 建立哈希密碼物件
                    PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();
                    // 轉換哈希密碼並放入newUser物件中的欄位
                    newUser.PasswordHash = passwordHasher.HashPassword(newUser, data.Password);
                    // 新增使用者並更新資料庫
                    _chitChitContext.ChitChitUser.Add(newUser);
                    _chitChitContext.SaveChanges();

                    // 回傳結果 - 成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }
                // 回傳結果 - 
                message = "DataNull";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });

            }

        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <param name="data">帳號資訊</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel data)
        {
            string funcFrom = "Controllers/LoginController/Login:[POST]";
            try
            {
                string message = "";

                // 取得目標使用者
                var targetUser = _chitChitContext.ChitChitUser.Where(u => u.UserName == data.Email && u.Email == data.Email).FirstOrDefault();
                if (targetUser != null)
                {
                    // 確認是否被禁用
                    if (targetUser.IsEnable)
                    {
                        var signInResult = await _signInManager.PasswordSignInAsync(targetUser, data.Password, true, lockoutOnFailure: false);

                        if (signInResult.Succeeded)
                        {
                            // 建立聲明列表
                            var claims = new List<Claim>
                            {
                                new Claim("Email", targetUser.Email),
                                new Claim("FullUserName", targetUser.FullUserName != null ? targetUser.FullUserName : ""),
                                new Claim("UserId", targetUser.Id),
                            };


                            // 回傳JWT
                            // 
                            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                            // 登入key
                            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SignKey"] ?? ""));
                            var jwtExpHours = 24; // 預設jwt可用時間(小時)


                            // 設定jwt相關資訊
                            var jwt = new JwtSecurityToken
                            (
                                issuer: configuration["JWT:Issuer"],
                                claims: claims,
                                expires: DateTime.Now.AddHours(jwtExpHours),
                                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256) // 產生JWT金鑰
                            );

                            // 產生jwt token
                            var token = new JwtSecurityTokenHandler().WriteToken(jwt);


                            // 建立 ClaimIdentity 使用者的聲明
                            var claimsIdentity = new ClaimsIdentity(claims);
                            HttpContext.User = new ClaimsPrincipal(claimsIdentity);
                            //// 盡力身分驗證的cookie 包含使用者身分信息存儲在cookie中 以便後續請求中進行身分驗證
                            //await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                            // 回傳結果 - 成功
                            message = "Success";
                            Log.Information($"{funcFrom}->{message}");
                            return Ok(new { status = "normal", message, result = token });
                        }
                        // 回傳結果 - 登入失敗
                        message = "LoginFail";
                        Log.Information($"{funcFrom}->{message}");
                        return Ok(new { status = "normal", message, result = new { } });
                    }
                    // 回傳結果 - 使用者禁用
                    message = "UserDisable";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }

                // 回傳結果 - 登入失敗
                message = "LoginFail";
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
        /// 登出
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            string funcFrom = "Controllers/LoginController/Logout:[GET]";
            try
            {
                string message = "";
                // Identity 登出當前使用者
                await _signInManager.SignOutAsync();
                //// Cookie 驗證進行登出動作
                //await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                // 回傳結果 - 登出失敗
                message = "Success";
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
        /// 忘記密碼
        /// </summary>
        /// <returns></returns>
        [HttpGet("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword([FromQuery] string email)
        {
            string funcFrom = "Controllers/LoginController/ForgetPassword::[GET]";
            try
            {
                string message = "";

                // 尋找相符的email
                var user = await _userManager.FindByEmailAsync(email);


                // 用戶不存在 
                if (user == null)
                {
                    // 回傳結果 - 用戶不存在
                    message = "NotExist";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }
                
                // 未確認郵件的情況
                if (!(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // 回傳結果 - 未確認郵件的情況
                    message = "EmailNotConfirmed";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }

                // 生成重設密碼的token
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                resetToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));

                // token儲存session
                var expiredTime = TimeSpan.FromMinutes(5);
                await _redisService.GetDatabase().StringSetAsync($"ResetPasswordToken:{email}", resetToken, expiredTime);

                HttpContext.Session.SetString(email, resetToken);
                //foreach (var key in HttpContext.Session.Keys)
                //{
                //    var value = HttpContext.Session.GetString(key);
                //    if (value != null)
                //    {
                //        Console.WriteLine($"session: {value}");
                //    }
                //}

                // 回傳結果 - 回傳token
                message = "Success";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { status = "normal", message, result = resetToken });
            }
            catch (Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 確認驗證碼
        /// </summary>
        /// <param name="token"></param>
        /// <param name="email"></param>
        /// messa
        /// <returns></returns>
        [HttpGet("ConfirmedToken")]
        public async Task<IActionResult> ConfirmedToken([FromQuery] string email, string token)
        {
            string funcFrom = "Controllers/LoginController/ConfirmedToken:[GET]";
            try
            {
                string message = "";


                
                var targetToken = await _redisService.GetDatabase().StringGetAsync($"ResetPasswordToken:{email}");
                Console.WriteLine($"token : {targetToken}");
                if (targetToken == token)
                {
                    // 回傳結果 - 重設密碼成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = new { } });
                }
                // 根據 email 取得對應的 token值
                //var targetToken = HttpContext.Session.GetString(email);
                //if (targetToken　!= null)
                //{
                //    // 確認 token 與 用戶填的是否一樣
                //    if (targetToken == token)
                //    {
                //        // 回傳結果 - 重設密碼成功
                //        message = "Success";
                //        Log.Information($"{funcFrom}->{message}");
                //        return Ok(new { status = "normal", message, result = new { } });
                //    }

                //    // 回傳結果 - token 輸入錯誤
                //    message = "TokenFail";
                //    Log.Information($"{funcFrom}->{message}");
                //    return Ok(new { status = "normal", message, result = new { } });

                //}

                // 回傳結果 - 無該名用戶請求重設密碼
                message = "NotUserToResetPassword";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { status = "normal", message, result = new { } });
            }
            catch(Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }


        /// <summary>
        /// 重設密碼
        /// </summary>
        /// <returns></returns>
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel data)
        {
            string funcFrom = "Controllers/LoginController/ResetPassword:[POST]";
            try
            {
                string message = "";

                // 尋找相符的email
                var user = await _userManager.FindByEmailAsync(data.Email);
                if (user != null)
                {
                    // 確認密碼與確認密碼相同
                    var checkPassword = await _userManager.CheckPasswordAsync(user, data.OldPassword);
                    if (checkPassword)
                    {
                        // 解碼回utf-8字符串
                        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(data.ResetToken));
                        // 重設密碼
                        var resetPasswordResult = await _userManager.ResetPasswordAsync(user, decodedToken, data.NewPassword);
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
        /// 解析JWT資訊
        /// </summary>
        /// <param name="jwtToken"></param>
        /// <returns></returns>
        [HttpGet("ParseJWT")]
        public IActionResult ParseJWT([FromQuery] string jwtToken)
        {
            string funcFrom = "Controllers/LoginController/ParseJWT:[GET]";
            try
            {
                string message = "";
                // 建立jwt處理物件
                var jwtHandler = new JwtSecurityTokenHandler();
                // 解析token
                var jwtInformation = jwtHandler.ReadJwtToken(jwtToken);
                // 提取 Token 中的聲明
                var claims = jwtInformation.Claims;
                // 建立新字典
                var response = new JwtClaimModel();

                foreach(var claim in claims)
                {
                    // 只取得要拿到的資訊
                    if (claim.Type == "Email")
                    {
                        response.Email = claim.Value;
                    }
                    if (claim.Type == "FullUserName")
                    {
                        response.FullUserName = claim.Value;
                    }
                    if (claim.Type == "UserId")
                    {
                        response.UserId = claim.Value;
                    }
                }
                
                var targetAvatar = _chitChitContext.ChitChitUser.Where(x => x.Id == response.UserId).FirstOrDefault();
                if (targetAvatar != null)
                {
                    response.Avatar = targetAvatar.Avatar;
                    
                    // 回傳結果 - 重設密碼成功
                    message = "Success";
                    Log.Information($"{funcFrom}->{message}");
                    return Ok(new { status = "normal", message, result = response});
                }

                // 回傳結果 - 查無使用者
                message = "NotFound";
                Log.Information($"{funcFrom}->{message}");
                return Ok(new { status = "normal", message, result = new { } });
            }
            catch(Exception ex)
            {
                // 回傳結果 - 伺服器錯誤
                Log.Error($"{funcFrom}->{ex.Message}");
                return Ok(new { status = "error", message = ex.Message, result = new { } });
            }
        }

        /// <summary>
        /// 註冊會員model
        /// </summary>
        public class RegisterMemberModel
        {
            /// <summary>
            /// 姓名
            /// </summary>
            public string? FullIUserName { get; set; }
            /// <summary>
            /// 帳號
            /// </summary>
            public string? UserName { get; set; }
            /// <summary>
            /// 密碼(未加密)
            /// </summary>
            public string? Password { get; set; }
            /// <summary>
            /// 信箱
            /// </summary>
            public string? Email { get; set; }
            /// <summary>
            /// 電話號碼
            /// </summary>
            public string? PhoneNumber { get; set; }
            /// <summary>
            /// 大頭貼
            /// </summary>
            public string? Avatar { get; set; }
            /// <summary>
            /// 停用使用者
            /// </summary>
            public bool IsEnable { get; set; }
        }

        /// <summary>
        /// 登入model
        /// </summary>
        public class LoginModel
        {
            /// <summary>
            /// 信箱
            /// </summary>
            public string Email { get; set; } = null!;
            
            /// <summary>
            /// 帳號
            /// </summary>
            public string Password { get; set; } = null!;
        }

        /// <summary>
        /// 重設密碼Model
        /// </summary>
        public class ResetPasswordModel
        {
            /// <summary>
            /// 信箱
            /// </summary>
            public string Email { get; set; } = null!;
            
            /// <summary>
            /// 密碼
            /// </summary>
            public string OldPassword { get; set; } = null!;

            /// <summary>
            /// 確認密碼
            /// </summary>
            public string NewPassword { get; set; } = null!;

            /// <summary>
            /// 重設密碼token
            /// </summary>
            public string ResetToken { get; set; } = null!;
        }

        public class JwtClaimModel
        {

            public string Email { get; set; } = null!;

            public string FullUserName { get; set; } = null!;

            public string UserId { get; set; } = null!;

            public string? Avatar { get; set; }


        }
    }
}
