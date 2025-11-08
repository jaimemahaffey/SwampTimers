# SwampTimers Add-on Documentation

## Installation

1. Add this repository to your Home Assistant add-on store
2. Install the SwampTimers add-on
3. Start the add-on
4. Access the UI via the sidebar panel

## Configuration

### storage_type
Type of storage backend to use:
- `yaml` (default): Human-readable YAML file storage
- `sqlite`: SQLite database storage (better for many timers)

### log_level
Logging verbosity:
- `debug`: Very detailed logs for troubleshooting
- `info` (default): General information
- `warning`: Warnings only
- `error`: Errors only

### update_interval
How often (in seconds) to check timers for activation/deactivation.
- Range: 10-300 seconds
- Default: 30 seconds
- Lower values = more responsive but higher CPU usage

### timezone
Timezone for timer calculations:
- `auto` (default): Use Home Assistant's timezone
- Or specify: `America/New_York`, `Europe/London`, etc.

## Usage

### Creating a Timer

1. Open the SwampTimers panel in Home Assistant
2. Click "Add Duration Timer" or "Add Time Range Timer"
3. Choose timer type:
   - **Duration Timer**: Starts at a specific time, runs for X minutes
   - **Time Range Timer**: Turns on at time A, off at time B
4. Configure timer settings:
   - Name and description
   - Schedule (times and days)
   - Enable/disable toggle
5. Click Save

### Timer Types

**Duration Timer:**
- Start time (e.g., 6:00 AM)
- Duration in minutes (e.g., 120 minutes = 2 hours)
- Active days (e.g., Monday, Wednesday, Friday)
- Perfect for: Pool pumps, irrigation systems, heating

**Time Range Timer:**
- On time (e.g., 6:00 PM)
- Off time (e.g., 11:00 PM)
- Can span midnight (e.g., 11:00 PM to 2:00 AM)
- Active days (select which days of the week)
- Perfect for: Outdoor lighting, security systems

### Day-of-Week Filtering

Both timer types support day-of-week filtering:
- Select specific days when the timer should be active
- Leave all days selected for daily operation
- Example: Pool pump runs Mon-Fri only

### Timer Status

Each timer card shows:
- Current status (Active/Inactive)
- Next activation time
- Next deactivation time
- Enable/disable toggle

## Data Storage

Timer data is stored in `/addon_configs/xxxxx_swamptimers/`:
- **YAML mode**: `timers.yaml` - Human-readable, can be manually edited
- **SQLite mode**: `timers.db` - Binary database format

### Backup

- Include add-on configuration in your Home Assistant backups
- YAML format makes it easy to version control or manually backup
- Data persists across add-on restarts and updates

## Troubleshooting

### Timer not activating

1. Check timer is enabled (toggle switch)
2. Verify current day is in "Active Days"
3. Check time zone setting matches your location
4. Review add-on logs for errors

### UI not accessible

1. Ensure add-on is started
2. Check add-on logs for startup errors
3. Try restarting the add-on
4. Verify port 8080 is not conflicting

### Data not persisting

1. Check `/addon_configs` directory permissions
2. Verify storage type matches your preference
3. Review logs for file write errors

## Future Features (Home Assistant Integration)

**Phase 2** will add:
- Home Assistant entity control
- Turn on/off switches, lights, etc. based on timers
- Custom service call support
- Event firing for automation integration
- Entity picker UI

## Icons

The add-on uses default icons. To customize:
- Create `icon.png` (256x256 px) for the add-on store listing
- Create `logo.png` (128x128 px) for the add-on details page
- Use timer/clock themed graphics
- Place in the repository root

## Support

- GitHub: https://github.com/mahaffey/swamptimers
- Issues: https://github.com/mahaffey/swamptimers/issues
- Discussions: https://github.com/mahaffey/swamptimers/discussions
