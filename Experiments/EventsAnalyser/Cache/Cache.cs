using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace EventsAnalyser.Cache
{
	internal class Cache
	{
		public static async Task<IList<T>> Get<T>(string key, Func<Task<IList<T>>> function)
		{
			var hashBytes = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
			var builder = new StringBuilder();
			foreach (var t in hashBytes) builder.Append(t.ToString("x2"));
			var keyHash = builder.ToString();

			var cacheFile = Path.Combine("Cache", keyHash);

			if (File.Exists(cacheFile))
			{

				return JsonConvert.DeserializeObject<IList<T>>(await File.ReadAllTextAsync(cacheFile).ConfigureAwait(false))!;
			}

			var result = await function().ConfigureAwait(false);

			if (!Path.Exists(cacheFile))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			}
			
			await File.WriteAllTextAsync(cacheFile, JsonConvert.SerializeObject(result)).ConfigureAwait(false);
			return result;
		}
	}
}
