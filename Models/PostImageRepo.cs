using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    /// <summary>
    /// 社群貼文圖片儲存區
    /// </summary>
    public class PostImageRepo
    {
        /// <summary>
        /// 貼文圖片儲存區 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 貼文 Id
        /// </summary>
        [JsonIgnore]
        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        /// <summary>
        /// 貼文ID
        /// </summary>
        public int PostId { get; set; }

        /// <summary>
        /// 貼文圖片儲存區
        /// </summary>
        [NotMapped]
        public List<string> ImgRepoImagesByJson
        {
            get
            {
                // 判斷字串是否為空值
                if (string.IsNullOrEmpty(ImgRepoImages))
                {
                    // 回傳列表
                    return new List<string> { };
                }
                // 非空值
                else
                {
                    // 反序列化轉化為 List<int>
                    var result = JsonConvert.DeserializeObject<List<string>>(ImgRepoImages);
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
                    ImgRepoImages = "[]";
                }
                else
                {
                    // 序列化並帶入ImgRepo_ImagesByJson
                    ImgRepoImages = JsonConvert.SerializeObject(value);
                }
            }
        }

        /// <summary>
        /// 圖片儲藏庫，json字串格式
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string ImgRepoImages { get; set; } = "[]";
    }
}
