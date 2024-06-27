using ChitChit.Areas.Identity.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 用戶群組關係
    /// </summary>
    public class GroupRel
    {
        /// <summary>
        ///  用戶群組關係Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用戶Id
        /// </summary>        
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 群組Id
        /// </summary>
        [JsonIgnore]
        [ForeignKey("GroupChatId")]        
        public virtual GroupChat? GroupChat { get; set; }
        

        /// <summary>
        /// 群組id
        /// </summary>
        public int GroupChatId { get; set; }
        
        /// <summary>
        /// 加入群組時間
        /// </summary>
        public DateTimeOffset JoinGroupTime { get; set; }

        /// <summary>
        /// 用戶在群組的角色
        /// </summary>
        public GroupRole Role { get; set; }
    }

    public enum GroupRole
    {
        [Description("建立者")]
        Admin = 0,

        [Description("一般使用者")]
        User = 1

    }
}
