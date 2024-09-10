using System.IO;
using Microsoft.AspNetCore.Http;

namespace WendlandtVentas.Web.Extensions
{
    public static class FormFileExtensions
    {
        public static byte[] ToByteArray(this IFormFile formFile)
        {
            using (var stream = new MemoryStream())
            {
                formFile.CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}