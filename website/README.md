# Miracle website

Static HTML, CSS, and browser JavaScript. The app artwork comes from `Resources/AppIcon.iconset` in this repository. The interactive conversion wheel is an illustrative demo and does not process visitor files.

## Deploy to Vercel

1. Import `ishuu9837/Miracle` and select the `main` branch.
2. Keep **Root Directory** at the repository root (`./`).
3. Use framework preset **Other**. The root `vercel.json` sets **Output Directory** to `website` and skips installation and build commands.
4. Deploy. No environment variables or ChatGPT sign-in are required by the website.

The DMG download links directly to the GitHub release, rather than storing the installer in the website deployment. Version 0.3.0 is an unnotarized test build for Apple Silicon and macOS 13+.

## Local preview

From the repository root:

```sh
python3 -m http.server 4175 --directory website
```

Open http://localhost:4175. Motion can be paused using the header control and respects the system reduced-motion preference.
