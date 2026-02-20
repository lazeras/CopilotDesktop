using CopilotDesktop.Core.Contracts.Services;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Threading;

namespace CopilotDesktop.Core.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var fileContent = JsonConvert.SerializeObject(content);
        var path = Path.Combine(folderPath, fileName);

        const int maxAttempts = 5;
        int attempt = 0;

        while (true)
        {
            // Use a unique temp filename each attempt to avoid collisions with other processes
            var tempPath = Path.Combine(folderPath, fileName + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                // Write to a unique temp file
                File.WriteAllText(tempPath, fileContent, Encoding.UTF8);

                // Try to copy/overwrite the destination (retry on transient locks)
                for (int copyAttempt = 0; copyAttempt < maxAttempts; copyAttempt++)
                {
                    try
                    {
                        File.Copy(tempPath, path, overwrite: true);
                        break;
                    }
                    catch (IOException)
                    {
                        if (copyAttempt == maxAttempts - 1) throw;
                        Thread.Sleep(100);
                    }
                }

                // Success
                break;
            }
            catch (IOException)
            {
                attempt++;
                if (attempt >= maxAttempts) throw;
                Thread.Sleep(100);
            }
            finally
            {
                // Best-effort cleanup of the temp file
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }
}