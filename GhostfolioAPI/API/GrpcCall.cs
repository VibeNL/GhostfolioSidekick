using Grpc.Net.Client;
using Grpc.Core;
using System.Text.Json;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
    public class GrpcCall
    {
        private readonly GrpcChannel channel;
        private readonly MyGrpcService.MyGrpcServiceClient client;

        public GrpcCall(string grpcUrl)
        {
            channel = GrpcChannel.ForAddress(grpcUrl);
            client = new MyGrpcService.MyGrpcServiceClient(channel);
        }

        public async Task<string?> DoGrpcGet(string methodName, string requestJson)
        {
            var request = new GrpcRequest { JsonPayload = requestJson };
            var response = await client.GetAsync(new GrpcRequest { MethodName = methodName, JsonPayload = requestJson });
            return response.JsonPayload;
        }

        public async Task<string?> DoGrpcPost(string methodName, string requestJson)
        {
            var request = new GrpcRequest { JsonPayload = requestJson };
            var response = await client.PostAsync(new GrpcRequest { MethodName = methodName, JsonPayload = requestJson });
            return response.JsonPayload;
        }

        public async Task<string?> DoGrpcPut(string methodName, string requestJson)
        {
            var request = new GrpcRequest { JsonPayload = requestJson };
            var response = await client.PutAsync(new GrpcRequest { MethodName = methodName, JsonPayload = requestJson });
            return response.JsonPayload;
        }

        public async Task<string?> DoGrpcPatch(string methodName, string requestJson)
        {
            var request = new GrpcRequest { JsonPayload = requestJson };
            var response = await client.PatchAsync(new GrpcRequest { MethodName = methodName, JsonPayload = requestJson });
            return response.JsonPayload;
        }

        public async Task<string?> DoGrpcDelete(string methodName, string requestJson)
        {
            var request = new GrpcRequest { JsonPayload = requestJson };
            var response = await client.DeleteAsync(new GrpcRequest { MethodName = methodName, JsonPayload = requestJson });
            return response.JsonPayload;
        }
    }
}
