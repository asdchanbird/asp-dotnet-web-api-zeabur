using ChitChit.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 私聊已讀回執表
    /// </summary>
    public class PrivateReadMessage
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用戶Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 私聊id
        /// </summary>
        [JsonIgnore]
        [ForeignKey("PrivateChatId")]
        public virtual PrivateChat? PrivateChat { get; set; }

        /// <summary>
        /// 私聊id
        /// </summary>
        public int PrivateChatId { get; set; }

        /// <summary>
        /// 已讀紀錄時間
        /// </summary>
        public DateTimeOffset ReadByTime {  get; set; }
    }
}
