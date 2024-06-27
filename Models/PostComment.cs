using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 社群留言
    /// </summary>
    public class PostComment
    {
        /// <summary>
        /// 留言 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用戶 Id
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 用戶名稱
        /// </summary>
        public string? FullUserName { get; set; }

        /// <summary>
        /// 貼文Id
        /// </summary>
        [JsonIgnore]
        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }
        
        /// <summary>
        /// 貼文Id
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 留言創建時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }

        /// <summary>
        /// 留言內容
        /// </summary>
        public string? CommentContent { get; set; }
    }
}
