using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CaroClient.Network;
using CaroShared.Contracts;

namespace CaroClient.Settings
{
    public class AvatarManager : IDisposable
    {
        private static AvatarManager? _instance;
        public static AvatarManager Instance => _instance ??= new AvatarManager();

        // Caches Avatar Images per PlayerId. Uses Image object.
        private readonly ConcurrentDictionary<string, Image> _avatarCache = new();
        private readonly ConcurrentDictionary<string, int> _avatarVersions = new();
        
        // In-flight requests to avoid spamming the server
        private readonly ConcurrentDictionary<string, bool> _pendingRequests = new();

        public event Action<string, Image?>? OnAvatarUpdated;

        private AvatarManager()
        {
            NetworkClient.Instance.OnAvatarDataReceived += HandleAvatarDataReceived;
            NetworkClient.Instance.OnAvatarChanged += HandleAvatarChanged;
        }

        public void Dispose()
        {
            NetworkClient.Instance.OnAvatarDataReceived -= HandleAvatarDataReceived;
            NetworkClient.Instance.OnAvatarChanged -= HandleAvatarChanged;

            foreach (var img in _avatarCache.Values)
            {
                img.Dispose();
            }
            _avatarCache.Clear();
            _avatarVersions.Clear();
            _pendingRequests.Clear();
        }

        public Image? GetAvatar(string playerId, int version, bool hasAvatar)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            // Always check cache first. If we have a newer or same version, return it.
            if (_avatarVersions.TryGetValue(playerId, out int cachedVersion))
            {
                if (cachedVersion >= version && _avatarCache.TryGetValue(playerId, out Image? img))
                {
                    // Return a clone to avoid GDI+ locking issues across UI threads
                    return (Image)img.Clone();
                }
            }

            if (!hasAvatar)
            {
                return null;
            }

            // If not cached or version mismatch, request it
            if (_pendingRequests.TryAdd(playerId, true))
            {
                _ = NetworkClient.Instance.SendAvatarRequestAsync(playerId);
            }

            return null; // Return null while loading
        }

        private void HandleAvatarChanged(AvatarChangedEvent ace)
        {
            if (!ace.HasAvatar)
            {
                if (_avatarCache.TryRemove(ace.PlayerId, out var oldImg))
                {
                    oldImg.Dispose();
                }
                _avatarVersions[ace.PlayerId] = ace.AvatarVersion;
                OnAvatarUpdated?.Invoke(ace.PlayerId, null);
            }
            else
            {
                // Force a fetch
                if (_pendingRequests.TryAdd(ace.PlayerId, true))
                {
                    _ = NetworkClient.Instance.SendAvatarRequestAsync(ace.PlayerId);
                }
            }
        }

        private void HandleAvatarDataReceived(AvatarDataEvent ade)
        {
            _pendingRequests.TryRemove(ade.PlayerId, out _);

            try
            {
                byte[] bytes = Convert.FromBase64String(ade.Base64Image);
                using var ms = new MemoryStream(bytes);
                using var originalImg = Image.FromStream(ms);
                
                // Copy to a new bitmap to fully release the stream
                var safeImg = new Bitmap(originalImg);

                if (_avatarCache.TryRemove(ade.PlayerId, out var oldImg))
                {
                    oldImg.Dispose();
                }

                _avatarCache[ade.PlayerId] = safeImg;
                _avatarVersions[ade.PlayerId] = ade.AvatarVersion;

                // Subscribers may use this only during the callback, or retain their own clone.
                using var notificationImage = (Image)safeImg.Clone();
                OnAvatarUpdated?.Invoke(ade.PlayerId, notificationImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AvatarManager] Error decoding avatar for {ade.PlayerId}: {ex.Message}");
            }
        }

        public static string ProcessAndEncodeAvatar(string filePath)
        {
            // Validate extension
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string[] validExts = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            if (!validExts.Contains(ext))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ ảnh PNG, JPG, JPEG, BMP, GIF.");
            }

            // Load original image safely
            using var original = Image.FromFile(filePath);
            
            // For GIF, only take the first frame (stops animation)
            if (ImageAnimator.CanAnimate(original))
            {
                original.SelectActiveFrame(new FrameDimension(original.FrameDimensionsList[0]), 0);
            }

            // Determine dimensions for Center Crop
            int minDim = Math.Min(original.Width, original.Height);
            Rectangle cropRect = new Rectangle(
                (original.Width - minDim) / 2,
                (original.Height - minDim) / 2,
                minDim,
                minDim
            );

            int targetSize = 256;
            using var bmp256 = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp256))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, targetSize, targetSize), cropRect, GraphicsUnit.Pixel);
            }

            // Try encoding to PNG
            using var ms = new MemoryStream();
            bmp256.Save(ms, ImageFormat.Png);
            byte[] bytes = ms.ToArray();

            // Check limit (256KB)
            if (bytes.Length <= 262144)
            {
                return Convert.ToBase64String(bytes);
            }

            // Fallback to 192x192 if too large
            targetSize = 192;
            using var bmp192 = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            using (var g2 = Graphics.FromImage(bmp192))
            {
                g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g2.SmoothingMode = SmoothingMode.HighQuality;
                g2.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g2.DrawImage(original, new Rectangle(0, 0, targetSize, targetSize), cropRect, GraphicsUnit.Pixel);
            }
            
            using var ms192 = new MemoryStream();
            bmp192.Save(ms192, ImageFormat.Png);
            byte[] bytes192 = ms192.ToArray();
            
            if (bytes192.Length > 262144)
            {
                throw new InvalidOperationException("Ảnh quá lớn ngay cả sau khi thu nhỏ. Vui lòng chọn ảnh khác.");
            }

            return Convert.ToBase64String(bytes192);
        }
    }
}
