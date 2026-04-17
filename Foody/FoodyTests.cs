using Foody.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId;//id на храната, която сме създали в тест 1
        
        [OneTimeSetUp] //веднъж да конфигурираме преди пускането на всички тестове
        public void Setup()
        {
            string jwtToken = GetJwtToken("exam_prep_2", "123456");
            //попълваме опциите на този restclient
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            RestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate.Status code: {response.StatusCode}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateFood_WithRequiredFields_ShouldSucceed()
        {
            
            //създаваме храна, с която да работим
            FoodDTO food = new FoodDTO
            {
                Name = "Soup",
                Description = "Soup with chicken and potatos.",
                Url = ""
            };

            //пишем request
            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            
            //добаваме body
            request.AddJsonBody(food);

            //изпълняваме заявката
            RestResponse response = client.Execute(request);

            //Ассерт
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code is 201 Created.");

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //горе пишем private static string foodId;
            //readyResponse има характеристика Msg и FoodId

            
             foodId = readyResponse.FoodId;
            
        }

        [Order(2)]
        [Test]
        public void EditFoodTitle_ShouldChangeTitle()
        {
            RestRequest request = new RestRequest($"/api/Food/Edit/{foodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken soup"

                }
            });

            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            //readyResponse
            //Msg = "Successfully edited!
            //FoodId

            Assert.That(readyResponse.Msg, Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]

        public void GetAllFoods_ShouldReturnNonEmptyArray()
        {
            RestRequest request = new RestRequest($"/api/Food/All", Method.Get);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            //response.Content = [
            //{ id:..
            //  name:
            //  description:
            //}]
            //искаме този масив да го преобразуваме в списък от храни
            List<FoodDTO> readyResponse = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse, Is.Not.Empty);
            Assert.That(readyResponse.Count, Is.GreaterThanOrEqualTo(1));

        }

        [Order(4)]
        [Test]

        public void DeleteExistingFood_ShouldSucceed()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{foodId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code is 200 OK.");
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            //readyResponse
            //Msg =
            //FoodId = null
            
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!")); 
        }

        [Order(5)]
        [Test]

        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = "",
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddBody(food);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]

        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "12345";
            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Soup"
                }
            });
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 NotFound.");
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            //readyResponse
            //Msg = "No food revues..."
            //FoodId = null

            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));

        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "12345";
            RestRequest request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 NotFound.");
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }

        [OneTimeTearDown] //веднъж разчистваме след изпълнението на всички тестове

        public void TearDown()
        {
            //clean up resources if needed
            this.client.Dispose();
        }

    }
}