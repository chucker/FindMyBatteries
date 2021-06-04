# iCloud auth flow

This code implements more or less the bare minimum of what's required to fetch from the Find My service.

## No session info yet

If session info exists in `NSUserDefaults` (the macOS preferences store), skip to the next section.

1. we pass information such as your username/password to `https://idmsa.apple.com/appleauth/auth/signin`. This will give us a session token, session ID and scnt, and tell us if two-factor authentication, two-step verification or neither is used.
2. we login to `https://setup.icloud.com/setup/ws/1/accountLogin`, using the previous session token and a generated-once client ID. This gives us a bunch of cookies.
3. if two-factor authentication is required, we show a dialog where you can input the code. We then mark our device as trusted by going to `https://idmsa.apple.com/appleauth/auth/2sv/trust`, and send the entered code to `https://idmsa.apple.com/appleauth/auth/verify/trusteddevice/securitycode`.
4. we store all this, JSON-serialized, in `NSUserDefaults`.

## Session info exists

1. we deserialize the info from `NSUserDefaults`, including `SessionToken`, `Scnt`, `ClientId`, and `LoginResultCookies`.
2. we login to `https://setup.icloud.com/setup/ws/1/accountLogin`, using the previous session token and a generated-once client ID. (This may set new cookies. However, we don't currently save those back to `NSUserDefaults`.)
