# Miracle website

Static HTML, CSS, and browser JavaScript. The app artwork comes from `Resources/AppIcon.iconset` in this repository. The interactive conversion wheel is an illustrative demo and does not process visitor files.

## Deploy to Vercel

1. Import `ishuu9837/Miracle` and select the `main` branch.
2. Keep **Root Directory** at the repository root (`./`).
3. Use framework preset **Other**. The root `vercel.json` sets **Output Directory** to `website` and skips installation and build commands.
4. Deploy. No environment variables or ChatGPT sign-in are required by the website.

The Mac DMG and Windows archives link directly to their separate GitHub releases. Mac v0.3.0 is an unnotarized test build for Apple Silicon and macOS 13+. Windows v0.3.2 has separate unsigned x64 (Intel/AMD) and ARM64 compact `.7z` packages with bundled offline engines. Extract the complete folder with Windows 11 24H2+ Extract All or an updated 7-Zip. The release records exact sizes and test scope; x64 verification here is under Windows ARM emulation. See the Windows release and feature guide for verification scope, prerequisites and fidelity limits.

## Local preview

From the repository root:

```sh
python3 -m http.server 4175 --directory website
```

Open http://localhost:4175. Motion can be paused using the header control and respects the system reduced-motion preference.

## Walkthrough and setup guide

`walkthrough.js` and `walkthrough.css` add five-step illustrative Mac and Windows tours: installation/extraction, launch, file selection, conversion and result. Platform tabs and step buttons work with a keyboard. Playback runs only while the section is visible, the page is foreground and motion is enabled; visitors can pause or replay. System reduced-motion preference and the header motion toggle stop autoplay. These animations use sample files and do not execute OS actions or process uploads.

The Mac DMG button keeps its direct GitHub asset link and opens a native setup dialog. Visitors can reopen it with the setup-guide button, dismiss with Escape or the close/Done buttons, and return focus to the trigger. The guide describes Applications installation and Apple's Privacy & Security approval flow for the unnotarized test build.

## Guides

`documentation.html` covers Mac and Windows setup, Windows drag + Shift wheel sets, supported formats, fidelity limits and troubleshooting. `privacy.html` documents local job history, session data, shortcuts, hosting, Google Fonts, GitHub downloads and public support. Both pages are linked from navigation and the footer and remain static HTML.
