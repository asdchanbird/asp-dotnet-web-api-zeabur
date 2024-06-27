using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ChitChit.Models
{
    /// <summary>
    /// 社群貼文點讚
    /// </summary>
    public class PostLike
    {
        /// <summary>
        /// 愛心 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 貼文 Id
        /// </summary>'
        [JsonIgnore]
        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        /// <summary>
        /// 貼文 Id
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 用戶Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 愛心創建時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }

        
    }
}
