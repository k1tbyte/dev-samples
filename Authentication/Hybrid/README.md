# Hybrid access/refresh tokens authentication microservice

This project demonstrates an identity server with a hybrid session + JWT authentication approach.

## Technologies Used

- .NET 9 / C# 13
- ASP.NET Core (Web API)
- Entity Framework Core (PostgreSQL)
- Redis (for session management and caching)
- JWT (JSON Web Tokens) for access/refresh tokens
- xUnit for automated testing
- Swagger/OpenAPI for API documentation
- Mapster for object mapping
- BCrypt.Net for password hashing

## Features

- Hybrid authentication: access and refresh tokens
- Session validation via Redis cache
- API endpoints for sign-up, sign-in, token refresh, session management, and user info
- Automated tests for authentication flows
- Environment variable mapping via .env files

## Setup Instructions

1. **Clone the repository**
2. **Configure environment variables**
   - Create a `.env` file in the root directory
   ``` sh
   cp .env.example .env
   ```
   Update .env with actual values
3. **Create a PostgreSQL user and schema**
   - Run the following SQL commands:
     ```sql
     CREATE USER "auth-microservice" WITH PASSWORD 'yourpassword';
     ALTER SCHEMA public OWNER TO "auth-microservice";
     ```
4. **Run Initial Migration**
   - Use the following command to apply the initial migration:
     ```sh
     dotnet ef database update --project AccessRefresh
     ```
   - This will create the required tables (`users`, `sessions`) in your database.
5. **Start the API**
   - Run the project:
     ```sh
     dotnet run --project AccessRefresh
     ```
6. **Explore the API**
   - Swagger UI is available at `/swagger` in development mode.
   - Use endpoints for authentication, session management, and user info.

## Testing

- Automated tests are provided in the `AccessRefresh.Tests` project.
- Create a test database and update the connection string in the `.env` file.
- Run tests with:
  ```sh
  dotnet test
  ```

## Why Hybrid Authentication?
Hybrid authentication combines the benefits of both session-based and token-based authentication. It allows for.

We have good security because we update tokens every N minutes. The problem with JWT is that we cannot simply revoke this token because we do not store it in the database, which is obvious. It also causes data to become outdated; if the username or something else changes, we cannot update the token in any way.
There is a partial solution to this problem: 
- store revoked tokens (blacklist) in Redis and check the token on each authenticated request. We will set the lifetime so that Redis automatically deletes the record after the access token expires.

But the problem of outdated user data still remains. Since we are already looking at the cache, why don't we just pull more information about the user/session?
Therefore, we partially utilize the advantages of JWT and partially utilize session authentication. In the payload, we will only store information that does not change: user_id, session_id (maybe email, etc.).
This solves both problems listed above but adds a response delay, as each request must pass through the identity server. This is the price to pay for maximum security and data accuracy.

Why not just use session authentication?
 - Session authentication is stored in cookies, which can be a problem in microservices.
 - If the session ID has somehow been leaked (an attacker has obtained the session ID), we will not be able to refresh it.
 - Session authentication is not suitable for mobile applications, where JWT is more common.

### Additional security
- The access token is short-lived (e.g., 15 minutes) and can be refreshed using the refresh token
- We attach a system fingerprint to requests
- Even if both tokens were stolen, we compare the fingerprints every time
- We save the session state so that we can also notify the user of login attempts using existing tokens
- We store the refresh token in the database. With each refresh request, we must also attach the expired access token. The access token guarantees that the refresh token really matches the pair. Therefore, when we get a request, we check the session_id claim and see if the refresh token for that session matches. This stops us from getting just a refresh token (from anywhere, even the database) and using it to get a new access token

## Edge Cases

### Default flow
1. User signs up or signs in
2. Identity server creates a session and returns access and refresh tokens,
   - Access token is short-lived (e.g., 15 minutes)
   - Refresh token is long-lived (e.g., 30 days)
   - We save the client fingerprint for this session
3. After a while, the access token expires
4. User makes a request with the expired access token and the refresh token
5. Identity server do some checks:
   - Validate access token like always but without expiration (e.g., check signature)
   - Check if the session is still valid (e.g., not expired, not revoked)
   - Compare the fingerprint with the one stored in the session
6. Check for refresh token reusing window for concurrent requests: 
   - If the refresh token is used within a short time window (e.g., 5 seconds), it is considered a valid request. This happens when multiple requests are made to refresh the token at the same time (e.g., multiple tabs open) but we need to ensure security in this tiny moment. In order to eliminate the possibility that an attacker could obtain duplicate tokens during those 5 seconds, we store not only the refresh token in the cache, but also the IP address and fingerprint.
   ``` c#
   $"reused_refresh:{refreshToken}:{fingerprint}:{ipAddress}"
    ```
   - If the refresh token in reuse window is valid, we return the cached pair of access and refresh tokens
7. If all checks pass, issue a new access token + refresh token
8. For short time (5-10 sec.), we store refresh token in Redis cache to reuse for concurrent requests
10. Return new access and refresh tokens to the user
11. User can continue using the application with the new tokens and the same session

### Concurrent requests
1. Steps 1 - 3 from the default flow
2. Client opens multiple tabs and each tab makes a request for refreshing
3. By default, the first request will pass all checks and return new tokens, but the others will fail because the refresh token is not found (alredy deleted after first request). As result, the user will be logged out in all tabs except the first one (or all tabs)
4. To avoid this, we implement a reuse window for refresh tokens:
   - If the refresh token is used within a short time window (e.g., 5 seconds), it is considered a valid request
   - We store the refresh token in Redis cache with a short expiration time (e.g., 10 seconds) to allow concurrent requests to succeed
   - After the reuse window expires, any further requests with the same refresh token will fail
5. If attacker tries to use the same refresh token from another IP address or fingerprint, it will fail because the session is not valid for that combination

### Revoke session/(s)
1. Client revoke specific session or all sessions
2. Identity server removes the session from the database and Redis cache
3. Any further requests immediately fail with 401 Unauthorized

### Tokens are stolen
1. Attackers stole access token
2. Check fingerprint -> it does not match -> return 401 Unauthorized
3. Attackes stole fingerprint, access token and refresh token
4. Attacker refresh token with stolen access token, refresh token and fingerprint
5. Log out of the original user session, as the new tokens are different
6. The user notices that something is wrong, goes to the session list, and sees all the information about the session, including the IP address of attacker
7. User can revoke the session or all sessions and change the password/etc.

### Refresh token is stolen from the database or somewhere else
1. Attacker tries to use the stolen refresh token to get a new valid access token
2. Access token is required -> bad request
3. Attacker tries to use access token from his session
4. Identity server compares the session_id claim and the refresh token with the one stored in the database
5. Unauthorized

## Conclusion
The hybrid approach combines the advantages of session-based and token-based authentication. It provides security, data accuracy, and flexibility for various client applications, including web and mobile. We always receive up-to-date data and can revoke a token at any time. We can track the status of sessions, which provides additional security and transparency. Using Redis for session management and caching improves performance and scalability. However, this method is slower than simply decoding the token without any checks on the identity server side, even when caching is taken into account, as it significantly increases the load on the identity server. But for enhanced security and data accuracy, this minor drawback is acceptable

This approach can be used in complex microservice architectures with an emphasis on security. For monolithic applications, I would consider session-based authentication or standard JWT with access/refresh. In most cases, I would use regular JWT verification without calls to the identity server.
