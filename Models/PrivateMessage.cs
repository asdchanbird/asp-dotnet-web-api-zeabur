using ChitChit.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    public class PrivateMessage
    {
        /// <summary>
        /// 私聊訊息Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 訊息內容
        /// </summary>
        public string? MessageContent { get; set; }

        /// <summary>
        /// 用戶Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 用戶名稱
        /// </summary>
        public string? FullUserName {  get; set; }

        /// <summary>
        /// 私聊 Id
        /// </summary>
        [JsonIgnore] // 轉換Json格式 省略欄位
        [ForeignKey("ChatId")]
        public virtual PrivateChat PrivateChat { get; set; } = null!;

        /// <summary>
        /// 私聊 Id
        /// </summary>

        public int ChatId { get; set; }

        /// <summary>
        /// 建立訊息時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }
    }
}
