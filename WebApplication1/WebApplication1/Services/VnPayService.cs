using System.Net;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _config;

    public VnPayService(IConfiguration config)
    {
        _config = config;
    }

    public string CreatePaymentUrl(Order order, string returnUrl, string clientIp)
    {
        var baseUrl    = _config["VnPay:BaseUrl"]!;
        var tmnCode    = _config["VnPay:TmnCode"]!;
        var hashSecret = _config["VnPay:HashSecret"]!;

        var txnRef     = $"{order.Id}_{DateTime.Now.Ticks}";
        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

        var data = new SortedDictionary<string, string>
        {
            ["vnp_Version"]    = "2.1.0",
            ["vnp_Command"]    = "pay",
            ["vnp_TmnCode"]    = tmnCode,
            ["vnp_Amount"]     = ((long)(order.Total * 100)).ToString(),
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"]   = "VND",
            ["vnp_IpAddr"]     = clientIp,
            ["vnp_Locale"]     = "vn",
            ["vnp_OrderInfo"]  = $"Thanh toan don hang {order.Id} NexusGear",
            ["vnp_OrderType"]  = "other",
            ["vnp_ReturnUrl"]  = returnUrl,
            ["vnp_TxnRef"]     = txnRef,
        };

        // Dùng WebUtility.UrlEncode (spaces → "+") để khớp với cách VNPay verify
        var queryStr = BuildUrlEncodedString(data);
        var hash     = HmacSha512(hashSecret, queryStr);

        return $"{baseUrl}?{queryStr}&vnp_SecureHash={hash}";
    }

    public VnPayReturnModel ProcessReturn(IQueryCollection query)
    {
        var hashSecret    = _config["VnPay:HashSecret"]!;
        var secureHash    = query["vnp_SecureHash"].ToString();
        var responseCode  = query["vnp_ResponseCode"].ToString();
        var txnRef        = query["vnp_TxnRef"].ToString();
        var transactionNo = query["vnp_TransactionNo"].ToString();

        // Loại bỏ vnp_SecureHash và vnp_SecureHashType trước khi rebuild chuỗi ký
        var filtered = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in query)
        {
            if (key != "vnp_SecureHash" && key != "vnp_SecureHashType")
                filtered[key] = value.ToString();
        }

        // Rebuild chuỗi ký theo cùng cách encode với lúc tạo URL
        var signData  = BuildUrlEncodedString(filtered);
        var checkHash = HmacSha512(hashSecret, signData);
        var isValid   = checkHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);

        var orderId = 0;
        if (!string.IsNullOrEmpty(txnRef))
            int.TryParse(txnRef.Split('_')[0], out orderId);

        return new VnPayReturnModel
        {
            IsValidSignature = isValid,
            IsSuccess        = isValid && responseCode == "00",
            OrderId          = orderId,
            TransactionId    = transactionNo,
            ResponseCode     = responseCode
        };
    }

    // Dùng WebUtility.UrlEncode (chuẩn của VNPay): space → "+"
    private static string BuildUrlEncodedString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var kv in data)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(WebUtility.UrlEncode(kv.Key));
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(kv.Value));
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLower();
    }
}
