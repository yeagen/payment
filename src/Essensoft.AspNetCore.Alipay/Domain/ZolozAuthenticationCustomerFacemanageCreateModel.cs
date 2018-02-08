using Newtonsoft.Json;

namespace Essensoft.AspNetCore.Alipay.Domain
{
    /// <summary>
    /// ZolozAuthenticationCustomerFacemanageCreateModel Data Structure.
    /// </summary>
    public class ZolozAuthenticationCustomerFacemanageCreateModel : AlipayObject
    {
        /// <summary>
        /// 地域编码
        /// </summary>
        [JsonProperty("areacode")]
        public string Areacode { get; set; }

        /// <summary>
        /// 业务量规模
        /// </summary>
        [JsonProperty("bizscale")]
        public string Bizscale { get; set; }

        /// <summary>
        /// 商户品牌
        /// </summary>
        [JsonProperty("brandcode")]
        public string Brandcode { get; set; }

        /// <summary>
        /// 商户机具唯一编码，关键参数
        /// </summary>
        [JsonProperty("devicenum")]
        public string Devicenum { get; set; }

        /// <summary>
        /// 拓展参数
        /// </summary>
        [JsonProperty("extinfo")]
        public string Extinfo { get; set; }

        /// <summary>
        /// 入库类型
        /// </summary>
        [JsonProperty("facetype")]
        public string Facetype { get; set; }

        /// <summary>
        /// 入库用户信息
        /// </summary>
        [JsonProperty("faceval")]
        public string Faceval { get; set; }

        /// <summary>
        /// 分组5
        /// </summary>
        [JsonProperty("group")]
        public string Group { get; set; }

        /// <summary>
        /// 门店编码
        /// </summary>
        [JsonProperty("storecode")]
        public string Storecode { get; set; }

        /// <summary>
        /// 有效期天数，如7天、30天、365天
        /// </summary>
        [JsonProperty("validtimes")]
        public string Validtimes { get; set; }
    }
}