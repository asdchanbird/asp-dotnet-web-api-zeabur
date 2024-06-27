using System.ComponentModel.DataAnnotations;

namespace ChitChit.Models
{
    /// <summary>
    /// 聊天室群組
    /// </summary>
    public class GroupChat
    {
        /// <summary>
        /// 群組Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        ///  群組名稱
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 使用者Id
        /// </summary>
        public string UserId { get; set; } = null!;


        /// <summary>
        /// 群組創建時間
        /// </summary>
        public DateTimeOffset RecordTime {  get; set; }


    }
}
