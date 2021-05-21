using System.Collections.Generic;
using System.IO;

namespace ErpNet.Api.Client
{
    /// <summary>
    /// Represents a secret storage of access token. When an access token is obtained it must be persisted for later usage.
    /// </summary>
    public interface IAccessTokenStore
    {
        /// <summary>
        /// Tries to get the stored access token for a given database.
        /// </summary>
        /// <param name="databaseUrl"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        bool TryGetAccessToken(string databaseUrl, out string? token);

        /// <summary>
        /// Stores the access token for a given database.
        /// </summary>
        /// <param name="databaseUrl"></param>
        /// <param name="token"></param>
        void SetAccessToken(string databaseUrl, string? token);
    }

    /// <summary>
    /// Represents a <see cref="IAccessTokenStore"/> that stores the access token in text file as a plain text.
    /// </summary>
    public class FileAccessTokenStore : IAccessTokenStore
    {
        private object syncRoot = new object();

        /// <summary>
        /// Creates an instance of <see cref="FileAccessTokenStore"/>
        /// </summary>
        /// <param name="fileName">the file path</param>
        public FileAccessTokenStore(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// The file path.
        /// </summary>
        public string FileName { get; }


        ///<inheritdoc/>
        public void SetAccessToken(string databaseUrl, string? token)
        {
            var data = ReadFile() ?? new Dictionary<string, string>();
            if (token == null)
                data.Remove(databaseUrl);
            else
                data[databaseUrl] = token;

            WriteFile(data);
        }
        ///<inheritdoc/>
        public bool TryGetAccessToken(string databaseUrl, out string? token)
        {
            var data = ReadFile();
            if (data != null && data.TryGetValue(databaseUrl, out token))
                return true;
            token = null;
            return false;
        }

        private IDictionary<string, string>? ReadFile()
        {
            string json;
            lock (syncRoot)
            {
                if (!File.Exists(FileName))
                    return null;
                json = File.ReadAllText(FileName);
            }
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }


        private void WriteFile(IDictionary<string, string> data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            lock (syncRoot)
            {
                File.WriteAllText(FileName, json);
            }
        }
    }
}