# macOS Code Signing Setup Guide

This document outlines the steps required to set up macOS code signing and notarization for Siren.

## Prerequisites

1. **Apple Developer Account** - You must be enrolled in the [Apple Developer Program](https://developer.apple.com/programs/) ($99/year)

## Step 1: Create Developer ID Certificate

1. Log in to [Apple Developer Portal](https://developer.apple.com/account/)
2. Navigate to **Certificates, Identifiers & Profiles**
3. Click **+** to create a new certificate
4. Select **Developer ID Application** (for apps distributed outside the Mac App Store)
5. Follow the instructions to create a Certificate Signing Request (CSR) using Keychain Access
6. Upload the CSR and download the certificate
7. Double-click the certificate to install it in Keychain Access

## Step 2: Export Certificate as .p12

1. Open **Keychain Access** on your Mac
2. Find your **Developer ID Application** certificate
3. Right-click and select **Export "Developer ID Application: Your Name"**
4. Choose **Personal Information Exchange (.p12)** format
5. Set a password for the .p12 file (you'll need this for GitHub secrets)
6. Save the file

## Step 3: Convert Certificate to Base64

On your Mac, run:
```bash
base64 -i YourCertificate.p12 -o certificate-base64.txt
```

Copy the contents of `certificate-base64.txt` - this will be your `MACOS_CERTIFICATE` secret.

## Step 4: Get Your Team ID

1. In Apple Developer Portal, go to **Membership**
2. Your **Team ID** is displayed there (format: `XXXXXXXXXX`)
3. Save this for the `APPLE_TEAM_ID` secret

## Step 5: Create App-Specific Password

1. Go to [appleid.apple.com](https://appleid.apple.com/)
2. Sign in with your Apple ID
3. Go to **Sign-In and Security** > **App-Specific Passwords**
4. Click **Generate an app-specific password**
5. Give it a label (e.g., "GitHub Actions Notarization")
6. Copy the generated password - this will be your `APPLE_ID_PASSWORD` secret

## Step 6: Get Your Signing Identity Names

You need **two** certificates for macOS signing:
1. **Developer ID Application** - for signing the app bundle
2. **Developer ID Installer** - for signing the installer package

You can find them by running:
```bash
security find-identity -v -p codesigning
```

Look for lines like:
```
Developer ID Application: Your Name (TEAM_ID)
Developer ID Installer: Your Name (TEAM_ID)
```

The full strings (including "Developer ID Application:" and "Developer ID Installer:") are your signing identities.

Alternatively, you can find them in Keychain Access - they're the exact names shown for your certificates.

**Note:** If you only have the Application certificate, you'll need to create the Installer certificate in the Apple Developer Portal as well.

## Step 7: Add GitHub Secrets

In your GitHub repository, go to **Settings** > **Secrets and variables** > **Actions** and add:

- `BUILD_CERTIFICATE_BASE64` - The base64-encoded Developer ID Application .p12 certificate
- `INSTALLER_CERTIFICATE_BASE64` - The base64-encoded Developer ID Installer .p12 certificate
- `P12_PASSWORD` - The password you set when exporting the .p12 files
- `MACOS_SIGN_APP_IDENTITY` - The full Application certificate name (e.g., "Developer ID Application: Your Name (TEAM_ID)")
- `MACOS_SIGN_INSTALL_IDENTITY` - The full Installer certificate name (e.g., "Developer ID Installer: Your Name (TEAM_ID)")
- `APPLE_USERNAME_ID` - Your Apple ID email address
- `APPLE_SIREN_ID_PASSWORD` - The app-specific password (from Step 5)
- `APPLE_TEAM_ID` - Your Team ID (from Step 4)
- `KEYCHAIN_PASSWORD` - A password for the temporary keychain (can be any secure string)

## Verification

After setting up the secrets, the GitHub Actions workflow will:
1. Import certificates into a temporary keychain
2. Set up a NotaryTool profile with your Apple credentials
3. Build an unsigned app bundle with Velopack
4. Restructure the app bundle to move static assets out of MacOS directory
5. Sign all Mach-O binaries with hardened runtime and entitlements
6. Create a signed installer package
7. Notarize and staple the installer

The signed and notarized app will be available as an artifact from the workflow run.

## Troubleshooting

- **"No identity found"**: Verify signing identity secrets match exactly what's shown in `security find-identity -v -p codesigning`
- **Notarization fails**: Check the notarization log for specific errors; common issues include unsigned binaries or missing entitlements
- **Certificate import fails**: Verify the base64 encoding is correct and the password matches
- **App crashes after signing**: Ensure `Siren/Siren.entitlements` includes necessary JIT permissions for .NET

