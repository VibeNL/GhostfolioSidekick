# Authentication Implementation

This document describes the token-based authentication system implemented for the Portfolio Viewer Blazor WebAssembly application.

## Overview

The application now requires users to authenticate using a secret access token before accessing any pages. The authentication system is built around the `IApplicationSettings.GhostfolioAccessToken` configuration value.

## Key Components

### 1. Authentication Services

- **`IAuthenticationService`**: Core authentication interface
- **`AuthenticationService`**: Implementation that handles login/logout and token storage
- **`ITokenValidationService`**: Interface for token validation
- **`TokenValidationService`**: Service that validates tokens against the configured value
- **`CustomAuthenticationStateProvider`**: Blazor authentication state provider

### 2. Authentication Flow

1. **Unauthenticated Access**: Users attempting to access any protected page are redirected to `/login`
2. **Login Process**: Users enter their access token on the login page
3. **Token Validation**: The token is validated against the configured `GhostfolioAccessToken`
4. **Session Storage**: Valid tokens are stored in browser localStorage
5. **Automatic Restoration**: On app reload, stored tokens are automatically validated

### 3. Configuration

The access token is configured in two places:

#### Client-side (WASM)
- `wwwroot/appsettings.json`
- `wwwroot/appsettings.Development.json`

```json
{
  "GhostfolioAccessToken": "your-secret-token-here"
}
```

#### Server-side (API Service)
The API service uses the `IApplicationSettings.GhostfolioAccessToken` from environment variables:
- Environment variable: `GHOSTFOLIO_ACCESTOKEN`

### 4. API Validation Endpoint

The API service provides a token validation endpoint:

```
POST /api/auth/validate
Authorization: Bearer <token>
```

This endpoint compares the provided token with the configured access token and returns validation status.

### 5. Protected Pages

All main pages are now protected with the `[Authorize]` attribute:
- Home (`/`)
- Holdings (`/holdings`)
- Holding Detail (`/holding/{symbol}`)
- Portfolio Time Series (`/portfolio-timeseries`)
- Tables (`/tables`)
- Transactions (`/transactions`)

### 6. Login Page

The login page (`/login`) provides:
- Simple token input form
- Client-side validation
- Error handling
- Automatic redirect on successful authentication
- Responsive Bootstrap UI

## Security Considerations

1. **Token Storage**: Tokens are stored in browser localStorage
2. **Token Transmission**: Tokens are sent to the API for validation via Authorization header
3. **Token Comparison**: Uses ordinal string comparison for token validation
4. **No Token Hashing**: Currently tokens are compared in plain text

## Usage Instructions

1. **Set the Access Token**: Configure `GhostfolioAccessToken` in your environment or configuration files
2. **Start the Application**: Both WASM and API service need to be running
3. **Access the Application**: Navigate to the application URL
4. **Login**: Enter the configured access token on the login page
5. **Use the Application**: All pages are now accessible after authentication

## Development Notes

- The system gracefully handles API unavailability by falling back to client-side validation
- Authentication state is maintained across browser sessions
- The logout functionality clears stored tokens and redirects to login
- All navigation is protected by the Blazor authorization system

## Future Enhancements

Potential improvements could include:
- Token hashing for additional security
- Token expiration and refresh mechanisms
- Multiple user support with role-based access
- Integration with external authentication providers
- Audit logging of authentication events