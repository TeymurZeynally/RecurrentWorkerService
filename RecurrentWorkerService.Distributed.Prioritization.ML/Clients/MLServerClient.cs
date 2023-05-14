using Newtonsoft.Json;
using RecurrentWorkerService.Distributed.Prioritization.ML.Repository.Models;
using System.Text;

namespace RecurrentWorkerService.Distributed.Prioritization.ML.Clients
{
	internal class MLServerClient
	{
		private readonly HttpClient _httpClient;

		public MLServerClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<Stream> DownloadOnnxModel(Metrics[] metrics, CancellationToken cancellationToken)
		{
			var content = new StringContent(JsonConvert.SerializeObject(metrics), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync("/", content, cancellationToken).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
