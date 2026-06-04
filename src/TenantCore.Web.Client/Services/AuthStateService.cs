using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Web.Client.Services;

/// <summary>
/// Service for managing authentication state across the WebAssembly application.
/// </summary>
public class AuthStateService
{
    private readonly TokenStorageService _tokenStorage;
    private UserProfileDto? _currentUser;
    private string? _accessToken;
    private DateTime? _tokenExpiry;
    private List<ApplicationDto> _availableApplications = [];
    private bool _isInitialized;

    public AuthStateService(TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_accessToken) && !IsTokenExpired;
    public bool IsInitialized => _isInitialized;
    public bool IsTokenExpired => _tokenExpiry.HasValue && _tokenExpiry.Value <= DateTime.UtcNow;
    public UserProfileDto? CurrentUser => _currentUser;
    public string? AccessToken => _accessToken;
    public IReadOnlyList<ApplicationDto> AvailableApplications => _availableApplications;

    // Temporary login state for 2FA flow
    public string? TempLoginToken { get; private set; }
    public string? QrCodeBase64 { get; private set; }
    public string? UserDisplayName { get; private set; }
    public bool IsFirstTimeSetup { get; private set; }
    public bool RequiresTwoFactor { get; private set; }
    public bool RequiresPasswordReset { get; private set; }
    public Guid? PendingPasswordResetUserId { get; private set; }

    /// <summary>
    /// Initializes auth state from persistent storage on app start.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            if (await _tokenStorage.IsTokenValidAsync())
            {
                _accessToken        = await _tokenStorage.GetAccessTokenAsync();
                _tokenExpiry        = await _tokenStorage.GetTokenExpiryAsync();
                _currentUser        = await _tokenStorage.GetUserAsync();
                _availableApplications = await _tokenStorage.GetAvailableApplicationsAsync();
            }
            else
            {
                await _tokenStorage.ClearTokensAsync();
            }
        }
        catch
        {
            // Ignore errors during initialization
        }
        finally
        {
            _isInitialized = true;
            OnAuthStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Sets the temporary login state after credentials validation.
    /// </summary>
    public void SetTempLoginState(LoginResponseDto response)
    {
        TempLoginToken = response.TempLoginToken;
        QrCodeBase64 = response.QrCodeBase64;
        UserDisplayName = response.UserDisplayName;
        IsFirstTimeSetup = response.IsFirstTimeSetup;
        RequiresTwoFactor = response.RequiresTwoFactor;
        RequiresPasswordReset = response.RequiresPasswordReset;

        if (response.RequiresPasswordReset && response.User != null)
        {
            PendingPasswordResetUserId = response.User.UserId;
        }

        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Completes login after successful authentication.
    /// </summary>
    public async Task SetAuthenticatedStateAsync(LoginResponseDto response)
    {
        _currentUser = response.User;
        _accessToken = response.AccessToken;
        _tokenExpiry = response.ExpiresAt;
        _availableApplications = response.AvailableApplications;

        await _tokenStorage.StoreTokensAsync(response);
        ClearTempState();
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Clears all authentication state (logout).
    /// </summary>
    public async Task ClearAuthStateAsync()
    {
        _currentUser = null;
        _accessToken = null;
        _tokenExpiry = null;
        _availableApplications = [];
        ClearTempState();

        await _tokenStorage.ClearTokensAsync();
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Clears temporary login state.
    /// </summary>
    public void ClearTempState()
    {
        TempLoginToken = null;
        QrCodeBase64 = null;
        UserDisplayName = null;
        IsFirstTimeSetup = false;
        RequiresTwoFactor = false;
        RequiresPasswordReset = false;
        PendingPasswordResetUserId = null;
    }

    /// <summary>
    /// Updates the current user profile.
    /// </summary>
    public void UpdateUserProfile(UserProfileDto user)
    {
        _currentUser = user;
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Returns the active role names the current user holds in a specific clinic application.
    /// Always reads from the JWT-backed UserProfile — never from localStorage — so it cannot
    /// be spoofed by a client-side state change.
    /// </summary>
    public IReadOnlyList<string> GetRolesForApplication(Guid applicationId)
    {
        if (_currentUser == null || applicationId == Guid.Empty) return [];
        return _currentUser.UserRoles
            .Where(r => r.ApplicationId == applicationId && r.IsActive)
            .Select(r => r.RoleName)
            .ToList();
    }
}
