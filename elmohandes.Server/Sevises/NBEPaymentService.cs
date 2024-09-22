using System.Text;

namespace elmohandes.Server.Sevises
{
    public class NBEPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public NBEPaymentService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> InitiatePayment(decimal amount, string customerMobile, string customerEmail)
        {
            var paymentRequest = new
            {
                MerchantCode = _config["NBE:MerchantCode"],
                OrderNumber = Guid.NewGuid().ToString(),
                CustomerMobile = customerMobile,
                CustomerEmail = customerEmail,
                Amount = amount,
                Currency = "EGP"
            };

            var content = new StringContent(JsonConvert.SerializeObject(paymentRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_config["NBE:ApiUrl"] + "/payments", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }

            throw new Exception("Failed to initiate payment");
        }
    }
}
