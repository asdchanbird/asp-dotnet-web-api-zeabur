using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ChitChit.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ChitChitUser class
public class ChitChitUser : IdentityUser
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public override string UserName { get; set; } = null!;

    [EmailAddress]
    [MaxLength(256)]
    public override string Email { get; set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>
    [MaxLength(256)]
    public string? FullUserName { get; set; }

    /// <summary>
    /// 大頭貼
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 生日日期
    /// </summary>
    [MaxLength(50)]
    public string? Birthday { get; set; }

    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsEnable { get; set; } = true;

    /// <summary>
    /// 狀態消息
    /// </summary>
    [MaxLength(512)]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// 是否刪除
    /// </summary>
    public bool IsDelete {  get; set; }

}

