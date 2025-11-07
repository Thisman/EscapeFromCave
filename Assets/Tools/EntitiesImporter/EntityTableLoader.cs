using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.EntitiesImporter
{
    public class EntityTableLoader
    {
        private static readonly HttpClient SharedClient = new HttpClient();

        public virtual async Task<string> LoadTableAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL must not be null or whitespace.", nameof(url));
            }

            using (var response = await SharedClient.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
