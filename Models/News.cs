using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    public class News
    {
        /// <summary>
        /// 最新消息id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 最新消息標題
        /// </summary>
        public string? NewTitle { get; set; }

        /// <summary>
        /// 最新消息內容
        /// </summary>
        public string? NewsContent { get; set; }

        /// <summary>
        /// 最新消息圖片Json格式
        /// </summary>
        [NotMapped]
        public List<string> NewsImgByJson
        {
            get
            {
                // 判斷字串是否為空值
                if (string.IsNullOrEmpty(NewsImg))
                {
                    // 回傳列表
                    return new List<string> { };
                }
                // 非空值
                else
                {
                    // 反序列化轉化為 List<int>
                    var result = JsonConvert.DeserializeObject<List<string>>(NewsImg);
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
                    NewsImg = "[]";
                }
                else
                {
                    // 序列化並帶入ImgRepo_ImagesByJson
                    NewsImg = JsonConvert.SerializeObject(value);
                }
            }
        }

        /// <summary>
        /// 最新消息圖片
        /// </summary>
        [MaxLength(256)]
        [System.Text.Json.Serialization.JsonIgnore]
        public string NewsImg { get; set; } = "[]";


        /// <summary>
        /// d
        /// </summary>
        public DateTimeOffset RecordTime { get; set; }
    }
}
