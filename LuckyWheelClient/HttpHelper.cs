using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class HttpHelper
    {
        private static readonly HttpClient httpClient;
        private static readonly string baseUrl = "http://localhost:8080"; // API URL của server
        private static readonly Dictionary<string, string> cachedResults = new Dictionary<string, string>();
        private static readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(5); // Cache hết hạn sau 5 phút
        private static readonly Dictionary<string, DateTime> cacheTimestamps = new Dictionary<string, DateTime>();

        static HttpHelper()
        {
            // Thiết lập HttpClient với các tùy chọn nâng cao
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxConnectionsPerServer = 10
            };

            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "LuckyWheelClient/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        }

        /// <summary>
        /// Gửi yêu cầu GET đến server với hỗ trợ cache
        /// </summary>
        /// <param name="endpoint">Đường dẫn API (ví dụ: /status)</param>
        /// <param name="useCache">Cho phép sử dụng cache hay không</param>
        /// <returns>Kết quả trả về từ server</returns>
        public static async Task<string> GetAsync(string endpoint, bool useCache = true)
        {
            try
            {
                // Kiểm tra cache nếu được phép
                string cacheKey = $"GET_{endpoint}";
                if (useCache && cachedResults.ContainsKey(cacheKey))
                {
                    // Kiểm tra xem cache có hết hạn chưa
                    if (cacheTimestamps.ContainsKey(cacheKey) &&
                        (DateTime.Now - cacheTimestamps[cacheKey]) < cacheExpiry)
                    {
                        return cachedResults[cacheKey];
                    }
                }

                // Chuẩn bị URL đầy đủ
                string url = $"{baseUrl}{endpoint}";

                // Gửi request GET
                HttpResponseMessage response = await httpClient.GetAsync(url);

                // Kiểm tra status code
                response.EnsureSuccessStatusCode();

                // Đọc nội dung trả về
                string result = await response.Content.ReadAsStringAsync();

                // Lưu vào cache nếu được phép
                if (useCache)
                {
                    cachedResults[cacheKey] = result;
                    cacheTimestamps[cacheKey] = DateTime.Now;
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request lỗi: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out");
                return "ERROR: Request timed out";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST đến server với dữ liệu JSON
        /// </summary>
        /// <param name="endpoint">Đường dẫn API</param>
        /// <param name="jsonContent">Nội dung JSON cần gửi</param>
        /// <returns>Kết quả trả về từ server</returns>
        public static async Task<string> PostJsonAsync(string endpoint, string jsonContent)
        {
            try
            {
                // Chuẩn bị URL đầy đủ
                string url = $"{baseUrl}{endpoint}";

                // Tạo HTTP content với dữ liệu JSON
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Gửi request POST
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                // Kiểm tra status code
                response.EnsureSuccessStatusCode();

                // Đọc nội dung trả về
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request lỗi: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out");
                return "ERROR: Request timed out";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Gửi yêu cầu POST với form data
        /// </summary>
        /// <param name="endpoint">Đường dẫn API</param>
        /// <param name="formData">Dictionary chứa dữ liệu form</param>
        /// <returns>Kết quả trả về từ server</returns>
        public static async Task<string> PostFormAsync(string endpoint, Dictionary<string, string> formData)
        {
            try
            {
                // Chuẩn bị URL đầy đủ
                string url = $"{baseUrl}{endpoint}";

                // Tạo form content từ Dictionary
                var formContent = new FormUrlEncodedContent(formData);

                // Gửi request POST
                HttpResponseMessage response = await httpClient.PostAsync(url, formContent);

                // Kiểm tra status code
                response.EnsureSuccessStatusCode();

                // Đọc nội dung trả về
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request lỗi: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out");
                return "ERROR: Request timed out";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Tải file từ server
        /// </summary>
        /// <param name="endpoint">Đường dẫn API đến file</param>
        /// <param name="savePath">Đường dẫn lưu file trên máy tính</param>
        /// <returns>True nếu tải thành công, false nếu thất bại</returns>
        public static async Task<bool> DownloadFileAsync(string endpoint, string savePath)
        {
            try
            {
                // Chuẩn bị URL đầy đủ
                string url = $"{baseUrl}{endpoint}";

                // Sử dụng WebClient để tải file
                using (WebClient client = new WebClient())
                {
                    // Thiết lập header
                    client.Headers.Add("User-Agent", "LuckyWheelClient/1.0");

                    // Tải file không đồng bộ
                    await client.DownloadFileTaskAsync(new Uri(url), savePath);
                    return true;
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"Lỗi tải file: {ex.Message}");
                MessageBox.Show($"Không thể tải file: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Tải lên file lên server
        /// </summary>
        /// <param name="endpoint">Đường dẫn API</param>
        /// <param name="filePath">Đường dẫn đến file cần tải lên</param>
        /// <param name="fileFieldName">Tên trường file</param>
        /// <param name="formData">Dữ liệu form bổ sung</param>
        /// <returns>Kết quả trả về từ server</returns>
        public static async Task<string> UploadFileAsync(string endpoint, string filePath, string fileFieldName = "file", Dictionary<string, string> formData = null)
        {
            try
            {
                // Kiểm tra file tồn tại
                if (!File.Exists(filePath))
                {
                    return "ERROR: File not found";
                }

                // Chuẩn bị URL đầy đủ
                string url = $"{baseUrl}{endpoint}";

                // Tạo multipart form content
                using (var content = new MultipartFormDataContent())
                {
                    // Thêm file
                    var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(filePath));
                    content.Add(fileContent, fileFieldName, Path.GetFileName(filePath));

                    // Thêm dữ liệu form khác nếu có
                    if (formData != null)
                    {
                        foreach (var kvp in formData)
                        {
                            content.Add(new StringContent(kvp.Value), kvp.Key);
                        }
                    }

                    // Gửi request POST
                    HttpResponseMessage response = await httpClient.PostAsync(url, content);

                    // Kiểm tra status code
                    response.EnsureSuccessStatusCode();

                    // Đọc nội dung trả về
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request lỗi: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không xác định: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        /// <summary>
        /// Xác định MIME type dựa vào phần mở rộng của file
        /// </summary>
        private static string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".txt":
                    return "text/plain";
                case ".zip":
                    return "application/zip";
                case ".csv":
                    return "text/csv";
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        /// Xóa tất cả các cache
        /// </summary>
        public static void ClearCache()
        {
            cachedResults.Clear();
            cacheTimestamps.Clear();
        }

        /// <summary>
        /// Kiểm tra kết nối đến server
        /// </summary>
        /// <returns>True nếu server đang hoạt động, false nếu không</returns>
        public static async Task<bool> CheckServerConnectionAsync()
        {
            try
            {
                string url = $"{baseUrl}/status";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}