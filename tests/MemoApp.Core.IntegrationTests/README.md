# MemoApp Core Integration Tests

This project contains integration tests for the MemoApp Core module, specifically focusing on real-world testing of external service integrations like the OpenAI image generation service.

## Prerequisites

### OpenAI API Key Configuration

To run the OpenAI Image Generator integration tests, you need to configure a valid OpenAI API key. You can do this in several ways:

#### Option 1: User Secrets (Recommended for Development)

```bash
cd MemoApp.Core.IntegrationTests
dotnet user-secrets init
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key-here"
dotnet user-secrets set "OpenAI:OrganizationId" "your-org-id" # Optional
```

#### Option 2: Environment Variables

```bash
export OpenAI__ApiKey="your-openai-api-key-here"
export OpenAI__OrganizationId="your-org-id" # Optional
```

#### Option 3: appsettings.json (Not Recommended - Don't Commit API Keys)

Edit `appsettings.json` and add your API key:

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here",
    "OrganizationId": "your-org-id"
  }
}
```

## Running the Tests

### Run All Integration Tests

```bash
dotnet test
```

### Run Only OpenAI Image Generator Tests

```bash
dotnet test --filter "OpenAIImageGeneratorTests"
```

### Run Tests with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Tests in Parallel

```bash
dotnet test --parallel
```

## Test Categories

### OpenAI Image Generator Tests

These tests verify the integration with OpenAI's DALL-E API:

- **Service Availability**: Tests if the OpenAI API is accessible
- **Single Image Generation**: Tests generating images for individual Major System words
- **Batch Image Generation**: Tests generating multiple images concurrently
- **Custom Options**: Tests various generation options (size, style, context)
- **Error Handling**: Tests behavior with invalid inputs
- **Cancellation**: Tests proper handling of cancellation tokens
- **Concurrency**: Tests handling multiple simultaneous requests

## Test Data

The tests use predefined Major System words that map to numbers:

- `tea` → 1 (T = 1)
- `noah` → 2 (N = 2)
- `mom` → 3 (M = 3)
- `rye` → 4 (R = 4)
- `law` → 5 (L = 5)
- `shoe` → 6 (SH = 6)
- `cow` → 7 (K/G = 7)
- `ivy` → 8 (V/F = 8)
- `pie` → 9 (P/B = 9)

## Important Notes

### Cost Considerations

⚠️ **These tests make real API calls to OpenAI and will incur costs!** 

- Each image generation call costs money based on OpenAI's pricing
- The tests are designed to minimize costs by using smaller image sizes and limiting batch operations
- Consider running tests sparingly during development

### Test Isolation

- Tests are designed to be independent and can run in any order
- Each test uses unique test data to avoid conflicts
- Tests include proper cleanup and resource disposal

### Rate Limiting

- The tests respect OpenAI's rate limits by using controlled concurrency
- Batch operations are limited to prevent overwhelming the API
- Tests include appropriate delays and retry logic where necessary

## Troubleshooting

### Common Issues

1. **"OpenAI API key is not configured"**
   - Ensure you've set up the API key using one of the methods above
   - Verify the key is accessible in your environment

2. **Rate Limit Exceeded**
   - Wait a few minutes before retrying
   - Consider reducing the number of concurrent tests

3. **Network Timeouts**
   - Check your internet connection
   - The default timeout is 60 seconds, which should be sufficient

4. **Test Failures Due to API Changes**
   - OpenAI occasionally updates their API
   - Check the OpenAI documentation for any breaking changes

### Debugging

To enable detailed logging, set the log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "MemoApp.Core": "Information"
    }
  }
}
```

## Contributing

When adding new integration tests:

1. Follow the existing naming conventions
2. Include proper assertions using FluentAssertions
3. Add comprehensive logging for debugging
4. Consider the cost implications of API calls
5. Ensure tests are independent and repeatable
6. Update this README if adding new test categories