#!/usr/bin/env bash
# Removes the service that deploy.sh installed: the unit, the program, the logs and the system user.
# Keeps the settings unless run with --purge. Safe to run again. Leaves this clone alone.
set -euo pipefail

readonly service=shutter-rf-bridge
readonly environment_file=/etc/default/$service
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
  sudo rm "$unit_file"
  sudo systemctl daemon-reload
fi
# A unit that failed stays loaded after its file is gone, until its failed state is cleared.
if systemctl is-failed --quiet "$service"; then
  sudo systemctl reset-failed "$service"
fi

sudo rm -rf "$install_dir" "$log_dir"

# Removed after the service has stopped, since userdel refuses a user that still runs processes.
if id "$service" &> /dev/null; then
  sudo userdel "$service"
fi
if getent group "$service" > /dev/null; then
  sudo groupdel "$service"
fi

if $purge; then
  sudo rm -f "$environment_file"
fi
if [[ -e $environment_file ]]; then
  echo "$service is uninstalled. Its settings are kept in $environment_file; --purge removes them."
else
  echo "$service is uninstalled."
fi
