# Manual Back fix

1. Back up Jellyfin Web's `index.html`. On a typical Linux install it is `/usr/share/jellyfin/web/index.html`.
2. Download [vidaa-back-manual.js](https://raw.githubusercontent.com/lewiswatson55/TVBackBridge-Jellyfin/main/vidaa-back-manual.js) into the **same folder** as `index.html`. Edit the key values at the top of the script if your remote differs from the `Backspace` / `8` defaults. Set `SHOW_KEY_VIEWER` to `true` to identify a different key, then turn it off again.
3. Add this **one tag** immediately before `</body>` in `index.html`:

   ```html
   <script src="vidaa-back-manual.js"></script>
   ```

4. Fully close and reopen Jellyfin on the TV. Jellyfin Web updates may replace `index.html`, so re-add the tag after an update. Do not load this script and the TV Back Bridge plugin at the same time.
