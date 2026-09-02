using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MatchdayApi.Services;

/// <summary>
/// Helper build URL thanh toán và kiểm tra chữ ký VNPay — viết theo đúng thuật toán chuẩn
/// VNPay yêu cầu (sắp xếp tham số theo tên A-Z, nối chuỗi, ký HMAC-SHA512 bằng HashSecret).
/// Không tự chế thuật toán khác vì VNPay sẽ từ chối nếu chữ ký sai định dạng.
/// </summary>
public class VnPayLibrary
{
    private readonly SortedList<string, string> _requestData = new(new VnPayComparer());
    private readonly SortedList<string, string> _responseData = new(new VnPayComparer());

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData[key] = value;
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData[key] = value;
        }
    }

    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>Build URL redirect người dùng sang trang thanh toán VNPay, đã ký sẵn vnp_SecureHash.</summary>
    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var data = new StringBuilder();
        foreach (var kv in _requestData)
        {
            data.Append(WebUtility.UrlEncode(kv.Key)).Append('=').Append(WebUtility.UrlEncode(kv.Value)).Append('&');
        }

        var queryString = data.ToString();
        var signData = queryString.TrimEnd('&');
        var secureHash = HmacSha512(hashSecret, signData);

        return $"{baseUrl}?{queryString}vnp_SecureHash={secureHash}";
    }

    /// <summary>Kiểm tra vnp_SecureHash mà VNPay gửi kèm ở Return URL / IPN có khớp dữ liệu không
    /// (chống giả mạo — không tự ý tin dữ liệu trả về nếu chưa validate qua hàm này).</summary>
    public bool ValidateSignature(string inputHash, string hashSecret)
    {
        var myChecksum = HmacSha512(hashSecret, GetResponseSignData());
        return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string GetResponseSignData()
    {
        var data = new StringBuilder();
        foreach (var kv in _responseData)
        {
            data.Append(WebUtility.UrlEncode(kv.Key)).Append('=').Append(WebUtility.UrlEncode(kv.Value)).Append('&');
        }

        return data.ToString().TrimEnd('&');
    }

    private static string HmacSha512(string key, string inputData)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    /// <summary>So sánh key theo đúng cách VNPay yêu cầu (ordinal trên chuỗi đã url-encode).</summary>
    private class VnPayComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            return string.CompareOrdinal(WebUtility.UrlEncode(x), WebUtility.UrlEncode(y));
        }
    }
}
