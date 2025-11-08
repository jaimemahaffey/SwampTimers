# Private Repository Setup Guide

This guide is for deploying SwampTimers as a **private** Home Assistant add-on that only you can access.

## Current Setup

✅ **Repository**: Private GitHub repository (already configured)
✅ **GitHub Actions**: Configured to build and push images
✅ **Container Registry**: Will use GitHub Container Registry (ghcr.io)

## Step-by-Step Setup

### 1. Push Your Code to GitHub

```bash
# Make sure you're on the feature branch
git push -u origin feature/home-assistant-integration

# Or push to main
git checkout main
git merge feature/home-assistant-integration
git push origin main
```

### 2. Make Container Images Private (Important!)

After the first GitHub Actions build completes:

1. Go to your GitHub repository page
2. Click on **Packages** (right side of the page)
3. Click on each `swamptimers-{arch}` package
4. Click **Package settings** (gear icon)
5. Scroll down to **Danger Zone**
6. Click **Change visibility** → **Private**
7. Repeat for all architecture packages

Alternatively, you can set package visibility to inherit from repository:
- This makes packages automatically private since your repo is private
- Go to Repository **Settings** → **Actions** → **General**
- Under "Workflow permissions", ensure packages inherit repo visibility

### 3. Create a GitHub Personal Access Token (PAT)

Since your repository and images are private, Home Assistant needs authentication to pull the images.

1. Go to GitHub **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token** → **Generate new token (classic)**
3. Set a note: "Home Assistant - SwampTimers Add-on"
4. Set expiration: **No expiration** (or your preference)
5. Select scopes:
   - ✅ `read:packages` (Download packages from GitHub Package Registry)
   - ✅ `repo` (if your repo is private)
6. Click **Generate token**
7. **Copy the token immediately** (you won't see it again!)

### 4. Configure Home Assistant to Use Private Registry

#### Option A: Using Add-on Configuration (Recommended)

Update your `config.yaml` to support registry authentication.

Add to `options` section:
```yaml
options:
  storage_type: yaml
  log_level: info
  update_interval: 30
  timezone: auto
  github_token: ""  # Leave empty, will be set in add-on config

schema:
  storage_type: list(yaml|sqlite)
  log_level: list(debug|info|warning|error)
  update_interval: int(10,300)
  timezone: str?
  github_token: password?  # Marked as password for security
```

**Then when installing the add-on:**
1. Before clicking Install, go to Configuration
2. Add your GitHub PAT to the `github_token` field
3. Click Save
4. Now click Install

#### Option B: Using Docker Authentication File (Advanced)

On your Home Assistant host:

```bash
# SSH into your Home Assistant
ssh root@homeassistant.local

# Login to GitHub Container Registry
docker login ghcr.io -u YOUR_GITHUB_USERNAME -p YOUR_GITHUB_PAT

# This creates /root/.docker/config.json with credentials
# Home Assistant will use this when pulling images
```

### 5. Add the Repository to Home Assistant

1. In Home Assistant, go to **Settings** → **Add-ons** → **Add-on Store**
2. Click **⋮** (three dots) → **Repositories**
3. Add: `https://github.com/jaimemahaffey/swamptimers`
4. Click **Add**

**Note**: Even though the repository is private, you can still add it to your HA. The initial repository fetch uses GitHub's public API, which allows reading repository metadata even for private repos you own.

### 6. Install the Add-on

1. Find **SwampTimers** in the add-on list
2. Click on it
3. If using Option A above, configure the GitHub token first
4. Click **Install**
5. Home Assistant will pull the private images using authentication
6. Configure your add-on settings
7. Click **Start**

## Security Considerations

### ✅ Advantages of Private Setup

- **Code Privacy**: Your source code is not publicly visible
- **Image Privacy**: Container images are not publicly accessible
- **Access Control**: Only you can install and use the add-on
- **No Discovery**: Add-on won't show up in any public listings

### ⚠️ Important Security Notes

1. **Protect Your PAT**:
   - Treat it like a password
   - Don't commit it to code
   - Use Home Assistant's password fields (marked as `password?` in schema)

2. **Token Permissions**:
   - Only grant minimum required permissions (`read:packages`, `repo`)
   - Consider setting expiration and renewing periodically

3. **Backup Your Token**:
   - Store it in a password manager
   - You'll need it again if you reinstall Home Assistant

## Troubleshooting Private Repository

### Error: "Failed to pull image"

**Cause**: Home Assistant can't authenticate to GitHub Container Registry

**Solution**:
1. Verify your GitHub PAT is correct
2. Check PAT has `read:packages` permission
3. Ensure container images are visible to your account
4. Try Option B (Docker login) if Option A doesn't work

### Error: "Repository not found"

**Cause**: GitHub API can't access your private repository

**Solution**:
1. Ensure you're logged into GitHub in your browser
2. Check repository URL is correct
3. Verify repository exists and you have access
4. Try accessing the repo directly: `https://github.com/jaimemahaffey/swamptimers`

### Add-on installs but won't start

**Cause**: Image pulled successfully but runtime error

**Solution**:
1. Check add-on logs: Settings → Add-ons → SwampTimers → Log
2. Look for .NET runtime errors
3. Verify architecture matches your HA host (amd64, aarch64, etc.)

## Updating the Add-on

When you push changes to GitHub:

1. GitHub Actions automatically builds new images
2. Version number in `config.yaml` should be updated
3. Home Assistant will show an update notification
4. Click **Update** to pull the new version

## Sharing with Specific People (Optional)

If you want to share with trusted users:

1. **Add them as collaborators** to your private repo
2. They follow the same setup process
3. They need their own GitHub PAT
4. They add your repository URL to their HA

## Alternative: Local Add-on (No GitHub)

If you prefer not to use GitHub at all:

1. Copy the add-on files directly to your HA machine
2. Place in `/addons/swamptimers/`
3. Add-on appears as "Local add-on" in HA
4. No authentication needed
5. See DEPLOYMENT.md → Option 2 for details

## Support

Since this is a private repository:
- Issues and discussions are private
- Only you and collaborators can access them
- Perfect for personal use and testing

Need help? Check the logs in Settings → Add-ons → SwampTimers → Log
