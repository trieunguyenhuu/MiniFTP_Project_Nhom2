using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniFtpServer_WPF.Services
{
    public static class FtpCommands
    {
        public const string WELCOME = "WELCOME";
        public const string LOGIN = "LOGIN";
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
        public const string LOGIN_FAIL = "LOGIN_FAIL";
        public const string LIST = "LIST";
        public const string LIST_SUCCESS = "LIST_SUCCESS";
        public const string MKDIR = "MKDIR";
        public const string MKDIR_SUCCESS = "MKDIR_SUCCESS";
        public const string UPLOAD = "UPLOAD";
        public const string UPLOAD_READY = "UPLOAD_READY";
        public const string UPLOAD_SUCCESS = "UPLOAD_SUCCESS";
        public const string DOWNLOAD = "DOWNLOAD";
        public const string DOWNLOAD_READY = "DOWNLOAD_READY";
        public const string DELETE = "DELETE";
        public const string DELETE_SUCCESS = "DELETE_SUCCESS";
        public const string GET_USERS = "GET_USERS";
        public const string USERS_LIST = "USERS_LIST";
        public const string CHANGE_PASSWORD = "CHANGE_PASSWORD";
        public const string QUIT = "QUIT";
        public const string LOGOUT = "LOGOUT";
        public const string ERROR = "ERROR";

        public const string GET_TRASH = "GET_TRASH";
        public const string RESTORE_FILE = "RESTORE_FILE";
        public const string PERMANENT_DELETE = "PERMANENT_DELETE";
        public const string EMPTY_TRASH = "EMPTY_TRASH";

        // share file
        public const string SHARE_FILE = "SHARE_FILE";
        public const string SHARE_SUCCESS = "SHARE_SUCCESS";
        public const string GET_SHARED_FILES = "GET_SHARED_FILES";
        public const string SHARED_FILES_LIST = "SHARED_FILES_LIST";
        public const string GET_SHARED_WITH = "GET_SHARED_WITH";
        public const string UNSHARE_FILE = "UNSHARE_FILE";
        public const string CHECK_ACCESS = "CHECK_ACCESS";
        public const string SHARE_FILE_BY_NAME = "SHARE_FILE_BY_NAME";

        public const string CWD = "CWD"; // thay đổi thư mục làm việc
        public const string CWD_SUCCESS = "CWD_SUCCESS";
    }

    public class ClientHandler
    {
        private Socket _clientSocket;
        private DatabaseService _dbService;
        private Action<string> _logAction;
        private string _storageRoot;

        private int _userId = -1;
        private int _currentFolderId = -1;
        private string _username = "";

        private const long MAX_FILE_SIZE = 100 * 1024 * 1024;

        public ClientHandler(Socket socket, DatabaseService db, Action<string> log, string storage)
        {
            _clientSocket = socket;
            _dbService = db;
            _logAction = log;
            _storageRoot = storage;
        }

        public async Task Process()
        {
            string ip = _clientSocket.RemoteEndPoint?.ToString() ?? "Unknown";

            try
            {
                using (var stream = new NetworkStream(_clientSocket, ownsSocket: false))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    await writer.WriteLineAsync($"{FtpCommands.WELCOME}|Vui lòng đăng nhập.");

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        string[] parts = line.Split('|');
                        string cmd = parts[0];

                        if (cmd == FtpCommands.LOGIN)
                        {
                            if (parts.Length < 3)
                            {
                                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin đăng nhập");
                                continue;
                            }

                            var userInfo = _dbService.CheckLoginGetInfo(parts[1], parts[2]);
                            if (userInfo != null)
                            {
                                _userId = userInfo.Item1;
                                _username = parts[1];
                                string fullName = userInfo.Item2;
                                string email = userInfo.Item3;
                                string description = userInfo.Item4;

                                _currentFolderId = _dbService.GetUserRootFolderId(_userId);

                                await writer.WriteLineAsync($"{FtpCommands.LOGIN_SUCCESS}|{fullName}|{email}|{description}");
                                _logAction($"✓ User {_username} ({fullName}) đã đăng nhập từ {ip}");
                            }
                            else
                            {
                                await writer.WriteLineAsync($"{FtpCommands.LOGIN_FAIL}|Sai tài khoản hoặc mật khẩu");
                                _logAction($"✗ Đăng nhập thất bại từ {ip}");
                            }
                            continue;
                        }

                        if (_userId == -1)
                        {
                            await writer.WriteLineAsync($"{FtpCommands.ERROR}|Chưa đăng nhập");
                            continue;
                        }

                        switch (cmd)
                        {
                            case FtpCommands.LIST:
                                await HandleList(writer);
                                break;

                            case FtpCommands.GET_TRASH:
                                await HandleGetTrash(writer);
                                break;

                            case FtpCommands.RESTORE_FILE:
                                await HandleRestore(parts, writer);
                                break;

                            case FtpCommands.PERMANENT_DELETE:
                                await HandlePermanentDelete(parts, writer);
                                break;

                            case FtpCommands.EMPTY_TRASH:
                                await HandleEmptyTrash(writer);
                                break;

                            case FtpCommands.MKDIR:
                                await HandleMkdir(parts, writer);
                                break;

                            case FtpCommands.UPLOAD:
                                await HandleUpload(parts, stream, writer);
                                break;

                            case FtpCommands.DOWNLOAD:
                                await HandleDownload(parts, stream, writer);
                                break;

                            case FtpCommands.DELETE:
                                await HandleDelete(parts, writer);
                                break;

                            case FtpCommands.GET_USERS:
                                await HandleGetUsers(writer);
                                break;

                            case FtpCommands.CHANGE_PASSWORD:
                                await HandleChangePassword(parts, writer);
                                break;

                            case FtpCommands.QUIT:
                            case FtpCommands.LOGOUT:
                                _logAction($"→ User {_username} đã đăng xuất");
                                return;

                            case FtpCommands.SHARE_FILE:
                                await HandleShareFile(parts, writer);
                                break;

                            case FtpCommands.GET_SHARED_FILES:
                                await HandleGetSharedFiles(writer);
                                break;

                            case FtpCommands.GET_SHARED_WITH:
                                await HandleGetSharedWith(parts, writer);
                                break;

                            case FtpCommands.UNSHARE_FILE:
                                await HandleUnshare(parts, writer);
                                break;

                            case FtpCommands.CHECK_ACCESS:
                                await HandleCheckAccess(parts, writer);
                                break;

                            case FtpCommands.SHARE_FILE_BY_NAME:
                                await HandleShareFileByName(parts, writer);
                                break;

                            case FtpCommands.CWD:
                                await HandleCwd(parts, writer);
                                break;

                            default:
                                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lệnh không hợp lệ");
                                break;
                        }
                    }
                }
            }
            catch (IOException)
            {
                _logAction($"✗ Client {ip} ngắt kết nối đột ngột");
            }
            catch (ObjectDisposedException)
            {
                _logAction($"✗ Kết nối tới {ip} đã đóng");
            }
            catch (Exception ex)
            {
                _logAction($"✗ Lỗi nghiêm trọng từ {ip}: {ex.Message}");
            }
            finally
            {
                try
                {
                    _clientSocket?.Shutdown(SocketShutdown.Both);
                    _clientSocket?.Close();
                    _clientSocket?.Dispose();
                }
                catch { }
            }
        }

        private async Task HandleList(StreamWriter writer)
        {
            try
            {
                string data = _dbService.GetFileList(_userId, _currentFolderId);
                await writer.WriteLineAsync($"{FtpCommands.LIST_SUCCESS}|{data}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi lấy danh sách: {ex.Message}");
            }
        }

        private async Task HandleGetTrash(StreamWriter writer)
        {
            try
            {
                var files = _dbService.GetDeletedFiles(_userId);
                StringBuilder sb = new StringBuilder();

                foreach (var file in files)
                {
                    // Format: FILE:id:name:size:deletedDate;
                    sb.Append($"FILE|{file.Item1}|{file.Item2}|{file.Item3}|{file.Item4:yyyy-MM-dd HH:mm:ss};");
                }

                await writer.WriteLineAsync($"TRASH_LIST|{sb}");
                _logAction($"📂 {_username} xem thùng rác");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi lấy thùng rác: {ex.Message}");
            }
        }

        private async Task HandleRestore(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu ID file");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                bool ok = _dbService.RestoreFile(fileId);

                if (ok)
                {
                    await writer.WriteLineAsync("RESTORE_SUCCESS|Khôi phục thành công");
                    _logAction($"♻ {_username} khôi phục file ID: {fileId}");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể khôi phục");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi khôi phục: {ex.Message}");
            }
        }

        private async Task HandlePermanentDelete(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu ID file");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                bool ok = _dbService.PermanentDeleteFile(fileId);

                if (ok)
                {
                    await writer.WriteLineAsync("PERMANENT_DELETE_SUCCESS|Đã xóa vĩnh viễn");
                    _logAction($"🗑 {_username} xóa vĩnh viễn file ID: {fileId}");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể xóa vĩnh viễn");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi xóa vĩnh viễn: {ex.Message}");
            }
        }

        private async Task HandleEmptyTrash(StreamWriter writer)
        {
            try
            {
                bool ok = _dbService.EmptyTrash(_userId);

                if (ok)
                {
                    await writer.WriteLineAsync("EMPTY_TRASH_SUCCESS|Đã dọn dẹp thùng rác");
                    _logAction($"🧹 {_username} đã dọn dẹp thùng rác");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể dọn dẹp");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi dọn dẹp: {ex.Message}");
            }
        }

        private async Task HandleMkdir(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Tên thư mục không hợp lệ");
                    return;
                }

                string folderName = SanitizeFileName(parts[1]);
                _dbService.AddFolder(_userId, _currentFolderId, folderName);
                await writer.WriteLineAsync($"{FtpCommands.MKDIR_SUCCESS}|Tạo thư mục thành công");
                _logAction($"📁 {_username} tạo folder: {folderName}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi tạo thư mục: {ex.Message}");
            }
        }

        private async Task HandleUpload(string[] parts, NetworkStream stream, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin file");
                    return;
                }

                string fileName = SanitizeFileName(parts[1]);
                long size = long.Parse(parts[2]);

                if (size > MAX_FILE_SIZE)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|File quá lớn (tối đa 100MB)");
                    return;
                }

                if (size <= 0)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Kích thước file không hợp lệ");
                    return;
                }

                string userFolder = Path.Combine(_storageRoot, _userId.ToString());
                Directory.CreateDirectory(userFolder);

                string physicalName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
                string savePath = Path.Combine(userFolder, physicalName);

                await writer.WriteLineAsync(FtpCommands.UPLOAD_READY);

                using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192];
                    long totalReceived = 0;

                    while (totalReceived < size)
                    {
                        int toRead = (int)Math.Min(buffer.Length, size - totalReceived);
                        int received = await stream.ReadAsync(buffer, 0, toRead);

                        if (received == 0)
                        {
                            throw new IOException("Kết nối bị ngắt trong quá trình upload");
                        }

                        await fs.WriteAsync(buffer, 0, received);
                        totalReceived += received;
                    }
                }

                _dbService.AddFile(_userId, _currentFolderId, fileName, size, physicalName);
                await writer.WriteLineAsync($"{FtpCommands.UPLOAD_SUCCESS}|Upload thành công");
                _logAction($"⬆ {_username} upload: {fileName} ({FormatFileSize(size)})");
            }
            catch (IOException ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi lưu file: {ex.Message}");
                _logAction($"✗ Upload thất bại: {ex.Message}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi upload: {ex.Message}");
                _logAction($"✗ Upload lỗi: {ex.Message}");
            }
        }

        private async Task HandleDownload(string[] parts, NetworkStream stream, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu ID file");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                var info = _dbService.GetFileInfo(fileId);

                if (info == null)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|File không tồn tại trong DB");
                    return;
                }

                string storagePath = Path.Combine(_storageRoot, _userId.ToString(), info.Item1);

                if (!File.Exists(storagePath))
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|File vật lý không tồn tại");
                    return;
                }

                long fileSize = new FileInfo(storagePath).Length;
                await writer.WriteLineAsync($"{FtpCommands.DOWNLOAD_READY}|{fileSize}");

                using (var fs = new FileStream(storagePath, FileMode.Open, FileAccess.Read))
                {
                    await fs.CopyToAsync(stream);
                }

                _logAction($"⬇ {_username} download: {info.Item2} ({FormatFileSize(fileSize)})");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi download: {ex.Message}");
                _logAction($"✗ Download lỗi: {ex.Message}");
            }
        }

        private async Task HandleDelete(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu ID file");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                _dbService.SoftDeleteFile(fileId);
                await writer.WriteLineAsync($"{FtpCommands.DELETE_SUCCESS}|Đã chuyển vào thùng rác");
                _logAction($"🗑 {_username} xóa file ID: {fileId}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi xóa: {ex.Message}");
            }
        }

        private async Task HandleGetUsers(StreamWriter writer)
        {
            try
            {
                string users = _dbService.GetAllUsers();
                await writer.WriteLineAsync($"{FtpCommands.USERS_LIST}|{users}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi lấy danh sách user: {ex.Message}");
            }
        }

        private async Task HandleChangePassword(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin mật khẩu");
                    return;
                }

                string currentPwd = parts[1];
                string newPwd = parts[2];

                // Gọi hàm VerifyPassword từ DatabaseService
                bool isValid = _dbService.VerifyPassword(_userId, currentPwd);

                if (!isValid)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Mật khẩu hiện tại không đúng");
                    _logAction($"✗ {_username} thử đổi mật khẩu nhưng sai mật khẩu cũ");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 6)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Mật khẩu mới phải có ít nhất 6 ký tự");
                    return;
                }

                // Gọi hàm UpdatePassword từ DatabaseService
                bool updated = _dbService.UpdatePassword(_userId, newPwd);

                if (updated)
                {
                    await writer.WriteLineAsync("CHANGE_PASSWORD_SUCCESS|Đổi mật khẩu thành công");
                    _logAction($"✓ {_username} đã đổi mật khẩu");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể cập nhật mật khẩu");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi đổi mật khẩu: {ex.Message}");
                _logAction($"✗ Lỗi đổi mật khẩu: {ex.Message}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            string safe = Path.GetFileName(fileName);
            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char c in invalidChars)
            {
                safe = safe.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(safe) ? "unnamed" : safe;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        // ======================share file=======================
        private async Task HandleShareFile(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin chia sẻ");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                int targetUserId = int.Parse(parts[2]);
                string accessLevel = parts.Length > 3 ? parts[3] : "READ";

                // Validate access level
                if (accessLevel != "READ" && accessLevel != "WRITE")
                {
                    accessLevel = "READ";
                }

                // Không cho phép share với chính mình
                if (targetUserId == _userId)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể chia sẻ với chính mình");
                    return;
                }

                bool success = _dbService.ShareFile(fileId, _userId, targetUserId, accessLevel);

                if (success)
                {
                    await writer.WriteLineAsync($"{FtpCommands.SHARE_SUCCESS}|Chia sẻ thành công");
                    _logAction($"📤 {_username} chia sẻ file ID:{fileId} với user ID:{targetUserId} ({accessLevel})");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể chia sẻ file");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi chia sẻ: {ex.Message}");
                _logAction($"✗ Lỗi share file: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy danh sách file được người khác share cho mình
        /// </summary>
        private async Task HandleGetSharedFiles(StreamWriter writer)
        {
            try
            {
                var files = _dbService.GetSharedFiles(_userId);
                StringBuilder sb = new StringBuilder();

                foreach (var file in files)
                {
                    // Format: id|name|size|access|owner;
                    sb.Append($"{file.Item1}|{file.Item2}|{file.Item3}|{file.Item4}|{file.Item5};");
                }

                await writer.WriteLineAsync($"{FtpCommands.SHARED_FILES_LIST}|{sb}");
                _logAction($"📥 {_username} xem file được chia sẻ");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy danh sách user đã được share file này
        /// Format: GET_SHARED_WITH|fileId
        /// </summary>
        private async Task HandleGetSharedWith(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu file ID");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                var users = _dbService.GetSharedWithUsers(fileId);
                StringBuilder sb = new StringBuilder();

                foreach (var user in users)
                {
                    // Format: userId|fullName|accessLevel;
                    sb.Append($"{user.Item1}|{user.Item2}|{user.Item3};");
                }

                await writer.WriteLineAsync($"SHARED_WITH_LIST|{sb}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Hủy chia sẻ file
        /// Format: UNSHARE_FILE|fileId|userId
        /// </summary>
        private async Task HandleUnshare(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                int targetUserId = int.Parse(parts[2]);

                bool success = _dbService.UnshareFile(fileId, targetUserId);

                if (success)
                {
                    await writer.WriteLineAsync("UNSHARE_SUCCESS|Đã hủy chia sẻ");
                    _logAction($"🔒 {_username} hủy share file ID:{fileId} với user ID:{targetUserId}");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể hủy chia sẻ");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra quyền truy cập file
        /// Format: CHECK_ACCESS|fileId
        /// </summary>
        private async Task HandleCheckAccess(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu file ID");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                string access = _dbService.CheckFileAccess(fileId, _userId);

                await writer.WriteLineAsync($"ACCESS_LEVEL|{access}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi: {ex.Message}");
            }
        }

        private async Task HandleShareFileByName(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 3)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu thông tin chia sẻ");
                    return;
                }

                int fileId = int.Parse(parts[1]);
                string targetFullName = parts[2];
                string accessLevel = parts.Length > 3 ? parts[3] : "READ";

                // Validate access level
                if (accessLevel != "READ" && accessLevel != "WRITE")
                {
                    accessLevel = "READ";
                }

                // Tìm user_id từ full_name
                int? targetUserId = _dbService.GetUserIdByFullName(targetFullName);

                if (targetUserId == null)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không tìm thấy người dùng '{targetFullName}'");
                    return;
                }

                // Không cho phép share với chính mình
                if (targetUserId.Value == _userId)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể chia sẻ với chính mình");
                    return;
                }

                bool success = _dbService.ShareFile(fileId, _userId, targetUserId.Value, accessLevel);

                if (success)
                {
                    await writer.WriteLineAsync($"{FtpCommands.SHARE_SUCCESS}|Đã chia sẻ với {targetFullName}");
                    _logAction($"📤 {_username} chia sẻ file ID:{fileId} với {targetFullName} ({accessLevel})");
                }
                else
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Không thể chia sẻ file");
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi chia sẻ: {ex.Message}");
                _logAction($"✗ Lỗi share file by name: {ex.Message}");
            }
        }

        private async Task HandleCwd(string[] parts, StreamWriter writer)
        {
            try
            {
                if (parts.Length < 2)
                {
                    await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thiếu tên thư mục");
                    return;
                }

                string target = parts[1];

                // Trường hợp 1: Quay lại thư mục cha ("..")
                if (target == "..")
                {
                    int? parentId = _dbService.GetParentId(_currentFolderId);
                    if (parentId != null)
                    {
                        _currentFolderId = parentId.Value;
                        await writer.WriteLineAsync($"{FtpCommands.CWD_SUCCESS}|Đã quay lại");
                    }
                    else
                    {
                        // Nếu parentId là null, tức là đang ở Root, không lùi được nữa (hoặc giữ nguyên Root)
                        // Logic tùy chọn: Giữ nguyên hoặc báo lỗi. Ở đây ta giữ nguyên.
                        await writer.WriteLineAsync($"{FtpCommands.CWD_SUCCESS}|Đang ở thư mục gốc");
                    }
                }
                // Trường hợp 2: Đi vào thư mục con
                else
                {
                    int? newFolderId = _dbService.GetFolderIdByName(_userId, _currentFolderId, target);
                    if (newFolderId != null)
                    {
                        _currentFolderId = newFolderId.Value;
                        await writer.WriteLineAsync($"{FtpCommands.CWD_SUCCESS}|Đã chuyển thư mục");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"{FtpCommands.ERROR}|Thư mục không tồn tại");
                    }
                }
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"{FtpCommands.ERROR}|Lỗi chuyển thư mục: {ex.Message}");
            }
        }

    }
}