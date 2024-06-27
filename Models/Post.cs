using ChitChit.Areas.Identity.Data;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    /// <summary>
    /// 社群貼文
    /// </summary>
    public class Post
    {
        /// <summary>
        /// 貼文 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 貼文使用者 Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 貼文內容
        /// </summary>
        public string? PostContent { get; set; }

        /// <summary>
        /// 標籤 - 串列格式
        /// </summary>
        [NotMapped]
        public List<string> HashtagByJson
        {
            get
            {
                // 判斷字串是否為空值
                if (string.IsNullOrEmpty(Hashtag))
                {
                    // 回傳列表
                    return new List<string> { };
                }
                // 非空值
                else
                {
                    // 反序列化轉化為 List<int>
                    var result = JsonConvert.DeserializeObject<List<string>>(Hashtag);
                    // 如果左邊為空值則將右邊的運算數值賦值給左邊
                    result ??= new List<string> { };
                    // 回傳
                    return result;
                }
            }
            set
            {
                // 判斷傳入的值不為空值或確認是否有元素存在
                if (value == null || !value.Any())
                {
                    // 無值 給予初始值
                    Hashtag = "[]";
                }
                else
                {
                    // 序列化並帶入ImgRepo_ImagesByJson
                    Hashtag = JsonConvert.SerializeObject(value);
                }
            }
        }

        /// <summary>
        /// 標籤 - 字串格式
        /// </summary>
        [MaxLength(256)]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Hashtag { get; set; } = "[]";
        /// <summary>
        /// 貼文創建時間
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }
    }
}
