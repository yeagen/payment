﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Essensoft.AspNetCore.Payment.Security;
using Essensoft.AspNetCore.Payment.WeChatPay.Parser;
using Essensoft.AspNetCore.Payment.WeChatPay.Request;
using Essensoft.AspNetCore.Payment.WeChatPay.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Essensoft.AspNetCore.Payment.WeChatPay
{
    public class WeChatPayClient : IWeChatPayClient
    {
        private const string appid = "appid";
        private const string mch_id = "mch_id";
        private const string mch_appid = "mch_appid";
        private const string mchid = "mchid";
        private const string wxappid = "wxappid";
        private const string sign_type = "sign_type";
        private const string nonce_str = "nonce_str";
        private const string sign = "sign";
        private const string enc_bank_no = "enc_bank_no";
        private const string enc_true_name = "enc_true_name";
        private const string partnerid = "partnerid";
        private const string noncestr = "noncestr";
        private const string timestamp = "timestamp";
        private const string appId = "appId";
        private const string timeStamp = "timeStamp";
        private const string nonceStr = "nonceStr";
        private const string signType = "signType";
        private const string paySign = "paySign";

        #region WeChatPayClient Constructors

        public WeChatPayClient(
            ILogger<WeChatPayClient> logger,
            IHttpClientFactory clientFactory,
            IOptionsSnapshot<WeChatPayOptions> optionsAccessor)
        {
            Logger = logger;
            ClientFactory = clientFactory;
            OptionsSnapshotAccessor = optionsAccessor;
        }

        #endregion

        public ILogger Logger { get; set; }

        public IHttpClientFactory ClientFactory { get; set; }

        public IOptionsSnapshot<WeChatPayOptions> OptionsSnapshotAccessor { get; set; }

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayRequest<T> request) where T : WeChatPayResponse
        {
            return await ExecuteAsync(request, null);
        }

        public async Task<T> ExecuteAsync<T>(IWeChatPayRequest<T> request, string optionsName) where T : WeChatPayResponse
        {
            var options = string.IsNullOrEmpty(optionsName) ? OptionsSnapshotAccessor.Value : OptionsSnapshotAccessor.Get(optionsName);
            // 字典排序
            var sortedTxtParams = new WeChatPayDictionary(request.GetParameters())
            {
                { mch_id, options.MchId },
                { nonce_str, Guid.NewGuid().ToString("N") }
            };

            if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
            {
                sortedTxtParams.Add(appid, options.AppId);
            }

            sortedTxtParams.Add(sign, WeChatPaySignature.SignWithKey(sortedTxtParams, options.Key));
            var content = WeChatPayUtility.BuildContent(sortedTxtParams);
            Logger.Log(options.LogLevel, "Request:{content}", content);

            using (var client = ClientFactory.CreateClient())
            {
                var body = await client.DoPostAsync(request.GetRequestUrl(), content);
                Logger.Log(options.LogLevel, "Response:{body}", body);

                var parser = new WeChatPayXmlParser<T>();
                var rsp = parser.Parse(body);
                CheckResponseSign(rsp, options);
                return rsp;
            }
        }

        #endregion

        #region IWeChatPayClient Members

        public async Task<T> ExecuteAsync<T>(IWeChatPayCertificateRequest<T> request, string certificateName) where T : WeChatPayResponse
        {
            return await ExecuteAsync(request, null, certificateName);
        }

        public async Task<T> ExecuteAsync<T>(IWeChatPayCertificateRequest<T> request, string optionsName, string certificateName) where T : WeChatPayResponse
        {
            var signType = true; // ture:MD5，false:HMAC-SHA256
            var excludeSignType = true;
            var options = string.IsNullOrEmpty(optionsName) ? OptionsSnapshotAccessor.Value : OptionsSnapshotAccessor.Get(optionsName);
            // 字典排序
            var sortedTxtParams = new WeChatPayDictionary(request.GetParameters());
            if (request is WeChatPayTransfersRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(mch_appid)))
                {
                    sortedTxtParams.Add(mch_appid, options.AppId);
                }

                sortedTxtParams.Add(mchid, options.MchId);
            }
            else if (request is WeChatPayGetPublicKeyRequest)
            {
                sortedTxtParams.Add(mch_id, options.MchId);
                sortedTxtParams.Add(sign_type, "MD5");
                excludeSignType = false;
            }
            else if (request is WeChatPayPayBankRequest)
            {
                if (options.PublicKey == null)
                {
                    throw new ArgumentNullException(nameof(options.RsaPublicKey));
                }

                var no = RSA_ECB_OAEPWithSHA1AndMGF1Padding.Encrypt(sortedTxtParams.GetValue(enc_bank_no), options.PublicKey);
                sortedTxtParams.SetValue(enc_bank_no, no);

                var name = RSA_ECB_OAEPWithSHA1AndMGF1Padding.Encrypt(sortedTxtParams.GetValue(enc_true_name), options.PublicKey);
                sortedTxtParams.SetValue(enc_true_name, name);

                sortedTxtParams.Add(mch_id, options.MchId);
                sortedTxtParams.Add(sign_type, "MD5");
            }
            else if (request is WeChatPayQueryBankRequest)
            {
                sortedTxtParams.Add(mch_id, options.MchId);
                sortedTxtParams.Add(sign_type, "MD5");
            }
            else if (request is WeChatPayGetTransferInfoRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
                {
                    sortedTxtParams.Add(appid, options.AppId);
                }

                sortedTxtParams.Add(mch_id, options.MchId);
                sortedTxtParams.Add(sign_type, "MD5");
            }
            else if (request is WeChatPayDownloadFundFlowRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
                {
                    sortedTxtParams.Add(appid, options.AppId);
                }

                sortedTxtParams.Add(mch_id, options.MchId);
                signType = false; // HMAC-SHA256
            }
            else if (request is WeChatPayRefundRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
                {
                    sortedTxtParams.Add(appid, options.AppId);
                }

                sortedTxtParams.Add(mch_id, options.MchId);
            }
            else if (request is WeChatPaySendRedPackRequest || request is WeChatPaySendGroupRedPackRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(wxappid)))
                {
                    sortedTxtParams.Add(wxappid, options.AppId);
                }

                sortedTxtParams.Add(mch_id, options.MchId);
            }
            else // 其他接口
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
                {
                    sortedTxtParams.Add(appid, options.AppId);
                }

                sortedTxtParams.Add(mch_id, options.MchId);
            }

            sortedTxtParams.Add(nonce_str, Guid.NewGuid().ToString("N"));
            sortedTxtParams.Add(sign, WeChatPaySignature.SignWithKey(sortedTxtParams, options.Key, signType, excludeSignType));

            var content = WeChatPayUtility.BuildContent(sortedTxtParams);
            Logger.Log(options.LogLevel, "Request:{content}", content);

            using (var client = ClientFactory.CreateClient(certificateName))
            {
                var body = await client.DoPostAsync(request.GetRequestUrl(), content);

                Logger.Log(options.LogLevel, "Response:{body}", body);

                var parser = new WeChatPayXmlParser<T>();
                var rsp = parser.Parse(body);
                CheckResponseSign(rsp, options, signType, excludeSignType);
                return rsp;
            }
        }

        #endregion

        #region IWeChatPayClient Members

        public Task<WeChatPayDictionary> ExecuteAsync(IWeChatPayCallRequest request)
        {
            return ExecuteAsync(request, null);
        }

        public Task<WeChatPayDictionary> ExecuteAsync(IWeChatPayCallRequest request, string optionsName)
        {
            var options = string.IsNullOrEmpty(optionsName) ? OptionsSnapshotAccessor.Value : OptionsSnapshotAccessor.Get(optionsName);
            var sortedTxtParams = new WeChatPayDictionary(request.GetParameters());

            if (request is WeChatPayAppCallPaymentRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appid)))
                {
                    sortedTxtParams.Add(appid, options.AppId);
                }

                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(partnerid)))
                {
                    sortedTxtParams.Add(partnerid, options.MchId);
                }
                sortedTxtParams.Add(noncestr, Guid.NewGuid().ToString("N"));
                sortedTxtParams.Add(timestamp, WeChatPayUtility.GetTimeStamp());
                sortedTxtParams.Add(sign, WeChatPaySignature.SignWithKey(sortedTxtParams, options.Key));
            }
            else if (request is WeChatPayLiteAppCallPaymentRequest || request is WeChatPayH5CallPaymentRequest)
            {
                if (string.IsNullOrEmpty(sortedTxtParams.GetValue(appId)))
                {
                    sortedTxtParams.Add(appId, options.AppId);
                }

                sortedTxtParams.Add(timeStamp, WeChatPayUtility.GetTimeStamp());
                sortedTxtParams.Add(nonceStr, Guid.NewGuid().ToString("N"));
                sortedTxtParams.Add(signType, "MD5");
                sortedTxtParams.Add(paySign, WeChatPaySignature.SignWithKey(sortedTxtParams, options.Key));
            }
            return Task.FromResult(sortedTxtParams);
        }

        #endregion

        #region Common Method

        private void CheckResponseSign(WeChatPayResponse response, WeChatPayOptions options, bool useMD5 = true, bool excludeSignType = true)
        {
            if (string.IsNullOrEmpty(response.Body))
            {
                throw new Exception("sign check fail: Body is Empty!");
            }

            if (response.Parameters.TryGetValue("sign", out var sign))
            {
                if (response.Parameters["return_code"] == "SUCCESS" && !string.IsNullOrEmpty(sign))
                {
                    var cal_sign = WeChatPaySignature.SignWithKey(response.Parameters, options.Key, useMD5, excludeSignType);
                    if (cal_sign != sign)
                    {
                        throw new Exception("sign check fail: check Sign and Data Fail!");
                    }
                }
            }
        }

        #endregion
    }
}
