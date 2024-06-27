using ChitChit.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChitChit.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class PersonalSetup
    {
        /// <summary>
        /// 個人化 Id
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用戶 Id
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// 主題
        /// </summary>
        public string? Theme {  get; set; }

        /// <summary>
        /// 語言類型
        /// </summary>
        public string? LanguageType { get; set; }

        /// <summary>
        /// 提醒方式
        /// </summary>
        public string? RemindType {  get; set; }
    }
}
