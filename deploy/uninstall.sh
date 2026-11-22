#!/usr/bin/env bash
# Removes the service that deploy.sh installed: the unit, the program, the logs and the system user.
# Keeps the settings unless run with --purge. Safe to run again. Leaves this clone alone.
set -euo pipefail

readonly service=shutter-rf-bridge
readonly settings_dir=/etc/$service
readonly install_dir=/opt/$service
readonly unit_file=/etc/systemd/system/$service.service
readonly log_dir=/var/log/$service

fail() {
  echo "Error: $*" >&2
  exit 1
}

[[ $EUID -ne 0 ]] || fail "run this script as your normal user, without sudo."

purge=false
case ${1-} in
  '') ;;
  --purge) purge=true ;;
  *) fail "unknown argument '$1'; the only option is --purge." ;;
esac
readonly purge

if [[ -e $unit_file ]]; then
  sudo systemctl disable --now "$service"
  # Includes the drop-in overrides of 'systemctl edit', which would otherwise apply to a reinstall.
  sudo rm -rf "$unit_file" "$unit_file.d"
  sudo systemctl daemon-reload
fi
# A unit that failed stays loaded after its file is gone, until its failed state is cleared.
if systemctl is-failed --quiet "$service"; then
  sudo systemctl reset-failed "$service"
fi

sudo rm -rf "$install_dir" "$log_dir"

# Removed after the service has stopped, since userdel refuses a user that still runs processes.
# It also removes the user's own group, which deploy.sh created with it.
if id "$service" &> /dev/null; then
  sudo userdel "$service"
fi

if $purge; then
  sudo rm -rf "$settings_dir"
fi
if [[ -e $settings_dir ]]; then
  echo "$service is uninstalled. Its settings are kept in $settings_dir; --purge removes them."
else
  echo "$service is uninstalled."
fi
