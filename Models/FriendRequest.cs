using ChitChit.Areas.Identity.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    /// <summary>
    /// 好友申請表
    /// </summary>
    public class FriendRequest
    {
        /// <summary>
        /// 好友申請id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 申請人id
        /// </summary>
        public string RequesterId { get; set; } = null!;

        /// <summary>
        /// 接收人id
        /// </summary>
        public string ReceiverId { get; set; } = null!;

        /// <summary>
        /// 申請時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }

        /// <summary>
        /// 申請狀態
        /// </summary>
        public Status RelationStatus { get; set; }

        /// <summary>
        /// 回應時間
        /// </summary>
        public DateTimeOffset? ResponseTime { get; set; }

    }
    public enum Status
    {
        [Description("尚未接受")]
        UnAccepted = 0,
        
        [Description("拒絕")]
        Reject = 1, 
        
        [Description("接受")]
        Success = 2,

        [Description("黑名單")]
        Blocked = 3,


    }
}
