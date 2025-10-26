# Changelog

## [Unreleased] - Load Test Performance Fixes

### Fixed
- **Request timeout**: Increased global request timeout from 3 seconds to 15 seconds to prevent premature timeouts during high load
- **Rate limiting**: Adjusted rate limits from 10 req/sec to 100 req/sec to better handle concurrent users
- **RabbitMQ connection pool**: Reduced connection pool size from 100 to 20 to prevent resource exhaustion
- **Channel concurrency**: Added semaphore locking to `EnsureChannelAsync()` in `ResilientRabbitMqService` to prevent race conditions

### Changed
- Request timeout increased to 15 seconds (allows RPC operations with 10-second timeout to complete)
- Rate limits increased to 100 requests/second and 1000 requests/minute
- Connection pool reduced to 20 connections for better resource management

## [Unreleased] - 2025-10-26

### Performance Improvements
- **Increased RabbitMQ Connection Pool**: From 10 to 100 connections for better concurrency
- **Added Response Compression**: GZIP compression for all HTTP responses (~70% bandwidth reduction)
- **Optimized Request Timeouts**: 
  - RPC timeout reduced from 60s to 10s
  - Global HTTP request timeout set to 3 seconds

### Security Enhancements
- **Rate Limiting**: Implemented IP-based rate limiting
  - 10 requests per second
  - 100 requests per minute
  - Configurable per-endpoint rules
- **Enhanced Error Handling**: Better handling of non-JSON responses from RPC calls
- **Connection Health Monitoring**: Automatic detection and cleanup of dead connections

### Infrastructure
- **Added Load Testing**: Comprehensive k6-based load testing script
- **Added Security Checks**: Automated security validation script
- **Improved Logging**: Enhanced error logging for RPC failures

### Testing
- All existing unit tests remain compatible (128 tests)
- New load testing scenarios added
- Security validation tests added

### Configuration
- Updated `appsettings.json` with rate limiting configuration
- Added `load-test.js` for performance testing
- Added `security-check.ps1` for security validation

## [Previous Versions]
See git history for earlier changes.

