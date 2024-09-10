using System.Collections.Generic;
using System.Threading.Tasks;

namespace WendlandtVentas.Core.Interfaces
{
    public interface IFileService
    {
        string SavePhoto(byte[] photo, string folder = "images", int maxWidth = 800);

        void DeletePhotos(string[] filePaths);

        void DeleteFile(string fileName, string mediaFolder);

        Task<string> SavePhotoInFolder(byte[] photoData, string mediaFolder);

        string DownloadPhoto(string url);

        string ReplaceWords(Dictionary<string, string> Words, string wordDocument);
    }
}