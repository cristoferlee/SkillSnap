using System.Net.Http.Json;
using SkillSnap.Contracts.Contact;

namespace SkillSnap.Client.Services;

public sealed class ContactService
{
    private readonly HttpClient httpClient;
    private readonly AuthService authService;
    private readonly AntiforgeryTokenStore tokenStore;

    public ContactService(
        HttpClient httpClient,
        AuthService authService,
        AntiforgeryTokenStore tokenStore)
    {
        this.httpClient = httpClient;
        this.authService = authService;
        this.tokenStore = tokenStore;
    }

    public async Task SubmitAsync(
        CreateContactMessageRequest contact)
    {
        if (string.IsNullOrWhiteSpace(tokenStore.Token))
        {
            await authService.InitializeAsync();

            if (string.IsNullOrWhiteSpace(tokenStore.Token))
            {
                throw new HttpRequestException(
                    "The contact session could not be initialized.");
            }
        }

        var response = await httpClient.PostAsJsonAsync(
            "api/contact",
            contact);

        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ContactMessageResponse>>
        GetMessagesAsync()
    {
        var messages = await httpClient
            .GetFromJsonAsync<List<ContactMessageResponse>>(
                "api/contact");

        return messages ?? [];
    }
}