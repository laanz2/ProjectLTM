using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LuckyWheelServer
{
    class Program
    {
        private static readonly int tcpPort = 9876; // Đã thay đổi từ 8888 sang 9876
        private static readonly int udpPort = 8889;
        private static readonly IPAddress multicastAddress = IPAddress.Parse("239.0.0.1");
        private static readonly int multicastPort = 8890;

        private static UdpClient udpServer;
        private static TcpListener tcpListener;
        private static UdpClient multicastClient;

        private static bool isRunning = true;
        private static Timer broadcastTimer;

        static void Main()
        {
            Console.Title = "Lucky Wheel Server";
            Console.WriteLine("🎮 LUCKY WHEEL SERVER");
            Console.WriteLine("====================");

            // Khởi tạo cơ sở dữ liệu
            try
            {
                using (var connection = CoSoDuLieu.MoKetNoi())
                {
                    Console.WriteLine("✅ Kết nối CSDL thành công.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi kết nối CSDL: {ex.Message}");
                Console.WriteLine("Nhấn phím bất kỳ để thoát...");
                Console.ReadKey();
                return;
            }

            try
            {
                // Khởi động TCP server
                StartTcpServer();

                // Khởi động UDP server
                StartUdpServer();

                // Khởi động Multicast server
                StartMulticastServer();

                // Thiết lập broadcast timer (gửi thông báo mỗi 60 giây)
                broadcastTimer = new Timer(BroadcastServerStatus, null, 0, 60000);

                Console.WriteLine("\n✅ Server đã sẵn sàng phục vụ!");
                Console.WriteLine("Nhấn 'Q' để thoát...");

                while (isRunning)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        isRunning = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khởi động server: {ex.Message}");
            }
            finally
            {
                ShutdownServer();
            }
        }

        private static void StartTcpServer()
        {
            try
            {
                // Đảm bảo cổng không bị chiếm
                try
                {
                    // Kiểm tra nếu cổng đang được sử dụng
                    System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                    System.Net.NetworkInformation.TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                    foreach (System.Net.NetworkInformation.TcpConnectionInformation tcpi in tcpConnInfoArray)
                    {
                        if (tcpi.LocalEndPoint.Port == tcpPort)
                        {
                            Console.WriteLine($"⚠️ Cảnh báo: Cổng {tcpPort} đã được sử dụng bởi một tiến trình khác!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Không thể kiểm tra cổng: {ex.Message}");
                }

                // Khởi tạo TcpListener với địa chỉ Loopback thay vì Any
                tcpListener = new TcpListener(IPAddress.Any, tcpPort);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpListener.Start(10); // Chỉ định backlog là 10

                Console.WriteLine($"✅ TCP Server đang lắng nghe tại cổng {tcpPort}...");
                Console.WriteLine($"Địa chỉ IP: {IPAddress.Any} (tất cả các interface mạng)");

                // Bắt đầu chấp nhận kết nối bất đồng bộ
                Task.Run(async () =>
                {
                    while (isRunning)
                    {
                        try
                        {
                            Console.WriteLine("Đang đợi kết nối từ client...");

                            // Đợi kết nối từ client
                            TcpClient client = await tcpListener.AcceptTcpClientAsync();

                            Console.WriteLine($"🔌 Client kết nối từ {((IPEndPoint)client.Client.RemoteEndPoint).Address}:{((IPEndPoint)client.Client.RemoteEndPoint).Port}");

                            // Tăng timeout cho client
                            client.ReceiveTimeout = 600000; // 10 phút
                            client.SendTimeout = 300000;    // 5 phút

                            // Tạo thread mới xử lý client
                            ThreadPool.QueueUserWorkItem(ClientHandler.HandleClient, client);
                        }
                        catch (Exception ex)
                        {
                            if (isRunning)
                            {
                                Console.WriteLine($"❌ Lỗi chấp nhận kết nối TCP: {ex.Message}");
                                Console.WriteLine($"Chi tiết lỗi: {ex}");

                                // Chờ một chút trước khi thử lại
                                await Task.Delay(1000);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khởi tạo TCP Server: {ex.Message}");
                Console.WriteLine($"Chi tiết lỗi: {ex}");
            }
        }

        private static void StartUdpServer()
        {
            udpServer = new UdpClient(udpPort);
            Console.WriteLine($"✅ UDP Server đang lắng nghe tại cổng {udpPort}...");

            // Bắt đầu nhận dữ liệu UDP bất đồng bộ
            Task.Run(async () =>
            {
                while (isRunning)
                {
                    try
                    {
                        UdpReceiveResult result = await udpServer.ReceiveAsync();
                        string message = System.Text.Encoding.UTF8.GetString(result.Buffer);

                        Console.WriteLine($"📡 Nhận UDP từ {result.RemoteEndPoint}: {message}");

                        // Xử lý tin nhắn UDP
                        string response = ProcessUdpMessage(message);

                        // Gửi phản hồi
                        byte[] responseData = System.Text.Encoding.UTF8.GetBytes(response);
                        await udpServer.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        if (isRunning)
                            Console.WriteLine($"❌ Lỗi nhận dữ liệu UDP: {ex.Message}");
                    }
                }
            });
        }

        private static void StartMulticastServer()
        {
            multicastClient = new UdpClient();
            Console.WriteLine($"✅ Multicast đã được thiết lập tại {multicastAddress}:{multicastPort}");
        }

        private static void BroadcastServerStatus(object state)
        {
            if (!isRunning) return;

            try
            {
                // Gửi thông tin trạng thái server qua UDP broadcast
                using (UdpClient broadcastClient = new UdpClient())
                {
                    broadcastClient.EnableBroadcast = true;

                    string statusMessage = $"SERVER_STATUS|Active|{DateTime.Now}";
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(statusMessage);

                    broadcastClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));
                    Console.WriteLine($"📢 Đã broadcast trạng thái server ({DateTime.Now})");
                }

                // Gửi thông tin trạng thái server qua multicast
                byte[] multicastData = System.Text.Encoding.UTF8.GetBytes($"MULTICAST_STATUS|Active|{DateTime.Now}");
                multicastClient.Send(multicastData, multicastData.Length, new IPEndPoint(multicastAddress, multicastPort));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi broadcast status: {ex.Message}");
            }
        }

        private static string ProcessUdpMessage(string message)
        {
            // Xử lý tin nhắn UDP đơn giản
            if (message.StartsWith("PING"))
            {
                return "PONG|" + DateTime.Now.ToString();
            }
            else if (message.StartsWith("STATUS"))
            {
                return "STATUS|Running|" + DateTime.Now.ToString();
            }
            else if (message.StartsWith("INFO"))
            {
                string[] parts = message.Split('|');
                if (parts.Length > 1)
                {
                    string username = parts[1];
                    int points = CoSoDuLieu.LayDiemNguoiChoi(username);
                    return $"INFO|{points}|5"; // Trả về điểm và 5 lượt quay miễn phí
                }
            }

            return "UNKNOWN_COMMAND";
        }

        private static void ShutdownServer()
        {
            Console.WriteLine("\n🛑 Đang tắt server...");

            // Hủy timer broadcast
            broadcastTimer?.Dispose();

            // Đóng TCP listener
            if (tcpListener != null)
            {
                tcpListener.Stop();
                Console.WriteLine("✅ Đã dừng TCP Server");
            }

            // Đóng UDP server
            if (udpServer != null)
            {
                udpServer.Close();
                Console.WriteLine("✅ Đã dừng UDP Server");
            }

            // Đóng multicast client
            if (multicastClient != null)
            {
                multicastClient.Close();
                Console.WriteLine("✅ Đã dừng Multicast Server");
            }

            Console.WriteLine("👋 Server đã tắt an toàn. Nhấn phím bất kỳ để thoát...");
            Console.ReadKey();
        }
    }
}