
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Dynamics.Webhooks.AFCopyContact.Model;
using System.Text;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace Dynamics.Webhooks.AFCopyContact
{
    public static class CopyContactBlob
    {
        public const string storageAccountName = "sadynamicsdevs";
        public const string containerName = "dynamicsdevs";

        [FunctionName("CopyContactBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<ResponseDynamics>(requestBody);
                var ContactId = data.OutputParameters.Where(x => x.key == "id").Select(x => x.value).FirstOrDefault();
                
                log.LogInformation($"Recuperou dados ID: {ContactId}");
                Uri StorageAccountUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
                BlobServiceClient BlobSClient = new BlobServiceClient(StorageAccountUri, new DefaultAzureCredential());
                
                log.LogInformation($"Conectou no BLOB");
                var containerClient = BlobSClient.GetBlobContainerClient(containerName);
                
                log.LogInformation($"Recuperou o container {containerClient.Uri.AbsoluteUri}");
                var blobClient = containerClient.GetBlobClient($"{ContactId}.json");

                byte[] array = Encoding.UTF8.GetBytes(requestBody);
                using (var content = new MemoryStream(array))
                {
                    await blobClient.UploadAsync(content, true);
                }
                log.LogInformation($"Contato {ContactId} salvo.");
            }
            catch (Exception e)
            {
                log.LogError($"Erro ao gerar arquivo, detalhes: {e.Message}");
                return new BadRequestResult();

            }
            return new OkResult();
        }
    }
}
