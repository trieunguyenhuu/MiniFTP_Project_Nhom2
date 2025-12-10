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
                            _userId = _dbService.CheckLogin(parts[1], parts[2]);
                            if (_userId != -1)
                            {
                                _username = parts[1];
                                _currentFolderId = _dbService.GetUserRootFolderId(_userId);
                                await writer.WriteLineAsync($"LOGIN_SUCCESS|Chào {_username}");
                                _logAction($"User {_username} đã đăng nhập.");
                            }
                            else await writer.WriteLineAsync("LOGIN_FAIL|Sai thông tin");
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
                                string fName = parts[1];
                                long fSize = long.Parse(parts[2]);
                                string phyName = Guid.NewGuid().ToString() + Path.GetExtension(fName);
                                string savePath = Path.Combine(_storageRoot, phyName);

                                await writer.WriteLineAsync("UPLOAD_READY");
                                using (var fs = new FileStream(savePath, FileMode.Create))
                                {
                                    byte[] buf = new byte[8192];
                                    long total = 0;
                                    while (total < fSize)
                                    {
                                        int r = await stream.ReadAsync(buf, 0, (int)Math.Min(buf.Length, fSize - total));
                                        if (r == 0) break;
                                        await fs.WriteAsync(buf, 0, r);
                                        total += r;
                                    }
                                }
                                _dbService.AddFile(_userId, _currentFolderId, fName, fSize, phyName);
                                await writer.WriteLineAsync("UPLOAD_SUCCESS|OK");
                                _logAction($"{_username} upload {fName}");
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
                        }
                    }
                }
            }
            catch (Exception ex) { _logAction($"Lỗi client {ip}: {ex.Message}"); }
            finally { _clientSocket.Close(); }
        }
    }
}