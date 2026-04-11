using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace VinhKhanh.API.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
	private readonly HttpClient _client;

	public ApiIntegrationTests(ApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	private async Task<string> LoginAndGetTokenAsync(string username = "admin", string password = "Admin@2026")
	{
		var res = await _client.PostAsJsonAsync("/api/auth/login", new { username, password });
		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		var json = await ReadJsonAsync(res);
		var token = json.GetProperty("token").GetString();
		Assert.False(string.IsNullOrWhiteSpace(token));
		return token!;
	}

	[Fact]
	public async Task Auth_Login_WithSeededUsers_ReturnsJwtTokens()
	{
		var adminRes = await _client.PostAsJsonAsync("/api/auth/login", new
		{
			username = "admin",
			password = "Admin@2026"
		});
		Assert.Equal(HttpStatusCode.OK, adminRes.StatusCode);

		var adminJson = await ReadJsonAsync(adminRes);
		Assert.Equal("Admin", adminJson.GetProperty("role").GetString());
		Assert.False(string.IsNullOrWhiteSpace(adminJson.GetProperty("token").GetString()));

		var ownerRes = await _client.PostAsJsonAsync("/api/auth/login", new
		{
			username = "owner1",
			password = "Owner@2026"
		});
		Assert.Equal(HttpStatusCode.OK, ownerRes.StatusCode);

		var ownerJson = await ReadJsonAsync(ownerRes);
		Assert.Equal("Owner", ownerJson.GetProperty("role").GetString());
		Assert.False(string.IsNullOrWhiteSpace(ownerJson.GetProperty("token").GetString()));
	}

	[Fact]
	public async Task Poi_Qr_Nearby_And_Crud_Translation_Workflow_Works()
	{
		var token = await LoginAndGetTokenAsync();
		var listRes = await _client.GetAsync("/api/poi?lang=vi");
		Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
		var list = await ReadJsonAsync(listRes);
		Assert.True(list.ValueKind == JsonValueKind.Array && list.GetArrayLength() > 0);

		var first = list[0];
		var firstId = first.GetProperty("id").GetInt32();
		var firstQr = first.GetProperty("qrCode").GetString();
		var firstLat = first.GetProperty("latitude").GetDouble();
		var firstLon = first.GetProperty("longitude").GetDouble();

		var qrRes = await _client.GetAsync($"/api/poi/qrcode/{firstQr}");
		Assert.Equal(HttpStatusCode.OK, qrRes.StatusCode);
		var qrObj = await ReadJsonAsync(qrRes);
		Assert.Equal(firstId, qrObj.GetProperty("id").GetInt32());

		var nearbyRes = await _client.PostAsJsonAsync("/api/poi/nearby", new
		{
			lat = firstLat,
			lon = firstLon,
			lang = "vi"
		});
		Assert.Equal(HttpStatusCode.OK, nearbyRes.StatusCode);
		var nearby = await ReadJsonAsync(nearbyRes);
		Assert.True(nearby.GetArrayLength() > 0);

		var qr = $"INT-QR-{Guid.NewGuid():N}";
		var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/poi")
		{
			Content = JsonContent.Create(new
			{
				name = "Integration POI",
				description = "Integration description",
				ownerInfo = (string?)null,
				latitude = firstLat + 0.00015,
				longitude = firstLon + 0.00015,
				mapX = 50,
				mapY = 50,
				triggerRadiusMeters = 20,
				priority = 8,
				cooldownSeconds = 40,
				imageUrl = (string?)null,
				audioViUrl = (string?)null,
				qrCode = qr,
				category = 0,
				isActive = true
			})
		};
		createReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var createRes = await _client.SendAsync(createReq);
		Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
		var created = await ReadJsonAsync(createRes);
		var newId = created.GetProperty("id").GetInt32();

		var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/poi/{newId}")
		{
			Content = JsonContent.Create(new
			{
				id = newId,
				name = "Integration POI Updated",
				description = "Integration description updated",
				ownerInfo = "owner",
				latitude = firstLat + 0.00015,
				longitude = firstLon + 0.00015,
				mapX = 55,
				mapY = 55,
				triggerRadiusMeters = 25,
				priority = 9,
				cooldownSeconds = 45,
				imageUrl = (string?)null,
				audioViUrl = (string?)null,
				qrCode = qr,
				category = 0,
				isActive = true
			})
		};
		updateReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var updateRes = await _client.SendAsync(updateReq);
		Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

		var trReq = new HttpRequestMessage(HttpMethod.Post, $"/api/poi/{newId}/translation")
		{
			Content = JsonContent.Create(new
			{
				languageCode = "en",
				name = "Integration EN",
				description = "Integration EN description",
				audioUrl = (string?)null
			})
		};
		trReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var trRes = await _client.SendAsync(trReq);
		Assert.Equal(HttpStatusCode.OK, trRes.StatusCode);

		var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/poi/{newId}");
		delReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var delRes = await _client.SendAsync(delReq);
		Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

		var afterDeleteRes = await _client.GetAsync($"/api/poi/{newId}");
		Assert.Equal(HttpStatusCode.OK, afterDeleteRes.StatusCode);
		var afterDelete = await ReadJsonAsync(afterDeleteRes);
		Assert.False(afterDelete.GetProperty("isActive").GetBoolean());
	}

	[Fact]
	public async Task Tour_Create_Update_Delete_And_Invalid_StopOrder_Are_Handled()
	{
		var token = await LoginAndGetTokenAsync();

		var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/tour")
		{
			Content = JsonContent.Create(new
			{
				name = "Integration Tour",
				description = "Integration tour desc",
				estimatedMinutes = 60,
				stops = new[]
				{
					new { poiId = 1, stopOrder = 1, stayMinutes = 15, note = "A" },
					new { poiId = 2, stopOrder = 2, stayMinutes = 20, note = "B" }
				}
			})
		};
		createReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var createRes = await _client.SendAsync(createReq);
		Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
		var created = await ReadJsonAsync(createRes);
		var tourId = created.GetProperty("id").GetInt32();

		var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/tour/{tourId}")
		{
			Content = JsonContent.Create(new
			{
				name = "Integration Tour Updated",
				description = "Integration tour updated",
				estimatedMinutes = 75,
				stops = new[]
				{
					new { poiId = 1, stopOrder = 1, stayMinutes = 10, note = "A" },
					new { poiId = 3, stopOrder = 2, stayMinutes = 15, note = "C" }
				}
			})
		};
		updateReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var updateRes = await _client.SendAsync(updateReq);
		Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

		var invalidReq = new HttpRequestMessage(HttpMethod.Post, "/api/tour")
		{
			Content = JsonContent.Create(new
			{
				name = "Bad Tour",
				description = "bad",
				estimatedMinutes = 30,
				stops = new[]
				{
					new { poiId = 1, stopOrder = 1, stayMinutes = 10, note = "A" },
					new { poiId = 2, stopOrder = 1, stayMinutes = 10, note = "B" }
				}
			})
		};
		invalidReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var invalidRes = await _client.SendAsync(invalidReq);
		Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);

		var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/tour/{tourId}");
		delReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		var delRes = await _client.SendAsync(delReq);
		Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
	}

	[Fact]
	public async Task Movement_Analytics_And_History_Endpoints_Work_With_Validation()
	{
		var invalidMovementRes = await _client.PostAsJsonAsync("/api/movement/batch", new
		{
			sessionId = "int-session",
			points = new[]
			{
				new { lat = 999, lon = 999, accuracy = 2.0f, timestamp = DateTime.UtcNow }
			}
		});
		Assert.Equal(HttpStatusCode.BadRequest, invalidMovementRes.StatusCode);

		var movementRes = await _client.PostAsJsonAsync("/api/movement/batch", new
		{
			sessionId = "int-session",
			points = new[]
			{
				new { lat = 10.7531, lon = 106.6780, accuracy = 2.0f, timestamp = DateTime.UtcNow }
			}
		});
		Assert.Equal(HttpStatusCode.OK, movementRes.StatusCode);
		var movementJson = await ReadJsonAsync(movementRes);
		Assert.True(movementJson.GetProperty("saved").GetInt32() >= 1);

		var analyticsRes = await _client.PostAsJsonAsync("/api/analytics/log", new
		{
			poiId = 1,
			sessionId = "int-session",
			languageCode = "vi",
			triggerType = "GPS",
			duration = 12
		});
		Assert.Equal(HttpStatusCode.OK, analyticsRes.StatusCode);

		var topRes = await _client.GetAsync("/api/analytics/top?days=30");
		Assert.Equal(HttpStatusCode.OK, topRes.StatusCode);
		var top = await ReadJsonAsync(topRes);
		Assert.True(top.ValueKind == JsonValueKind.Array);
		Assert.True(top.GetArrayLength() >= 1);

		var historyLogRes = await _client.PostAsJsonAsync("/api/history/log", new
		{
			sessionId = "int-session",
			eventType = "INT_TEST",
			poiId = 1,
			languageCode = "vi",
			payload = "ok"
		});
		Assert.Equal(HttpStatusCode.OK, historyLogRes.StatusCode);

		var historyRes = await _client.GetAsync("/api/history?page=1&size=50&eventType=INT_TEST");
		Assert.Equal(HttpStatusCode.OK, historyRes.StatusCode);
		var history = await ReadJsonAsync(historyRes);
		Assert.True(history.GetProperty("total").GetInt32() >= 1);
	}

	[Fact]
	public async Task Ai_Endpoints_Return_Controlled_Responses_When_External_Providers_Are_Unavailable()
	{
		var chatRes = await _client.PostAsJsonAsync("/api/ai/chat", new
		{
			message = "Xin chao",
			language = "vi",
			history = Array.Empty<object>()
		});
		Assert.Equal(HttpStatusCode.OK, chatRes.StatusCode);

		var chatJson = await ReadJsonAsync(chatRes);
		var reply = chatJson.GetProperty("reply").GetString();
		Assert.False(string.IsNullOrWhiteSpace(reply));

		var ttsRes = await _client.PostAsJsonAsync("/api/ai/tts", new
		{
			text = "Xin chao",
			lang = "vi",
			voice = "vi-VN-HoaiMyNeural"
		});
		Assert.True(ttsRes.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable);
	}

	private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		using var doc = JsonDocument.Parse(text);
		return doc.RootElement.Clone();
	}
}
