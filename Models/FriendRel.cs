using ChitChit.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    /// <summary>
    /// 用戶好友關係
    /// </summary>
    public class FriendRel
    {
        /// <summary>
        /// 用戶好友關係 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用戶 Id
        /// </summary>
        public string UserId1 { get; set; } = null!;

        /// <summary>
        /// 好友Id
        /// </summary>
        public string UserId2 { get; set; } = null!;

        /// <summary>
        /// 用戶好友關係狀態
        /// </summary>
        public Status RelationStatus { get; set; }
        
        /// <summary>
        /// 用戶好友關係建立時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }

    }
}
