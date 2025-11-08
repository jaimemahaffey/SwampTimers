# SwampTimers Deployment Guide

## Important: Private Repository Setup

**This repository is configured as PRIVATE.** For detailed instructions on deploying a private Home Assistant add-on, see [PRIVATE_REPO_SETUP.md](PRIVATE_REPO_SETUP.md).

Quick summary for private deployment:
1. Push code to GitHub (already private)
2. Make container images private after first build
3. Create GitHub Personal Access Token (PAT)
4. Configure Home Assistant with authentication
5. Add repository and install add-on

## Deploying to Home Assistant

### Prerequisites
- Home Assistant instance (Core, Supervised, or OS)
- GitHub account (for repository hosting)
- GitHub Personal Access Token with `read:packages` and `repo` permissions (for private repo)
- (Optional) Docker Desktop for local testing

### Option 1: Deploy via GitHub (Recommended)

#### Step 1: Prepare Your Repository

1. **Repository URLs are already configured**:
   - `config.yaml` image: `ghcr.io/jaimemahaffey/swamptimers-{arch}`
   - `repository.yaml` URL: `https://github.com/jaimemahaffey/swamptimers`
   - Maintainer: Jaime Mahaffey

3. **Add Icon and Logo** (optional but recommended):
   - Create `icon.png` (256x256 px)
   - Create `logo.png` (128x128 px)
   - Use timer/clock themed icons

#### Step 2: Push to GitHub

```bash
# Ensure you're on the feature branch
git branch

# Push the branch to GitHub
git push origin feature/home-assistant-integration

# Or push to main if you've merged
git checkout main
git merge feature/home-assistant-integration
git push origin main
```

#### Step 3: Set Up GitHub Actions for Building

Create `.github/workflows/build.yaml`:

```yaml
name: Build Add-on

on:
  push:
    branches:
      - main
  pull_request:
    branches:
      - main
  release:
    types:
      - published

jobs:
  build:
    name: Build add-on
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Login to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.repository_owner }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push
        uses: home-assistant/builder@master
        with:
          args: |
            --all \
            --target . \
            --docker-hub ghcr.io/${{ github.repository_owner }}
```

#### Step 4: Add to Home Assistant

1. In Home Assistant, navigate to **Settings** → **Add-ons** → **Add-on Store**
2. Click the **⋮** (three dots) menu in the top right
3. Select **Repositories**
4. Add the repository URL: `https://github.com/jaimemahaffey/swamptimers`
5. Click **Add**
6. Refresh the page
7. Find "SwampTimers" in the add-on list
8. Click on it and click **Install**

#### Step 5: Configure the Add-on

1. After installation, go to the **Configuration** tab
2. Set your preferred options:
   ```yaml
   storage_type: yaml  # or sqlite
   log_level: info     # debug, info, warning, error
   update_interval: 30 # seconds
   timezone: auto      # or specific timezone like America/New_York
   ```
3. Click **Save**

#### Step 6: Start the Add-on

1. Go to the **Info** tab
2. Click **Start**
3. Check the **Log** tab for any errors
4. Once started, click **Open Web UI** or access via the sidebar panel

### Option 2: Local Development/Testing

#### Build and Test Locally (Without Full HA)

```bash
# Build the Docker image
docker build -t swamptimers:dev .

# Run locally with mock configuration
docker run -d \
  --name swamptimers-test \
  -p 8080:8080 \
  -e SUPERVISOR_TOKEN=mock_token_for_testing \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v $(pwd)/test-data:/data \
  swamptimers:dev

# Check logs
docker logs swamptimers-test

# Access the UI
# Open browser to http://localhost:8080

# Stop and remove
docker stop swamptimers-test
docker rm swamptimers-test
```

#### Install Locally in Home Assistant (Advanced)

For local testing with a real Home Assistant installation:

1. **Copy files to HA add-on directory**:
   ```bash
   # On the HA host (or via SSH/Samba)
   mkdir -p /addons/swamptimers
   cp -r /path/to/SwampTimers/* /addons/swamptimers/
   ```

2. **Add local add-on**:
   - In HA, go to **Settings** → **Add-ons** → **Add-on Store**
   - Click **⋮** → **Check for updates**
   - SwampTimers should appear under "Local add-ons"

3. **Install and configure** as in Option 1, Steps 5-6

### Option 3: Manual Docker Build and Deploy

If you can't use GitHub Actions:

```bash
# Build for your architecture (example: amd64)
docker build \
  --build-arg BUILD_FROM=ghcr.io/home-assistant/amd64-base:latest \
  --build-arg BUILD_ARCH=amd64 \
  --build-arg BUILD_VERSION=1.0.0 \
  -t ghcr.io/jaimemahaffey/swamptimers-amd64:latest \
  .

# Push to GitHub Container Registry
docker push ghcr.io/jaimemahaffey/swamptimers-amd64:latest
```

For multi-arch builds, you'll need to build for each architecture separately or use `docker buildx`.

## Verifying Deployment

### Check Add-on Status

1. **Logs**: Settings → Add-ons → SwampTimers → Log
   - Should show: "Now listening on: http://+:8080"
   - No errors about missing dependencies

2. **Web UI**: Click "Open Web UI" or sidebar panel
   - Timer Schedules page should load
   - Can create/edit/delete timers
   - Settings page shows current storage type

3. **Data Persistence**:
   - Create a test timer
   - Restart the add-on
   - Verify timer is still there

### Troubleshooting

**Add-on won't start:**
- Check logs for .NET runtime errors
- Verify `run.sh` has execute permissions (should be automatic)
- Ensure base image is accessible

**UI not accessible:**
- Check Ingress is enabled in `config.yaml`
- Verify port 8080 in config matches Dockerfile
- Look for SignalR websocket connection errors in browser console

**Timers not persisting:**
- Check `/data` volume is mounted correctly
- Verify file permissions in container logs
- Try switching storage type (YAML ↔ SQLite)

**Configuration not loading:**
- Check `appsettings.json` is included in Docker image
- Verify environment variables in `run.sh` are set
- Review add-on configuration matches schema in `config.yaml`

## Updating the Add-on

### For Users

1. Settings → Add-ons → SwampTimers
2. If update available, click **Update**
3. Restart if needed

### For Developers

1. Update version in `config.yaml`
2. Commit changes
3. Push to GitHub
4. GitHub Actions will build new version
5. Users will see update notification

```bash
# Update version
# Edit config.yaml: version: "1.0.1"

git add config.yaml
git commit -m "Bump version to 1.0.1"
git push origin main

# Or create a release tag
git tag v1.0.1
git push origin v1.0.1
```

## Next Steps After Deployment

Once the add-on is running:

1. **Create timers** for your schedule needs
2. **Test timer activation/deactivation** times
3. **Verify timezone** handling is correct
4. **Review logs** during timer transitions
5. **Backup** your timer configuration

For Phase 2 (HA Entity Control), you'll be able to:
- Link timers to Home Assistant entities
- Control switches, lights, etc. automatically
- Use custom service calls
- Create automations based on timer events

## Support

- **Documentation**: See [DOCS.md](DOCS.md)
- **Issues**: https://github.com/jaimemahaffey/swamptimers/issues
- **Discussions**: https://github.com/jaimemahaffey/swamptimers/discussions
