using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 聊天私聊
    /// </summary>
    public class PrivateChat
    {
        /// <summary>
        /// 私聊Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 私聊建立時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }


        /// <summary>
        /// 好友關係Id
        /// </summary>
        [JsonIgnore] // 轉換Json格式 省略欄位
        [ForeignKey("FriendRelId")]
        public virtual FriendRel FriendRel { get; set; } = null!;

        /// <summary>
        /// 好友關係Id
        /// </summary>
        public int FriendRelId { get; set; }


        /// <summary>
        /// 是否釘選
        /// </summary>
        public bool IsPinned { get; set; } = false;
    }
}
