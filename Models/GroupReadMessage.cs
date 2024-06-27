using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 已讀回執
    /// </summary>
    public class GroupReadMessage
    {
        /// <summary>
        /// 已讀Id
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// 使用者 Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 群組Id
        /// </summary>
        [JsonIgnore]
        [ForeignKey("GroupChatId")]
        public virtual GroupChat? ChatGroup { get; set; }

        /// <summary>
        /// 群組Id
        /// </summary>
        /// 刪除規則: 設為NULL
        public int? GroupChatId { get; set; }

        /// <summary>
        /// 已讀時間
        /// </summary>
        public DateTimeOffset ReadByTime { get; set; }
    }
}
