using ChitChit.Areas.Identity.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    public class FriendBlocked
    {
        /// <summary>
        ///  封鎖 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 封鎖人Id
        /// </summary>
        public string BlockerId { get; set; } = null!;

        /// <summary>
        /// 被封鎖人Id
        /// </summary>
        public string BlockedId { get; set; } = null!;

        /// <summary>
        /// 紀錄時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }
    }
}
