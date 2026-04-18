using MovieCatalogExam.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace MovieCatalog
{
    public class MovieCatalogTests
    {
        private RestClient client;
        private static string movieId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("HristoK", "123456");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:5000")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);

        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:5000");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new 
            { 
                email = "hristokrazhev@gmail.com", 
                password = "123456"
            });
            
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]

        public void CreateNewMovie_WithRequiredFields_ShouldSuccess()
        {
            var movie = new MovieDto
            {
                Id = "",
                Title = "Action Movie",
                Description = "An action-packed movie."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseObject = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);
            Assert.That(responseObject, Is.Not.Null);

            Assert.That(responseObject.Msg, Is.EqualTo("Movie created successfully!"));

            Assert.That(responseObject.Movie, Is.Not.Null);

            Assert.That(responseObject.Movie.Id, Is.Not.Null.And.Not.Empty);

            movieId = responseObject.Movie.Id;

        }

        [Order(2)]
        [Test]

        public void EditExistingMovie_ShouldReturnSuccess()
        {
            var editRequestData = new MovieDto
            {
                Id = "",
                Title = "Edited movie title",
                Description = "New title after editing"
            };


            var request = new RestRequest("/api/Movie/Edit", Method.Put);

            request.AddQueryParameter("movieId", movieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }

        [Order(3)]
        [Test]

        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDto>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);
        }

        [Order(4)]
        [Test]

        public void DeleteMovie_ShouldReturnSuccess() 
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", movieId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(deleteResponse, Is.Not.Null);
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]

        public void CreateMovie_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var movieData = new MovieDto
            {
                Id = "",
                Title = "",
                Description = "This is a test movie description"
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]

        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "9999999";
            var editRequestData = new MovieDto
            {
                Id = "",
                Title = "Edited movie title",
                Description = "This is an edited movie description."
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editRequestData);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            Assert.That(response.Content, Does.Contain("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "9999999";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            var response = this.client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            
            Assert.That(response.Content, Does.Contain("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown] 
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}