using ChitChit.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 聊天訊息
    /// </summary>
    public class GroupMessage
    {
        /// <summary>
        /// 訊息Id
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// 訊息內容
        /// </summary>
        public string? MessageContent { get; set; }

        /// <summary>
        /// 訊息所屬群組
        /// </summary>
        [JsonIgnore]
        [ForeignKey("ChatId")]
        public virtual GroupChat? GroupChat {  get; set; }

        /// <summary>
        /// 群組id
        /// </summary>
        public int? ChatId { get; set; }

        /// <summary>
        /// 用戶Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 用戶名稱
        /// </summary>
        public string? FullUserName { get; set; }

        /// <summary>
        /// 建立訊息時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }

    }
}
