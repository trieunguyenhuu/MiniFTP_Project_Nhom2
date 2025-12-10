using System;
using System.Collections.Generic; // Cần cho List
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;

namespace MiniFtpServer_WPF.Services
{
    public class FtpServer
    {
        private TcpListener _listener;
        private bool _isRunning;
        private DatabaseService _db;
        private Action<string> _logger;

        // --- THÊM MỚI: Hàm báo cáo số lượng client ---
        private Action<int> _countCallback;

        // --- THÊM MỚI: Danh sách quản lý client ---
        private List<Socket> _clients = new List<Socket>();
        private string _storageRoot;

        // Cập nhật Constructor nhận thêm countCallback
        public FtpServer(DatabaseService db, Action<string> logger, Action<int> countCallback)
        {
            _db = db;
            _logger = logger;
            _countCallback = countCallback; // Lưu hàm báo cáo lại
            _storageRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MASTER_STORAGE");
            Directory.CreateDirectory(_storageRoot);
        }

        public void Start()
        {
            if (_isRunning) return;
            _listener = new TcpListener(IPAddress.Any, 9999);
            _listener.Start();
            _isRunning = true;
            _logger("Server đã khởi động...");
            Task.Run(ListenLoop);
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            // Ngắt kết nối tất cả client khi Stop
            lock (_clients)
            {
                foreach (var client in _clients) client.Close();
                _clients.Clear();
            }
            _countCallback(0); // Reset số lượng về 0
            _logger("Server đã dừng.");
        }

        private async Task ListenLoop()
        {
            try
            {
                while (_isRunning)
                {
                    Socket socket = await _listener.AcceptSocketAsync();

                    // --- CẬP NHẬT SỐ LƯỢNG ---
                    lock (_clients)
                    {
                        _clients.Add(socket);
                        _countCallback(_clients.Count); // Báo ra ngoài: "Có thêm người!"
                    }

                    _logger($"Client kết nối: {socket.RemoteEndPoint}");

                    // Tạo Handler và chạy
                    var handler = new ClientHandler(socket, _db, _logger, _storageRoot);

                    // Chạy xử lý và chờ khi nó kết thúc để trừ số lượng
                    _ = Task.Run(async () =>
                    {
                        await handler.Process();
                        // Khi Client thoát (hàm Process chạy xong):
                        lock (_clients)
                        {
                            _clients.Remove(socket);
                            _countCallback(_clients.Count); // Báo ra ngoài: "Bớt 1 người!"
                        }
                    });
                }
            }
            catch { }
        }
    }
}