using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Diagnostics;
using Xunit;

namespace IntegrationTests
{
	public class UnitTest1 : IAsyncLifetime
	{
		private IContainer container = default!;
		private HttpClient httpClient = default!;

		public async Task InitializeAsync()
		{
			// Create a new instance of a container.
			container = new ContainerBuilder()
			  // Set the image for the container to "testcontainers/helloworld:1.1.0".
			  .WithImage("testcontainers/helloworld:1.1.0")
			  // Bind port 8080 of the container to a random port on the host.
			  .WithPortBinding(8080, true)
			  // Wait until the HTTP endpoint of the container is available.
			  .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080)))
			  // Build the container configuration.
			  .Build();

			// Start the container.
			await container.StartAsync()
			  .ConfigureAwait(false);

			// Initialize HttpClient.
			httpClient = new HttpClient();
		}

		public async Task DisposeAsync()
		{
			// Dispose HttpClient.
			httpClient.Dispose();

			// Stop and dispose the container.
			await container.StopAsync();
			await container.DisposeAsync();
		}

		[Fact]
		public async Task Test1()
		{
			// Ensure the container is running.
			Assert.True(container.State == TestcontainersStates.Running, "The container is not running.");

			// Construct the request URI by specifying the scheme, hostname, assigned random host port, and the endpoint "uuid".
			var requestUri = new UriBuilder(Uri.UriSchemeHttp, container.Hostname, container.GetMappedPublicPort(8080), "uuid").Uri;

			// Send an HTTP GET request to the specified URI and retrieve the response as a string.
			var guid = await httpClient.GetStringAsync(requestUri)
			  .ConfigureAwait(false);

			// Ensure that the retrieved UUID is a valid GUID.
			Assert.True(Guid.TryParse(guid, out _), "The retrieved UUID is not a valid GUID.");
		}
	}
}
