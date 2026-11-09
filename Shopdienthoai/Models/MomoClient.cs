using System.Net.Http.Json;
using System.Text.Json;

namespace Shopdienthoai.Models
{
    public class MomoClient
    {
        private readonly HttpClient _http; private readonly IConfiguration _c;
        public MomoClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _c = config;
        }
        public static bool Ready(IConfiguration c) => c.GetValue<bool>("Momo:Enabled") && new[] { "PartnerCode", "AccessKey", "SecretKey" }.All(k => !string.IsNullOrWhiteSpace(c["Momo:" + k])) && Uri.TryCreate(c["Momo:PublicBaseUrl"], UriKind.Absolute, out var u) && u.Scheme == "https" && !u.IsLoopback;
        public async Task<MomoResult> Create(Giaodichthanhtoan g)
        {
            var partner = _c["Momo:PartnerCode"]!;
            var access = _c["Momo:AccessKey"]!;
            var secret = _c["Momo:SecretKey"]!;
            var publicUrl = _c["Momo:PublicBaseUrl"]!.TrimEnd('/');
            var redirectUrl = publicUrl + "/Payment/Return?id=" + g.MaHd;
            var ipnUrl = publicUrl + "/Payment/Ipn";
            var info = "ChienNguyenShop - don hang #" + g.MaHd;
            var amount = checked((long)g.SoTien);
            var raw = $"accessKey={access}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={g.OrderId}&orderInfo={info}&partnerCode={partner}&redirectUrl={redirectUrl}&requestId={g.RequestId}&requestType=captureWallet";
            using var response = await _http.PostAsJsonAsync(Endpoint("create"), new
            {
                partnerCode = partner,
                requestId = g.RequestId,
                amount,
                orderId = g.OrderId,
                orderInfo = info,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData = "",
                requestType = "captureWallet",
                autoCapture = true,
                signature = ShopRules.Hmac(secret, raw)
            });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MomoResult>() ?? throw new HttpRequestException("Empty MoMo response");
        }
        public async Task<MomoResult> Query(Giaodichthanhtoan g)
        {
            var partner = _c["Momo:PartnerCode"]!;
            var requestId = Guid.NewGuid().ToString("N");
            var raw = $"accessKey={_c["Momo:AccessKey"]}&orderId={g.OrderId}&partnerCode={partner}&requestId={requestId}";
            using var response = await _http.PostAsJsonAsync(Endpoint("query"), new
            {
                partnerCode = partner,
                requestId,
                orderId = g.OrderId,
                lang = "vi",
                signature = ShopRules.Hmac(_c["Momo:SecretKey"]!, raw)
            });
            response.EnsureSuccessStatusCode();
            var r = await response.Content.ReadFromJsonAsync<MomoResult>() ?? throw new HttpRequestException("Empty query response");
            if (r.OrderId != g.OrderId || r.PartnerCode != partner || r.Amount != (long)g.SoTien || !r.ResultCode.HasValue)
                throw new HttpRequestException("MoMo query mismatch");
            // Query responses echo the query requestId; IPN identifies the original create request.
            r.RequestId = g.RequestId;
            return r;
        }
        private string Endpoint(string action)
        {
            // This project intentionally uses Sandbox only. Never silently switch to live payments.
            if (_c["Momo:BaseUrl"]?.TrimEnd('/') != "https://test-payment.momo.vn")
                throw new InvalidOperationException("Chỉ hỗ trợ MoMo Sandbox trong bản đồ án.");
            return "https://test-payment.momo.vn/v2/gateway/api/" + action;
        }
        public bool ValidIpn(MomoResult r)
        {
            if (!Ready(_c) || r.PartnerCode != _c["Momo:PartnerCode"] || !r.ResultCode.HasValue)
                return false;
            var raw = $"accessKey={_c["Momo:AccessKey"]}&amount={r.Amount}&extraData={r.ExtraData}&message={r.Message}&orderId={r.OrderId}&orderInfo={r.OrderInfo}&orderType={r.OrderType}&partnerCode={r.PartnerCode}&payType={r.PayType}&requestId={r.RequestId}&responseTime={r.ResponseTime}&resultCode={r.ResultCode}&transId={r.TransId}";
            return ShopRules.SameSignature(r.Signature, ShopRules.Hmac(_c["Momo:SecretKey"]!, raw));
        }
        public static bool SafePayUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var u) && u.Scheme == "https" && (u.Host == "momo.vn" || u.Host.EndsWith(".momo.vn", StringComparison.OrdinalIgnoreCase));
    }
}
