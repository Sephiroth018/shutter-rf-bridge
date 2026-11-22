#!/usr/bin/env bash
# Installs or updates the service from this clone: pulls its branch, publishes and (re)starts.
# Run it as your normal user; it asks for sudo where it needs root.
set -euo pipefail

readonly service=shutter-rf-bridge
readonly environment_file=/etc/default/$service
readonly install_dir=/opt/$service
repository=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
readonly repository

fail() {
  echo "Error: $*" >&2
  exit 1
}

[[ $EUID -ne 0 ]] || fail "run this script as your normal user, without sudo."

# The pull may change this script, so the pulled version runs the rest.
if [[ ${1-} != --pulled ]]; then
  git -C "$repository" pull --ff-only
  exec "$repository/deploy/deploy.sh" --pulled
fi

command -v dotnet > /dev/null || fail "the .NET SDK is missing; install it as the README describes."
[[ -e /dev/spidev0.0 ]] || fail "SPI is off; turn it on with 'sudo raspi-config nonint do_spi 0' and reboot."
getent group spi > /dev/null || fail "the group 'spi' is missing; it comes with Raspberry Pi OS."

if ! id "$service" &> /dev/null; then
  sudo useradd --system --user-group --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin "$service"
fi

if [[ ! -e $environment_file ]]; then
  sudo install --mode=600 "$repository/deploy/$service.env" "$environment_file"
  echo "Created $environment_file. Fill in the house values, then run this script again."
  exit 0
fi

commit=$(git -C "$repository" rev-parse HEAD)
readonly commit
readonly deployed_commit_file=$install_dir/.deployed-commit
# Skips the build and the restart, which would drop pending timed Stops, when nothing changed.
if [[ -e $deployed_commit_file && $(< "$deployed_commit_file") == "$commit" ]] && systemctl is-active --quiet "$service"; then
  echo "$service is already up to date."
  exit 0
fi

publish_dir=$(mktemp --directory)
trap 'rm -rf "$publish_dir"' EXIT
dotnet publish "$repository/ShutterRfBridge" --configuration Release --runtime linux-arm64 --self-contained false --output "$publish_dir"

sudo install --mode=644 "$repository/deploy/$service.service" /etc/systemd/system/
sudo systemctl daemon-reload
# Stopped before its files are replaced, since a running service may still load some of them.
sudo systemctl stop "$service"
# Clears the start limit, which otherwise refuses starts after repeated failures.
if systemctl is-failed --quiet "$service"; then
  sudo systemctl reset-failed "$service"
fi
sudo rm -rf "$install_dir"
sudo install --directory --mode=755 "$install_dir"
sudo cp -r "$publish_dir/." "$install_dir/"
echo "$commit" | sudo tee "$deployed_commit_file" > /dev/null
sudo systemctl enable --now "$service"
echo "$service is running."
