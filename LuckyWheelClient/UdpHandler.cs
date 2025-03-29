using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace LuckyWheelClient
{
    public class UdpHandler
    {
        private static readonly int udpPort = 8889;
        private static readonly int multicastPort = 8890;
        private static readonly IPAddress multicastAddress = IPAddress.Parse("239.0.0.1");

        private UdpClient udpClient;
        private UdpClient multicastClient;
        private bool isListening = false;
        private CancellationTokenSource tokenSource;
        private List<string> messageHistory;
        private System.Windows.Forms.Timer pingTimer; // Chỉ định rõ loại Timer

        // Sự kiện thông báo khi nhận được tin nhắn mới
        public event EventHandler<UdpMessageEventArgs> MessageReceived;
        public event EventHandler<ServerStatusEventArgs> ServerStatusChanged;

        // Lớp sự kiện để truyền dữ liệu khi nhận được tin nhắn
        public class UdpMessageEventArgs : EventArgs
        {
            public string Message { get; set; }
            public string Source { get; set; }
            public DateTime TimeReceived { get; set; }
        }

        // Lớp sự kiện để thông báo trạng thái server
        public class ServerStatusEventArgs : EventArgs
        {
            public bool IsOnline { get; set; }
            public DateTime LastUpdate { get; set; }
            public string StatusMessage { get; set; }
        }

        public UdpHandler()
        {
            // Khởi tạo danh sách lưu trữ tin nhắn
            messageHistory = new List<string>();

            // Khởi tạo UDP client
            udpClient = new UdpClient();

            // Thiết lập và bắt đầu nghe broadcast
            Task.Run(() => SetupBroadcastListener());

            // Thiết lập và bắt đầu nghe multicast
            Task.Run(() => SetupMulticastListener());

            // Thiết lập ping server định kỳ
            StartServerStatusCheck();
        }

        private void SetupBroadcastListener()
        {
            try
            {
                // Tạo UDP client để lắng nghe broadcast
                using (UdpClient listener = new UdpClient())
                {
                    // Thiết lập socket để có thể sử dụng lại cổng
                    listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    // Thiết lập buffer size lớn hơn để xử lý nhiều dữ liệu
                    listener.Client.ReceiveBufferSize = 65536;

                    // Bind đến cổng UDP
                    listener.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));

                    isListening = true;
                    tokenSource = new CancellationTokenSource();

                    // Vòng lặp lắng nghe tin nhắn broadcast
                    while (isListening && !tokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                            byte[] data = listener.Receive(ref remoteEP);
                            string message = Encoding.UTF8.GetString(data);

                            // Lưu tin nhắn vào lịch sử
                            lock (messageHistory)
                            {
                                messageHistory.Add($"[{DateTime.Now}] Broadcast: {message}");
                                // Giới hạn số lượng tin nhắn lưu trữ
                                if (messageHistory.Count > 100)
                                    messageHistory.RemoveAt(0);
                            }

                            // Phát ra sự kiện khi nhận được tin nhắn
                            OnMessageReceived(message, $"Broadcast from {remoteEP}", DateTime.Now);

                            // Kiểm tra xem có phải thông báo trạng thái server
                            CheckServerStatus(message);
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Broadcast listener error: {ex.Message}");
                            // Chờ một chút rồi thử lại
                            Task.Delay(1000, tokenSource.Token).Wait();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up broadcast listener: {ex.Message}");
            }
        }

        private void SetupMulticastListener()
        {
            try
            {
                // Tạo multicast client
                multicastClient = new UdpClient();

                // Thiết lập socket để có thể sử dụng lại cổng
                multicastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Cho phép gửi multicast từ IP bất kỳ
                multicastClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.Any.GetAddressBytes());

                // Thiết lập buffer size lớn hơn
                multicastClient.Client.ReceiveBufferSize = 65536;

                // Bind đến cổng multicast
                multicastClient.Client.Bind(new IPEndPoint(IPAddress.Any, multicastPort));

                // Tham gia nhóm multicast
                multicastClient.JoinMulticastGroup(multicastAddress);

                isListening = true;
                tokenSource = new CancellationTokenSource();

                // Bắt đầu lắng nghe không đồng bộ
                Task.Run(async () =>
                {
                    while (isListening && !tokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            UdpReceiveResult result = await multicastClient.ReceiveAsync();
                            string message = Encoding.UTF8.GetString(result.Buffer);

                            // Lưu tin nhắn vào lịch sử
                            lock (messageHistory)
                            {
                                messageHistory.Add($"[{DateTime.Now}] Multicast: {message}");
                                // Giới hạn số lượng tin nhắn lưu trữ
                                if (messageHistory.Count > 100)
                                    messageHistory.RemoveAt(0);
                            }

                            // Phát ra sự kiện khi nhận được tin nhắn
                            OnMessageReceived(message, $"Multicast from {result.RemoteEndPoint}", DateTime.Now);

                            // Kiểm tra xem có phải thông báo trạng thái server
                            CheckServerStatus(message);
                        }
                        catch (Exception ex)
                        {
                            if (!tokenSource.Token.IsCancellationRequested)
                                Console.WriteLine($"Multicast listener error: {ex.Message}");

                            // Chờ một chút rồi thử lại
                            await Task.Delay(1000, tokenSource.Token);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up multicast listener: {ex.Message}");
            }
        }

        // Phương thức phát ra sự kiện khi nhận được tin nhắn
        protected virtual void OnMessageReceived(string message, string source, DateTime timeReceived)
        {
            MessageReceived?.Invoke(this, new UdpMessageEventArgs
            {
                Message = message,
                Source = source,
                TimeReceived = timeReceived
            });
        }

        // Kiểm tra thông báo trạng thái server
        private void CheckServerStatus(string message)
        {
            if (message.StartsWith("SERVER_STATUS|") || message.StartsWith("MULTICAST_STATUS|"))
            {
                string[] parts = message.Split('|');
                if (parts.Length >= 3)
                {
                    bool isOnline = parts[1] == "Active";
                    DateTime updateTime = DateTime.Now;

                    // Nếu có timestamp từ server
                    if (parts.Length >= 4 && DateTime.TryParse(parts[2] + " " + parts[3], out DateTime serverTime))
                    {
                        updateTime = serverTime;
                    }

                    // Phát ra sự kiện thông báo trạng thái server đã thay đổi
                    ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs
                    {
                        IsOnline = isOnline,
                        LastUpdate = updateTime,
                        StatusMessage = message
                    });
                }
            }
        }

        // Gửi yêu cầu UDP đến server
        public async Task<string> SendUdpRequest(string message)
        {
            try
            {
                // Gửi tin nhắn đến server
                byte[] data = Encoding.UTF8.GetBytes(message);
                await udpClient.SendAsync(data, data.Length, "localhost", udpPort);

                // Tạo endpoint để nhận phản hồi
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                // Thiết lập timeout để không đợi mãi
                using (var timeoutCts = new CancellationTokenSource(5000)) // 5 giây timeout
                {
                    var receiveTask = udpClient.ReceiveAsync();
                    var completedTask = await Task.WhenAny(receiveTask, Task.Delay(5000, timeoutCts.Token));

                    if (completedTask == receiveTask)
                    {
                        var result = await receiveTask;
                        string response = Encoding.UTF8.GetString(result.Buffer);

                        // Lưu phản hồi vào lịch sử
                        lock (messageHistory)
                        {
                            messageHistory.Add($"[{DateTime.Now}] Response: {response}");
                            if (messageHistory.Count > 100)
                                messageHistory.RemoveAt(0);
                        }

                        return response;
                    }
                    else
                    {
                        // Timeout
                        return "TIMEOUT";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending UDP request: {ex.Message}");
                return $"ERROR|{ex.Message}";
            }
        }

        // Gửi tin nhắn multicast
        public async Task<bool> SendMulticastMessage(string message)
        {
            try
            {
                // Tạo mới client nếu chưa có
                if (multicastClient == null)
                {
                    multicastClient = new UdpClient();
                }

                // Gửi tin nhắn đến nhóm multicast
                byte[] data = Encoding.UTF8.GetBytes(message);
                await multicastClient.SendAsync(data, data.Length, new IPEndPoint(multicastAddress, multicastPort));

                // Lưu tin nhắn đã gửi vào lịch sử
                lock (messageHistory)
                {
                    messageHistory.Add($"[{DateTime.Now}] Sent Multicast: {message}");
                    if (messageHistory.Count > 100)
                        messageHistory.RemoveAt(0);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending multicast message: {ex.Message}");
                return false;
            }
        }

        // Bắt đầu kiểm tra trạng thái server định kỳ
        private void StartServerStatusCheck()
        {
            // Tạo timer để ping server mỗi 30 giây
            pingTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000 // 30 giây
            };
            pingTimer.Tick += async (s, e) =>
            {
                await PingServer();
            };
            pingTimer.Start();
        }

        // Ping server để kiểm tra trạng thái
        private async Task PingServer()
        {
            try
            {
                string response = await SendUdpRequest("PING");
                bool isOnline = response.StartsWith("PONG");

                ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs
                {
                    IsOnline = isOnline,
                    LastUpdate = DateTime.Now,
                    StatusMessage = isOnline ? "Server đang hoạt động" : "Server không phản hồi"
                });
            }
            catch
            {
                // Nếu có lỗi, coi như server offline
                ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs
                {
                    IsOnline = false,
                    LastUpdate = DateTime.Now,
                    StatusMessage = "Không thể kết nối đến server"
                });
            }
        }

        // Lấy lịch sử tin nhắn
        public List<string> GetMessageHistory()
        {
            lock (messageHistory)
            {
                return new List<string>(messageHistory);
            }
        }

        // Ngừng lắng nghe khi đóng ứng dụng
        public void StopListening()
        {
            isListening = false;
            tokenSource?.Cancel();

            // Dừng timer
            pingTimer?.Stop();
            pingTimer?.Dispose();

            try
            {
                if (multicastClient != null)
                {
                    multicastClient.DropMulticastGroup(multicastAddress);
                    multicastClient.Close();
                    multicastClient = null;
                }

                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping UDP listener: {ex.Message}");
            }
        }
    }
}