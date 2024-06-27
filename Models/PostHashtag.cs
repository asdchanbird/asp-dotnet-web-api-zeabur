using System.ComponentModel.DataAnnotations;

namespace ChitChit.Models
{
    public class PostHashtag
    {
        /// <summary>
        /// 標籤Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 標籤名稱
        /// </summary>
        public string? TagName { get; set; }

        /// <summary>
        /// 使用標籤次數
        /// </summary>
        public int TagCount { get; set; }

        /// <summary>
        /// 紀錄時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }
    }
}
