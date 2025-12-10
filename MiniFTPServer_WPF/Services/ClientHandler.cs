using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniFtpServer_WPF.Services
{
    public class ClientHandler
    {
        private Socket _clientSocket;
        private DatabaseService _dbService;
        private Action<string> _logAction;
        private string _storageRoot;

        // Trạng thái client
        private int _userId = -1;
        private int _currentFolderId = -1;
        private string _username = "";

        public ClientHandler(Socket socket, DatabaseService db, Action<string> log, string storage)
        {
            _clientSocket = socket;
            _dbService = db;
            _logAction = log;
            _storageRoot = storage;
        }

        public async Task Process()
        {
            string ip = _clientSocket.RemoteEndPoint.ToString();
            try
            {
                using (var stream = new NetworkStream(_clientSocket))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    await writer.WriteLineAsync("WELCOME|Vui lòng đăng nhập.");

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string[] parts = line.Split('|');
                        string cmd = parts[0];

                        if (cmd == "LOGIN")
                        {
                            var userInfo = _dbService.CheckLoginGetInfo(parts[1], parts[2]); 
                            if (userInfo != null)
                            {
                                _userId = userInfo.Item1;
                                _username = parts[1]; // username đăng nhập
                                string fullName = userInfo.Item2; // Tên hiển thị

                                _currentFolderId = _dbService.GetUserRootFolderId(_userId);

                                // Trả về: LOGIN_SUCCESS|TenHienThi
                                await writer.WriteLineAsync($"LOGIN_SUCCESS|{fullName}");
                                _logAction($"User {_username} ({fullName}) đã đăng nhập.");
                            }
                            else { await writer.WriteLineAsync("LOGIN_FAIL|Sai tài khoản"); }
                            continue;
                        }

                        if (_userId == -1) { await writer.WriteLineAsync("ERROR|Chưa đăng nhập"); continue; }

                        switch (cmd)
                        {
                            case "LIST":
                                string data = _dbService.GetFileList(_userId, _currentFolderId);
                                await writer.WriteLineAsync($"LIST_SUCCESS|{data}");
                                break;

                            case "MKDIR":
                                _dbService.AddFolder(_userId, _currentFolderId, parts[1]);
                                await writer.WriteLineAsync("MKDIR_SUCCESS|Xong");
                                _logAction($"{_username} tạo folder {parts[1]}");
                                break;

                            case "UPLOAD": // UPLOAD|Name|Size
                                string fileName = parts[1];
                                long size = long.Parse(parts[2]);

                                // Tạo folder riêng cho User nếu chưa có: MASTER_STORAGE/UserID/
                                string userFolder = Path.Combine(_storageRoot, _userId.ToString());
                                Directory.CreateDirectory(userFolder);

                                string physicalName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
                                string savePath = Path.Combine(userFolder, physicalName); // <-- Lưu vào folder con

                                await writer.WriteLineAsync("UPLOAD_READY");
                                using (var fs = new FileStream(savePath, FileMode.Create))
                                {
                                    byte[] buf = new byte[8192];
                                    long total = 0;
                                    while (total < size)
                                    {
                                        int r = await stream.ReadAsync(buf, 0, (int)Math.Min(buf.Length, size - total));
                                        if (r == 0) break;
                                        await fs.WriteAsync(buf, 0, r);
                                        total += r;
                                    }
                                }
                                _dbService.AddFile(_userId, _currentFolderId, fileName, size, physicalName);
                                await writer.WriteLineAsync("UPLOAD_SUCCESS|Thành công");
                                break;

                            case "DOWNLOAD": // DOWNLOAD|FileID
                                int fId = int.Parse(parts[1]);
                                var info = _dbService.GetFileInfo(fId);
                                if (info != null && File.Exists(Path.Combine(_storageRoot, info.Item1)))
                                {
                                    string p = Path.Combine(_storageRoot, info.Item1);
                                    await writer.WriteLineAsync($"DOWNLOAD_READY|{new FileInfo(p).Length}");
                                    using (var fs = new FileStream(p, FileMode.Open)) await fs.CopyToAsync(stream);
                                }
                                else await writer.WriteLineAsync("ERROR|Không tìm thấy file");
                                break;

                            case "DELETE": // DELETE|FileID
                                int delId = int.Parse(parts[1]);
                                _dbService.SoftDeleteFile(delId);
                                await writer.WriteLineAsync("DELETE_SUCCESS|Đã chuyển vào thùng rác");
                                break;

                            // CASE GET_USERS (Mới - Cho nút Share)
                            case "GET_USERS":
                                string users = _dbService.GetAllUsers();
                                await writer.WriteLineAsync($"USERS_LIST|{users}");
                                break;
                        }
                        if (cmd == "QUIT" || cmd == "LOGOUT")
                        {
                            _logAction($"User {_username} đã đăng xuất.");
                            return; // Thoát hàm Process -> Xuống finally đóng socket
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Đây là lỗi khi Client tắt đột ngột hoặc Server Stop
                _logAction($"Client {ip} đã ngắt kết nối.");
            }
            catch (ObjectDisposedException)
            {
                // Lỗi khi server stop và object bị hủy
                _logAction($"Kết nối tới {ip} đã đóng.");
            }
            // -----------------------------
            catch (Exception ex)
            {
                // Chỉ báo lỗi với những lỗi lạ khác
                _logAction($"Lỗi lạ client {ip}: {ex.Message}");
            }
            finally
            {
                _clientSocket.Close();
            }
        }
    }
}