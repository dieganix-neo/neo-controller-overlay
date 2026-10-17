# Gamepad Viewer controller skins

Original creator/source: Gamepad Viewer (mrmcpowned / Chris) and contributing skin artists.

Website: https://gamepadviewer.com/

Source assets retrieved 2026-10-17:

- `ps4/*.png`: SVGs under https://gamepadviewer.com/ps4-white-assets/ named `base.svg`, `triggers.svg`, `bumper.svg`, `touchpad.svg`, `start.svg`, `face.svg`, `sticks.svg`, and `dpad.svg`.
- `ps4/meta.png`: https://gamepadviewer.com/ps4-assets/meta.svg
- `xbox/*.png`: SVGs under https://gamepadviewer.com/xbox-assets/ named `base-white.svg`, `trigger.svg`, `bumper.svg`, `start-select.svg`, `abxy.svg`, `stick.svg`, `dpad.svg`, and `quadrant.svg`.
- Sprite positions and dimensions: https://gamepadviewer.com/style.css

The SVGs are rasterized at twice their original width to PNG, with alpha preserved. The application reproduces the website's sprite placement and reads live controller data using SDL. The original SVGs are included alongside the PNGs for attribution and reproducibility.

These third-party artwork files remain the property of their original creators. They are excluded from the application's MIT license; this repository does not grant a separate license to the artwork or claim authorship of it. PlayStation and Xbox branding belongs to the respective owners.
